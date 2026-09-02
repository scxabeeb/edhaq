using eDhaq.Common.Constants;
using eDhaq.Common.DTOs;
using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using eDhaq.Repositories.Interfaces;
using eDhaq.Services.Interfaces;
using eDhaq.Web.Areas.Api.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Areas.Api.Controllers;

public class OrdersController : ApiControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ICustomerRepository _customerRepository;
    private readonly IDriverRepository _driverRepository;
    private readonly INotificationService _notificationService;
    private readonly AppDbContext _db;

    /// <summary>
    /// USSD payment short-code template. The {0} placeholder is replaced with
    /// the amount to pay, e.g. *884*442628*25#
    /// </summary>
    public const string UssdPaymentTemplate = "*884*442628*{0}#";

    public static string BuildUssdCode(decimal amount) =>
        string.Format(UssdPaymentTemplate, Math.Round(amount, 2));

    public OrdersController(
        IOrderService orderService,
        ICustomerRepository customerRepository,
        IDriverRepository driverRepository,
        INotificationService notificationService,
        AppDbContext db)
    {
        _orderService = orderService;
        _customerRepository = customerRepository;
        _driverRepository = driverRepository;
        _notificationService = notificationService;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PagedOrdersResponse>> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var customer = await GetCustomerAsync();
        if (customer is null)
        {
            return Forbid();
        }

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var orders = (await _orderService.GetCustomerOrdersAsync(customer.Id, page, pageSize)).ToList();
        var totalCount = await _orderService.GetCustomerOrderCountAsync(customer.Id);

        var paged = new PagedOrdersResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Orders = orders
        };

        return Ok(paged);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDetailDto>> GetOrder(int id)
    {
        var order = await _orderService.GetOrderDetailsAsync(id);
        if (order is null)
        {
            return NotFound(new ProblemDetails { Title = "Order not found." });
        }

        var userId = GetCurrentUserId();
        if (order.Customer.UserId != userId && !User.IsInRole("Administrator") && !User.IsInRole("Manager"))
        {
            return Forbid();
        }

        return Ok(ToDetailDto(order));
    }

    [HttpPost]
    public async Task<ActionResult<OrderSummaryDto>> CreateOrder([FromBody] CreateOrderDto request)
    {
        var customer = await GetCustomerAsync();
        if (customer is null)
        {
            return Forbid();
        }

        var addressOwned = await EnsureAddressesOwnedAsync(customer.Id, request.PickupAddressId, request.DeliveryAddressId);
        if (!addressOwned)
        {
            return BadRequest(new ProblemDetails { Title = "One or more selected addresses are not valid for this customer." });
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new ProblemDetails { Title = "At least one order item is required." });
        }

        string? actorUserId = GetCurrentUserId();

        var order = await _orderService.CreateOrderAsync(customer.Id, request, actorUserId);

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, new OrderSummaryDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            EstimatedCompletionAt = order.EstimatedCompletionAt
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Payment endpoints
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the USSD payment code the customer should dial to clear the
    /// payment for this order, e.g. *884*442628*25#
    /// </summary>
    [HttpGet("{id:int}/payment-ussd")]
    public async Task<ActionResult<object>> GetPaymentUssd(int id)
    {
        var order = await _orderService.GetOrderDetailsAsync(id);
        if (order is null)
        {
            return NotFound(new ProblemDetails { Title = "Order not found." });
        }

        var userId = GetCurrentUserId();
        if (order.Customer.UserId != userId && !User.IsInRole("Administrator") && !User.IsInRole("Manager"))
        {
            return Forbid();
        }

        return Ok(new
        {
            orderId = order.Id,
            orderNumber = order.OrderNumber,
            amount = order.TotalAmount,
            ussdCode = BuildUssdCode(order.TotalAmount),
            paymentStatus = order.PaymentStatus.ToString(),
            instruction = "Dial this code on your phone to complete the payment. Delivery will only happen after the payment is cleared."
        });
    }

    /// <summary>
    /// Customer confirms they have cleared the payment using the USSD code.
    /// Marks the order as paid so that delivery can proceed.
    /// </summary>
    [HttpPost("{id:int}/pay")]
    public async Task<IActionResult> PayOrder(int id, [FromBody] PayOrderRequest? request)
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
            .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound(new ProblemDetails { Title = "Order not found." });
        }

        var userId = GetCurrentUserId();
        if (order.Customer.UserId != userId && !User.IsInRole("Administrator") && !User.IsInRole("Manager"))
        {
            return Forbid();
        }

        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            return BadRequest(new ProblemDetails { Title = "This order has already been paid." });
        }

        var reference = string.IsNullOrWhiteSpace(request?.TransactionReference)
            ? $"USSD-{order.OrderNumber}-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : request!.TransactionReference!.Trim();

        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.OrderId == order.Id);
        if (payment is null)
        {
            payment = new Payment { OrderId = order.Id };
            _db.Payments.Add(payment);
        }

        payment.TransactionReference = reference;
        payment.Method = order.PaymentMethod;
        payment.Status = PaymentStatus.Paid;
        payment.Amount = order.TotalAmount;
        payment.GatewayResponse = "Paid via USSD " + BuildUssdCode(order.TotalAmount);
        payment.PaidAt = DateTime.UtcNow;

        order.PaymentStatus = PaymentStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _notificationService.CreateAsync(
            order.Customer.UserId,
            "Payment received",
            $"Your payment of ${order.TotalAmount:0.##} for order {order.OrderNumber} has been cleared. Your delivery can now proceed.",
            NotificationType.PaymentConfirmed,
            orderId: order.Id);

        return Ok(new { message = "Payment cleared successfully.", ussdCode = BuildUssdCode(order.TotalAmount) });
    }

    [HttpPost("{id:int}/confirm-delivery")]
    public async Task<ActionResult<OrderDetailDto>> ConfirmDelivery(int id)
    {
        var order = await _orderService.GetOrderDetailsAsync(id);
        if (order is null)
        {
            return NotFound(new ProblemDetails { Title = "Order not found." });
        }

        var userId = GetCurrentUserId();
        if (order.Customer.UserId != userId && !User.IsInRole("Administrator") && !User.IsInRole("Manager"))
        {
            return Forbid();
        }

        if (order.Status != OrderStatus.Delivered)
        {
            return BadRequest(new ProblemDetails { Title = "Order cannot be confirmed yet." });
        }

        await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
        {
            OrderId = order.Id,
            Status = OrderStatus.CustomerConfirmed,
            Note = "Customer confirmed delivery"
        }, userId, User.Identity?.Name);

        await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
        {
            OrderId = order.Id,
            Status = OrderStatus.Completed,
            Note = "Order completed"
        }, userId, User.Identity?.Name);

        order = await _orderService.GetOrderDetailsAsync(id) ?? order;

        return Ok(ToDetailDto(order));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Driver endpoints
    // ════════════════════════════════════════════════════════════════════════

    [HttpGet("driver/assignments")]
    [Authorize(Roles = "Administrator,PickupDriver,DeliveryDriver")]
    public async Task<ActionResult<List<DriverAssignmentDetailDto>>> GetDriverAssignments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DriverJobAction? statusFilter = null,
        [FromQuery] bool? isPickupFilter = null)
    {
        var driver = await GetDriverAsync();
        if (driver is null)
        {
            return Forbid();
        }

        var query = _db.DriverAssignments
            .Where(x => x.DriverId == driver.Id)
            .Include(x => x.Order)
            .ThenInclude(o => o.Customer)
            .ThenInclude(c => c.User)
            .Include(x => x.Order.PickupAddress)
            .Include(x => x.Order.DeliveryAddress)
            .Include(x => x.Order.Items)
            .ThenInclude(i => i.Service)
            .AsQueryable();

        if (statusFilter.HasValue)
        {
            query = query.Where(x => x.Status == statusFilter.Value);
        }

        if (isPickupFilter.HasValue)
        {
            query = query.Where(x => x.IsPickup == isPickupFilter.Value);
        }

        var assignments = await query
            .OrderByDescending(x => x.AssignedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = assignments.Select(a => new DriverAssignmentDetailDto
        {
            Id = a.Id,
            OrderId = a.OrderId,
            OrderNumber = a.Order.OrderNumber,
            Status = a.Order.Status,
            PaymentStatus = a.Order.PaymentStatus,
            PaymentMethod = a.Order.PaymentMethod,
            TotalAmount = a.Order.TotalAmount,
            IsPickup = a.IsPickup,
            Action = a.Status,
            AssignedAt = a.AssignedAt,
            AcceptedAt = a.AcceptedAt,
            CompletedAt = a.CompletedAt,
            Notes = a.Notes,
            PickupScheduledAt = a.Order.PickupScheduledAt,
            DeliveryScheduledAt = a.Order.DeliveryScheduledAt,
            PickupActualAt = a.Order.PickupActualAt,
            DeliveryActualAt = a.Order.DeliveryActualAt,
            PickupStreet = a.Order.PickupAddress?.Street,
            PickupCityName = a.Order.PickupAddress?.City?.Name,
            DeliveryStreet = a.Order.DeliveryAddress?.Street,
            DeliveryCityName = a.Order.DeliveryAddress?.City?.Name,
            CustomerName = $"{a.Order.Customer.User.FirstName} {a.Order.Customer.User.LastName}".Trim(),
            CustomerPhone = a.Order.Customer.User.PhoneNumber,
            ServiceNames = a.Order.Items.Select(i => i.Service?.Name ?? string.Empty).ToList()
        }).ToList();

        return Ok(result);
    }

    [HttpPost("driver/assignments/{assignmentId:int}/accept")]
    [Authorize(Roles = "Administrator,PickupDriver,DeliveryDriver")]
    public async Task<IActionResult> AcceptAssignment(int assignmentId)
    {
        var driver = await GetDriverAsync();
        if (driver is null)
        {
            return Forbid();
        }

        var assignment = await _db.DriverAssignments
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.DriverId == driver.Id);

        if (assignment is null)
        {
            return NotFound(new ProblemDetails { Title = "Assignment not found." });
        }

        if (assignment.Status == DriverJobAction.Completed)
        {
            return BadRequest(new ProblemDetails { Title = "This assignment is already completed." });
        }

        if (assignment.Status != DriverJobAction.Pending)
        {
            return BadRequest(new ProblemDetails { Title = "This assignment is not in a pending state." });
        }

        var userId = GetCurrentUserId();
        var note = assignment.IsPickup
            ? "Pickup driver accepted assignment."
            : "Delivery driver accepted assignment.";

        await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
        {
            OrderId = assignment.OrderId,
            Status = assignment.Order.Status,
            Note = note
        }, userId, User.Identity?.Name);

        assignment.Status = DriverJobAction.Accepted;
        assignment.AcceptedAt = DateTime.UtcNow;
        assignment.Notes = note;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Assignment accepted." });
    }

    [HttpPost("driver/assignments/{assignmentId:int}/on-the-way")]
    [Authorize(Roles = "Administrator,PickupDriver,DeliveryDriver")]
    public async Task<IActionResult> AssignmentOnTheWay(int assignmentId)
    {
        var driver = await GetDriverAsync();
        if (driver is null)
        {
            return Forbid();
        }

        var assignment = await _db.DriverAssignments
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.DriverId == driver.Id);

        if (assignment is null)
        {
            return NotFound(new ProblemDetails { Title = "Assignment not found." });
        }

        if (assignment.Status != DriverJobAction.Accepted)
        {
            return BadRequest(new ProblemDetails { Title = "Assignment must be accepted before notifying 'On the way'." });
        }

        var userId = GetCurrentUserId();

        if (assignment.IsPickup && assignment.Order.Status == OrderStatus.DriverAssigned)
        {
            await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
            {
                OrderId = assignment.OrderId,
                Status = OrderStatus.DriverOnTheWay,
                Note = "Pickup driver is on the way."
            }, userId, User.Identity?.Name);
        }
        else if (!assignment.IsPickup && assignment.Order.Status == OrderStatus.ReadyForDelivery)
        {
            await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
            {
                OrderId = assignment.OrderId,
                Status = OrderStatus.OutForDelivery,
                Note = "Delivery driver is on the way."
            }, userId, User.Identity?.Name);
        }
        else
        {
            // Already on the way or in a later stage — just log a tracking note.
            await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
            {
                OrderId = assignment.OrderId,
                Status = assignment.Order.Status,
                Note = assignment.IsPickup
                    ? "Pickup driver is on the way."
                    : "Delivery driver is on the way."
            }, userId, User.Identity?.Name);
        }

        return Ok(new { message = "Customer notified: driver is on the way." });
    }

    [HttpPost("driver/assignments/{assignmentId:int}/at-gate")]
    [Authorize(Roles = "Administrator,PickupDriver,DeliveryDriver")]
    public async Task<IActionResult> AssignmentAtGate(int assignmentId)
    {
        var driver = await GetDriverAsync();
        if (driver is null)
        {
            return Forbid();
        }

        var assignment = await _db.DriverAssignments
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.DriverId == driver.Id);

        if (assignment is null)
        {
            return NotFound(new ProblemDetails { Title = "Assignment not found." });
        }

        if (assignment.Status != DriverJobAction.Accepted)
        {
            return BadRequest(new ProblemDetails { Title = "Assignment must be accepted before notifying 'At the gate'." });
        }

        var userId = GetCurrentUserId();
        var note = assignment.IsPickup
            ? "Pickup driver has arrived and is at the gate."
            : "Delivery driver has arrived and is at the gate.";

        await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
        {
            OrderId = assignment.OrderId,
            Status = assignment.Order.Status,
            Note = note
        }, userId, User.Identity?.Name);

        return Ok(new { message = "Customer notified: driver is at the gate." });
    }

    [HttpPost("driver/assignments/{assignmentId:int}/complete")]
    [Authorize(Roles = "Administrator,PickupDriver,DeliveryDriver")]
    public async Task<IActionResult> CompleteAssignment(int assignmentId)
    {
        var driver = await GetDriverAsync();
        if (driver is null)
        {
            return Forbid();
        }

        var assignment = await _db.DriverAssignments
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.DriverId == driver.Id);

        if (assignment is null)
        {
            return NotFound(new ProblemDetails { Title = "Assignment not found." });
        }

        if (assignment.Status == DriverJobAction.Completed)
        {
            return BadRequest(new ProblemDetails { Title = "This assignment is already completed." });
        }

        if (assignment.Status != DriverJobAction.Accepted)
        {
            return BadRequest(new ProblemDetails { Title = "Assignment must be accepted before completion." });
        }

        if (!assignment.IsPickup && !assignment.Order.PickupActualAt.HasValue)
        {
            return BadRequest(new ProblemDetails { Title = "Pickup must be completed before delivery." });
        }

        // ── Payment gate: delivery cannot be completed until payment is cleared ──
        if (!assignment.IsPickup && assignment.Order.PaymentStatus != PaymentStatus.Paid)
        {
            var ussdCode = BuildUssdCode(assignment.Order.TotalAmount);

            await _notificationService.CreateAsync(
                assignment.Order.Customer.UserId,
                "Payment required before delivery",
                $"Your order {assignment.Order.OrderNumber} (${assignment.Order.TotalAmount:0.##}) cannot be delivered until the payment is cleared. Please dial {ussdCode} on your phone to pay, then mark it as paid in the app.",
                NotificationType.PaymentReminder,
                orderId: assignment.OrderId);

            return BadRequest(new ProblemDetails
            {
                Title = "Payment must be cleared before delivery.",
                Detail = $"The customer must dial {ussdCode} to clear the payment of ${assignment.Order.TotalAmount:0.##}. A notification has been sent to the customer."
            });
        }

        var userId = GetCurrentUserId();

        if (assignment.IsPickup)
        {
            await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
            {
                OrderId = assignment.OrderId,
                Status = OrderStatus.ClothesPickedUp,
                Note = "Clothes picked up by driver."
            }, userId, User.Identity?.Name);

            await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
            {
                OrderId = assignment.OrderId,
                Status = OrderStatus.LaundryReceived,
                Note = "Clothes delivered to the laundry."
            }, userId, User.Identity?.Name);
        }
        else
        {
            await _orderService.UpdateStatusAsync(new UpdateOrderStatusDto
            {
                OrderId = assignment.OrderId,
                Status = OrderStatus.Delivered,
                Note = "Delivery completed by driver."
            }, userId, User.Identity?.Name);
        }

        assignment.Status = DriverJobAction.Completed;
        assignment.CompletedAt = DateTime.UtcNow;

        if (assignment.IsPickup)
        {
            assignment.Order.PickupActualAt = DateTime.UtcNow;
            assignment.Notes = "Clothes delivered to the laundry.";
        }
        else
        {
            assignment.Order.DeliveryActualAt = DateTime.UtcNow;
            assignment.Notes = "Delivery completed by driver.";
        }

        await _db.SaveChangesAsync();

        return Ok(new { message = "Assignment completed." });
    }

    [HttpPost("driver/assignments/{assignmentId:int}/collect-payment")]
    [Authorize(Roles = "Administrator,DeliveryDriver")]
    public async Task<IActionResult> CollectPayment(int assignmentId)
    {
        var driver = await GetDriverAsync();
        if (driver is null)
        {
            return Forbid();
        }

        var assignment = await _db.DriverAssignments
            .Include(x => x.Order)
            .FirstOrDefaultAsync(x => x.Id == assignmentId && x.DriverId == driver.Id);

        if (assignment is null)
        {
            return NotFound(new ProblemDetails { Title = "Assignment not found." });
        }

        if (assignment.IsPickup)
        {
            return BadRequest(new ProblemDetails { Title = "Payment collection is only for delivery assignments." });
        }

        if (assignment.Order.PaymentStatus == PaymentStatus.Paid)
        {
            return BadRequest(new ProblemDetails { Title = "Payment has already been collected for this order." });
        }

        assignment.Order.PaymentStatus = PaymentStatus.Paid;
        assignment.Order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Payment collected successfully." });
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Admin endpoints
    // ════════════════════════════════════════════════════════════════════════

    [HttpGet("admin")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<ActionResult<PagedOrdersResponse>> GetAdminOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] OrderStatus? statusFilter = null,
        [FromQuery] bool? activeOnly = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Orders
            .Include(o => o.Customer)
            .ThenInclude(c => c.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x => x.OrderNumber.Contains(s)
                                     || x.Customer.User.FirstName.Contains(s)
                                     || x.Customer.User.LastName.Contains(s)
                                     || x.Customer.User.Email!.Contains(s));
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(x => x.Status == statusFilter.Value);
        }

        if (activeOnly == true)
        {
            var terminal = new[] { OrderStatus.Delivered, OrderStatus.Completed, OrderStatus.Cancelled, OrderStatus.CustomerConfirmed };
            query = query.Where(x => !terminal.Contains(x.Status));
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= dateTo.Value);
        }

        var totalCount = await query.CountAsync();

        var orders = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt,
                EstimatedCompletionAt = o.EstimatedCompletionAt
            })
            .ToListAsync();

        var paged = new PagedOrdersResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Orders = orders
        };

        return Ok(paged);
    }

    [HttpPost("admin/{id:int}/status")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto request)
    {
        request.OrderId = id;
        var userId = GetCurrentUserId();
        var updated = await _orderService.UpdateStatusAsync(request, userId, User.Identity?.Name);

        if (!updated)
        {
            return BadRequest(new ProblemDetails { Title = "Could not update the order status. Check the current stage of the order." });
        }

        return Ok(new { message = "Order status updated." });
    }

    [HttpPost("admin/assign-driver")]
    [Authorize(Roles = "Administrator,Manager")]
    public async Task<IActionResult> AssignDriver([FromBody] AssignDriverRequest request)
    {
        var order = await _db.Orders
            .Include(x => x.DriverAssignments)
            .FirstOrDefaultAsync(x => x.Id == request.OrderId);

        if (order is null)
        {
            return NotFound(new ProblemDetails { Title = "Order not found." });
        }

        if (!await _db.Drivers.AnyAsync(x => x.Id == request.DriverId))
        {
            return NotFound(new ProblemDetails { Title = "Driver not found." });
        }

        var isPickupAssignment = order.Status != OrderStatus.ReadyForDelivery
            && order.Status != OrderStatus.OutForDelivery
            && order.Status != OrderStatus.Delivered
            && order.Status != OrderStatus.Completed
            && order.Status != OrderStatus.CustomerConfirmed;

        var assignment = order.DriverAssignments
            .FirstOrDefault(x => x.IsPickup == isPickupAssignment && x.Status != DriverJobAction.Completed);

        if (assignment is null)
        {
            _db.DriverAssignments.Add(new DriverAssignment
            {
                OrderId = order.Id,
                DriverId = request.DriverId,
                IsPickup = isPickupAssignment,
                Status = DriverJobAction.Pending,
                AssignedAt = DateTime.UtcNow,
                Notes = "Assigned from admin"
            });
        }
        else
        {
            assignment.DriverId = request.DriverId;
            assignment.AssignedAt = DateTime.UtcNow;
            assignment.Status = DriverJobAction.Pending;
        }

        if (isPickupAssignment)
        {
            order.Status = OrderStatus.DriverAssigned;
        }

        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Driver assigned successfully." });
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Private helpers
    // ════════════════════════════════════════════════════════════════════════

    private async Task<Customer?> GetCustomerAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _customerRepository.GetByUserIdAsync(userId);
    }

    private async Task<Driver?> GetDriverAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var driver = await _driverRepository.GetByUserIdAsync(userId);

        // Auto-create a Driver profile if the user has a driver role but no
        // Driver entity exists yet. This ensures the driver portal shows data
        // instead of returning 403 Forbidden.
        if (driver is null && (User.IsInRole("Administrator") || User.IsInRole("PickupDriver") || User.IsInRole("DeliveryDriver")))
        {
            driver = new Driver
            {
                UserId = userId,
                Status = DriverStatus.Offline,
                CreatedAt = DateTime.UtcNow
            };
            _db.Drivers.Add(driver);
            await _db.SaveChangesAsync();
        }

        return driver;
    }

    private async Task<bool> EnsureAddressesOwnedAsync(int customerId, int pickupId, int deliveryId)
    {
        var owned = await _db.Addresses
            .Where(a => a.CustomerId == customerId)
            .Select(a => a.Id)
            .ToListAsync();

        return owned.Contains(pickupId) && owned.Contains(deliveryId);
    }

    private static OrderDetailDto ToDetailDto(Order order)
    {
        return new OrderDetailDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            PaymentMethod = order.PaymentMethod,
            SubTotal = order.SubTotal,
            DeliveryFee = 0, // delivery fee removed everywhere
            Discount = order.Discount,
            TotalAmount = order.SubTotal - order.Discount,
            SpecialInstructions = order.SpecialInstructions,
            PickupScheduledAt = order.PickupScheduledAt,
            PickupActualAt = order.PickupActualAt,
            DeliveryScheduledAt = order.DeliveryScheduledAt,
            DeliveryActualAt = order.DeliveryActualAt,
            EstimatedCompletionAt = order.EstimatedCompletionAt,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ServiceId = i.ServiceId,
                ServiceName = i.Service?.Name,
                CategoryName = i.Service?.Category?.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                Notes = i.Notes
            }).ToList(),
            Trackings = order.Trackings.OrderByDescending(t => t.CreatedAt).Select(t => new OrderTrackingDto
            {
                Status = t.Status,
                Note = t.Note,
                UpdatedByName = t.UpdatedByName,
                DriverLatitude = t.DriverLatitude,
                DriverLongitude = t.DriverLongitude,
                CreatedAt = t.CreatedAt
            }).ToList(),
            DriverAssignments = order.DriverAssignments.OrderByDescending(d => d.AssignedAt).Select(d => new DriverAssignmentDto
            {
                DriverId = d.DriverId,
                DriverName = d.Driver?.User != null ? $"{d.Driver.User.FirstName} {d.Driver.User.LastName}".Trim() : null,
                PhoneNumber = d.Driver?.User?.PhoneNumber,
                VehicleModel = d.Driver?.VehicleModel,
                LicensePlate = d.Driver?.LicensePlate,
                IsPickup = d.IsPickup,
                Status = d.Status,
                AssignedAt = d.AssignedAt
            }).ToList(),
            PickupAddress = order.PickupAddress is null ? null : new AddressSummaryDto
            {
                Id = order.PickupAddress.Id,
                Label = order.PickupAddress.Label,
                Street = order.PickupAddress.Street,
                District = order.PickupAddress.District,
                CityName = order.PickupAddress.City?.Name,
                VillageName = order.PickupAddress.Village?.Name,
                SubVillageName = order.PickupAddress.SubVillage?.Name
            },
            DeliveryAddress = order.DeliveryAddress is null ? null : new AddressSummaryDto
            {
                Id = order.DeliveryAddress.Id,
                Label = order.DeliveryAddress.Label,
                Street = order.DeliveryAddress.Street,
                District = order.DeliveryAddress.District,
                CityName = order.DeliveryAddress.City?.Name,
                VillageName = order.DeliveryAddress.Village?.Name,
                SubVillageName = order.DeliveryAddress.SubVillage?.Name
            },
            QrCodeBase64 = order.QrCodeBase64,
            BarcodeValue = order.BarcodeValue
        };
    }
}
