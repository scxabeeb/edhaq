using ClosedXML.Excel;
using eDhaq.Data;
using eDhaq.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace eDhaq.Services.Implementations;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateOrdersPdfAsync(DateTime? from = null, DateTime? to = null)
    {
        var data = await QueryOrders(from, to);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text("e-Dhaq Orders Report").SemiBold().FontSize(18);
                page.Content().Column(col =>
                {
                    foreach (var row in data)
                    {
                        col.Item().Text($"{row.OrderNumber} | {row.CustomerName} | {row.Status} | {row.TotalAmount:C} | {row.CreatedAt:yyyy-MM-dd}");
                    }
                });
                page.Footer().AlignCenter().Text($"Generated on {DateTime.UtcNow:u}");
            });
        }).GeneratePdf();
    }

    public async Task<byte[]> GenerateOrdersExcelAsync(DateTime? from = null, DateTime? to = null)
    {
        var data = await QueryOrders(from, to);
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Orders");

        ws.Cell(1, 1).Value = "Order #";
        ws.Cell(1, 2).Value = "Customer";
        ws.Cell(1, 3).Value = "Status";
        ws.Cell(1, 4).Value = "Total";
        ws.Cell(1, 5).Value = "Created At";

        var row = 2;
        foreach (var item in data)
        {
            ws.Cell(row, 1).Value = item.OrderNumber;
            ws.Cell(row, 2).Value = item.CustomerName;
            ws.Cell(row, 3).Value = item.Status;
            ws.Cell(row, 4).Value = item.TotalAmount;
            ws.Cell(row, 5).Value = item.CreatedAt;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<string> GenerateOrdersCsvAsync(DateTime? from = null, DateTime? to = null)
    {
        var data = await QueryOrders(from, to);
        var sb = new StringBuilder();
        sb.AppendLine("OrderNumber,Customer,Status,TotalAmount,CreatedAt");

        foreach (var item in data)
        {
            sb.AppendLine($"{item.OrderNumber},{Escape(item.CustomerName)},{item.Status},{item.TotalAmount},{item.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        return sb.ToString();
    }

    private static string Escape(string val)
    {
        if (val.Contains(',') || val.Contains('"'))
        {
            return $"\"{val.Replace("\"", "\"\"")}\"";
        }

        return val;
    }

    private async Task<List<OrderReportRow>> QueryOrders(DateTime? from, DateTime? to)
    {
        var q = _db.Orders
            .Include(x => x.Customer)
                .ThenInclude(c => c.User)
            .AsQueryable();

        if (from.HasValue)
        {
            q = q.Where(x => x.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            q = q.Where(x => x.CreatedAt <= to.Value);
        }

        return await q.OrderByDescending(x => x.CreatedAt)
            .Select(x => new OrderReportRow
            {
                OrderNumber = x.OrderNumber,
                CustomerName = x.Customer.User.FirstName + " " + x.Customer.User.LastName,
                Status = x.Status.ToString(),
                TotalAmount = x.TotalAmount,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    private class OrderReportRow
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
