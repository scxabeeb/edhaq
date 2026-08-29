using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eDhaq.Models.Entities;

public class Invoice
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    [Required, MaxLength(30)]
    public string InvoiceNumber { get; set; } = string.Empty;  // INV-2026-000001

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    public bool IsPaid { get; set; } = false;
    public string? PdfPath { get; set; }
}
