using eDhaq.Models.Entities;
using eDhaq.Models.Enums;

namespace eDhaq.Services.Interfaces;

public interface IPaymentService
{
    Task<List<Payment>> GetPaymentsAsync(PaymentStatus? status = null, DateTime? from = null, DateTime? to = null);
    Task<Payment?> GetByOrderIdAsync(int orderId);
    Task<Payment> EnsurePaymentForOrderAsync(int orderId);
    Task<bool> UpdatePaymentStatusAsync(int paymentId, PaymentStatus status, string? actorUserId = null, string? note = null);
    Task<bool> UpdatePaymentMethodAsync(int paymentId, PaymentMethod method);

    /// <summary>
    /// Records that the delivery driver collected the payment for an order from the
    /// customer. Customers never mark their own orders as paid — only the driver
    /// (or finance staff) can.
    /// </summary>
    Task<Payment> RecordDriverCollectionAsync(int orderId, PaymentMethod method, string? transactionReference, string? driverUserId);
}
