using eDhaq.Common.Constants;
using eDhaq.Common.DTOs;
using eDhaq.Common.Helpers;
using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using eDhaq.Repositories.Interfaces;
using eDhaq.Services.Interfaces;
using eDhaq.Services.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QRCoder;

namespace eDhaq.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<TrackingHub> _hubContext;
    private readonly ILogger<OrderService> _logger;

    private static readonly Dictionary<OrderStatus, OrderStatus> LaundryStageFlow = new()
    {
        [OrderStatus.LaundryReceived] = OrderStatus.Sorting,
        [OrderStatus.Sorting] = OrderStatus.Washing,
        [OrderStatus.Washing] = OrderStatus.Drying,
        [OrderStatus.Drying] = OrderStatus.Ironing,
        [OrderStatus.Ironing] = OrderStatus.Folding,
        [OrderStatus.Folding] = OrderStatus.Packaging,
        [OrderStatus.Packaging] = OrderStatus.ReadyForDelivery
    };

    private static readonly HashSet<OrderStatus> ManualLaundryStages =
    [
        OrderStatus.LaundryReceived,
        OrderStatus.Sorting,
        OrderStatus.Washing,
        OrderStatus.DryCleaning,
        OrderStatus.Drying,
        OrderStatus.Ironing,
        OrderStatus.Folding,
        OrderStatus.Packaging,
        OrderStatus.ReadyForDelivery
    ];

    public OrderService(
        IUnitOfWork uow,
        AppDbContext db,
        INotificationService notificationService,
        IHubContext<TrackingHub> hubContext,
        ILogger<OrderService> logger)
    {
        _uow = uow;
        _db = db;
        _notificationService = notificationService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<Order> CreateOrderAsync(int customerId, CreateOrderDto dto, string? actorUserId = null)
    {
        var useTransaction = _db.Database.IsRelational();
        if (useTransaction)
        {
            await _uow.BeginTransactionAsync();
        }

        try
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == customerId);
            if (customer is null)
            {
                throw new InvalidOperationException($"Customer {customerId} was not found.");
            }

            var sequence = await _uow.Orders.GetNextSequenceNumberAsync();
            var orderNumber = OrderNumberGenerator.Generate(sequence);
            var order = new Order
            {
                OrderNumber = orderNumber,
                CustomerId = customerId,
                PickupAddressId = dto.PickupAddressId,
                DeliveryAddressId = dto.DeliveryAddressId,
                PickupScheduledAt = dto.PickupScheduledAt,
                DeliveryScheduledAt = dto.DeliveryScheduledAt,
                PaymentMethod = dto.PaymentMethod,
                PaymentStatus = PaymentStatus.Pending,
                Status = OrderStatus.OrderPlaced,
                SpecialInstructions = dto.SpecialInstructions,
                EstimatedCompletionAt = dto.PickupScheduledAt.AddHours(24),
                CreatedAt = DateTime.UtcNow
            };

            decimal subtotal = 0;
            var serviceIds = dto.Items.Select(x => x.ServiceId).Distinct().ToList();
            var services = await _db.LaundryServices
                .Where(s => serviceIds.Contains(s.Id) && s.IsActive)
                .ToDictionaryAsync(s => s.Id, s => s.PricePerPiece);

            foreach (var item in dto.Items)
            {
                if (!services.TryGetValue(item.ServiceId, out var unitPrice))
                {
                    throw new InvalidOperationException($"Service {item.ServiceId} is not available.");
                }

                var total = unitPrice * item.Quantity;
                subtotal += total;

                order.Items.Add(new OrderItem
                {
                    ServiceId = item.ServiceId,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = total,
                    Notes = item.Notes
                });
            }

            order.SubTotal = subtotal;
            order.DeliveryFee = 0;
            order.Discount = 0;
            order.TotalAmount = order.SubTotal - order.Discount;
            customer.TotalOrders += 1;
            customer.TotalSpent += order.TotalAmount;

            order.QrCodeBase64 = GenerateQr(order.OrderNumber);
            order.BarcodeValue = order.OrderNumber;

            await _uow.Orders.AddAsync(order);

            _db.Payments.Add(new Payment
            {
                Order = order,
                Amount = order.TotalAmount,
                Method = order.PaymentMethod,
                Status = order.PaymentStatus,
                TransactionReference = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{orderNumber}",
                Currency = "USD",
                CreatedAt = DateTime.UtcNow
            });

            order.Trackings.Add(new OrderTracking
            {
                Status = OrderStatus.OrderPlaced,
                Note = "Order created",
                UpdatedByUserId = actorUserId,
                UpdatedByName = "System",
                CreatedAt = DateTime.UtcNow
            });

            await _uow.SaveChangesAsync();
            if (useTransaction)
            {
                await _uow.CommitTransactionAsync();
            }

            await _notificationService.CreateAsync(customer.UserId,
                "Order Placed",
                $"Your order {order.OrderNumber} has been created.",
                NotificationType.OrderCreated,
                $"/Customer/Orders/Track?orderId={order.Id}",
                order.Id);

            await NotifyOperationalUsersAsync(order);

            await _hubContext.Clients.Group($"order-{order.OrderNumber}").SendAsync("orderStatusChanged", new
            {
                orderId = order.Id,
                orderNumber = order.OrderNumber,
                status = order.Status.ToString(),
                note = "Order created",
                timestamp = DateTime.UtcNow
            });

            return order;
        }
        catch (Exception ex)
        {
            if (useTransaction)
            {
                await _uow.RollbackTransactionAsync();
            }

            _logger.LogError(ex, "Error creating order for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<Order?> GetOrderDetailsAsync(int orderId)
        => await _uow.Orders.GetWithDetailsAsync(orderId);

    public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
        => await _uow.Orders.GetByOrderNumberAsync(orderNumber);

    public async Task<IEnumerable<OrderSummaryDto>> GetCustomerOrdersAsync(int customerId, int page = 1, int pageSize = 10)
    {
        var orders = await _uow.Orders.GetByCustomerAsync(customerId, page, pageSize);
        return orders.Select(o => new OrderSummaryDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status,
            TotalAmount = o.TotalAmount,
            CreatedAt = o.CreatedAt,
            EstimatedCompletionAt = o.EstimatedCompletionAt
        });
    }

        public async Task<int> GetCustomerOrderCountAsync(int customerId)
        => await _uow.Orders.GetCustomerOrderCountAsync(customerId);

    public async Task<IEnumerable<OrderSummaryDto>> GetAllOrdersAsync(int page = 1, int pageSize = 20)
    {
        var orders = await _uow.Orders.GetAllAsync(page, pageSize);
        return orders.Select(o => new OrderSummaryDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status,
            TotalAmount = o.TotalAmount,
            CreatedAt = o.CreatedAt,
            EstimatedCompletionAt = o.EstimatedCompletionAt,
            CustomerName = o.Customer?.User != null
                ? $"{o.Customer.User.FirstName} {o.Customer.User.LastName}".Trim()
                : string.Empty
        });
    }

    public async Task<bool> UpdateStatusAsync(UpdateOrderStatusDto dto, string? actorUserId = null, string? actorName = null)
    {
        var order = await _uow.Orders.GetWithDetailsAsync(dto.OrderId);
        if (order is null)
        {
            return false;
        }

        if (!IsAllowedStatusTransition(order.Status, dto.Status))
        {
            _logger.LogWarning(
                "Rejected invalid status transition for order {OrderNumber}: {Current} -> {Requested}",
                order.OrderNumber,
                order.Status,
                dto.Status);
            return false;
        }

        order.Status = dto.Status;
        order.UpdatedAt = DateTime.UtcNow;

        order.Trackings.Add(new OrderTracking
        {
            OrderId = order.Id,
            Status = dto.Status,
            Note = dto.Note,
            UpdatedByUserId = actorUserId,
            UpdatedByName = actorName,
            CreatedAt = DateTime.UtcNow
        });

        _uow.Orders.Update(order);
        await _uow.SaveChangesAsync();

        var customer = await _uow.Customers.GetByIdAsync(order.CustomerId);
        if (customer is not null)
        {
            await _notificationService.CreateAsync(customer.UserId,
                "Order Update",
                $"Your order {order.OrderNumber} status is now {dto.Status}.",
                NotificationType.General,
                $"/Customer/Orders/Track?orderId={order.Id}",
                order.Id);
        }

        await _hubContext.Clients.Group($"order-{order.OrderNumber}").SendAsync("orderStatusChanged", new
        {
            orderId = order.Id,
            orderNumber = order.OrderNumber,
            status = dto.Status.ToString(),
            note = dto.Note,
            timestamp = DateTime.UtcNow
        });

        return true;
    }

    public async Task<string> GenerateNextOrderNumberAsync()
    {
        var next = await _uow.Orders.GetNextSequenceNumberAsync();
        return OrderNumberGenerator.Generate(next);
    }

    public async Task<Dictionary<OrderStatus, int>> GetOrderStatusCountsAsync()
        => await _uow.Orders.GetStatusCountsAsync();

    private static string GenerateQr(string value)
    {
        using var qrGen = new QRCodeGenerator();
        using var data = qrGen.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(5);
        return Convert.ToBase64String(bytes);
    }

    private async Task NotifyOperationalUsersAsync(Order order)
    {
        var operationalRoles = new[]
        {
            AppRoles.Administrator,
            AppRoles.Manager,
            AppRoles.LaundryStaff
        };

        var candidates = await (
            from user in _db.Users
            join userRole in _db.UserRoles on user.Id equals userRole.UserId
            join role in _db.Roles on userRole.RoleId equals role.Id
            where user.IsActive && role.Name != null && operationalRoles.Contains(role.Name)
            select new
            {
                user.Id,
                RoleName = role.Name!
            })
            .ToListAsync();

        foreach (var recipient in candidates
            .GroupBy(x => x.Id)
            .Select(x => new
            {
                UserId = x.Key,
                IsStaff = x.Any(y => y.RoleName == AppRoles.LaundryStaff)
            }))
        {
            var actionUrl = recipient.IsStaff
                ? $"/Staff/Orders/Process?Search={order.OrderNumber}"
                : $"/Admin/Orders/Index?Search={order.OrderNumber}";

            await _notificationService.CreateAsync(
                recipient.UserId,
                "New Customer Order",
                $"Order {order.OrderNumber} was placed and is ready for processing.",
                NotificationType.OrderCreated,
                actionUrl,
                order.Id);
        }
    }

    private static bool IsAllowedStatusTransition(OrderStatus current, OrderStatus requested)
    {
        if (current == requested)
        {
            return true;
        }

        if (current == OrderStatus.ClothesPickedUp)
        {
            return ManualLaundryStages.Contains(requested);
        }

        if (LaundryStageFlow.TryGetValue(current, out var nextLaundryStage) && requested == nextLaundryStage)
        {
            return true;
        }

        if (ManualLaundryStages.Contains(current) && ManualLaundryStages.Contains(requested))
        {
            return true;
        }

        if (current == OrderStatus.ReadyForDelivery && requested == OrderStatus.OutForDelivery)
        {
            return true;
        }

        if (current == OrderStatus.DriverAssigned && requested == OrderStatus.DriverOnTheWay)
        {
            return true;
        }

        if (current == OrderStatus.OutForDelivery && requested == OrderStatus.Delivered)
        {
            return true;
        }

        if (current == OrderStatus.Delivered && (requested == OrderStatus.CustomerConfirmed || requested == OrderStatus.Completed))
        {
            return true;
        }

        return true;
    }
}
