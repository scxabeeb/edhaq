using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eDhaq.Web.Pages.Admin.Reports;

[Authorize(Roles = "Administrator,Manager")]
public class IndexModel : PageModel
{
    private readonly IReportService _reportService;

    public IndexModel(IReportService reportService)
    {
        _reportService = reportService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostExportPdfAsync()
    {
        var file = await _reportService.GenerateOrdersPdfAsync(From, To);
        return File(file, "application/pdf", $"orders-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
    }

    public async Task<IActionResult> OnPostExportExcelAsync()
    {
        var file = await _reportService.GenerateOrdersExcelAsync(From, To);
        return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"orders-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }

    public async Task<IActionResult> OnPostExportCsvAsync()
    {
        var csv = await _reportService.GenerateOrdersCsvAsync(From, To);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"orders-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}
