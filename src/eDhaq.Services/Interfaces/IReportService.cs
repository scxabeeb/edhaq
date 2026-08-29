namespace eDhaq.Services.Interfaces;

public interface IReportService
{
    Task<byte[]> GenerateOrdersPdfAsync(DateTime? from = null, DateTime? to = null);
    Task<byte[]> GenerateOrdersExcelAsync(DateTime? from = null, DateTime? to = null);
    Task<string> GenerateOrdersCsvAsync(DateTime? from = null, DateTime? to = null);
}
