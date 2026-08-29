using eDhaq.Data;
using eDhaq.Models.Entities;
using eDhaq.Models.Enums;
using eDhaq.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;

    public PaymentService(AppDbContext db, INotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    public async Task<List<Payment>> GetPaymentsAsync(PaymentStatus? status = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _db.Payments
            .Include(x => x.Order)
                .ThenInclude(x => x.Customer)
                .ThenInclude(x => x.User)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= to.Value);
        }

        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync();
    }

    public async Task<Payment?> GetByOrderIdAsync(int orderId)
        => await _db.Payments
            .Include(x => x.Order)
                .ThenInclude(x => x.Customer)
                .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.OrderId == orderId);

    public async Task<Payment> EnsurePaymentForOrderAsync(int orderId)
    {
        var existing = await GetByOrderIdAsync(orderId);
        if (existing is not null)
        {
            return existing;
        }

        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == orderId)
            ?? throw new InvalidOperationException("Order not found.");

        var payment = new Payment
        {
            OrderId = orderId,
            Amount = order.TotalAmount,
            Method = order.PaymentMethod,
            Status = order.PaymentStatus,
            TransactionReference = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{orderId}",
            Currency = "USD",
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return payment;
    }

    public async Task<bool> UpdatePaymentStatusAsync(int paymentId, PaymentStatus status, string? actorUserId = null, string? note = null)
    {
        var payment = await _db.Payments
            .Include(x => x.Order)
                .ThenInclude(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == paymentId);

        if (payment is null)
        {
            return false;
        }

        payment.Status = status;
        payment.Order.PaymentStatus = status;
        if (status == PaymentStatus.Paid)
        {
            payment.PaidAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.Id == payment.Order.CustomerId);
        if (customer is not null)
        {
            await _notificationService.CreateAsync(
                customer.UserId,
                "Payment Update",
                $"Payment for order {payment.Order.OrderNumber} is now {status}.",
                NotificationType.PaymentConfirmed,
                $"/Customer/Payments/Index?orderId={payment.OrderId}",
                payment.OrderId);
        }

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = actorUserId,
            Action = "UpdatePaymentStatus",
            EntityName = nameof(Payment),
            EntityId = paymentId.ToString(),
            NewValues = $"Status={status}; Note={note}",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdatePaymentMethodAsync(int paymentId, PaymentMethod method)
    {
        var payment = await _db.Payments.Include(x => x.Order).FirstOrDefaultAsync(x => x.Id == paymentId);
        if (payment is null)
        {
            return false;
        }

        payment.Method = method;
        payment.Order.PaymentMethod = method;
        await _db.SaveChangesAsync();
        return true;
    }
}
