using eDhaq.Common.DTOs;
using eDhaq.Data;
using eDhaq.Models.Enums;
using eDhaq.Repositories.Interfaces;
using eDhaq.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Customer.Orders;

[Authorize]
public class CreateModel : PageModel
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IOrderService _orderService;
    private readonly AppDbContext _db;

    public CreateModel(ICustomerRepository customerRepository, IOrderService orderService, AppDbContext db)
    {
        _customerRepository = customerRepository;
        _orderService = orderService;
        _db = db;
    }

    [BindProperty]
    public CreateOrderDto Input { get; set; } = new()
    {
        PickupScheduledAt = DateTime.UtcNow.AddHours(2),
        DeliveryScheduledAt = DateTime.UtcNow.AddDays(1),
        PaymentMethod = PaymentMethod.Cash,
        Items = [new CreateOrderItemDto { Quantity = 1 }]
    };

    public List<SelectListItem> AddressOptions { get; private set; } = [];
    public List<SelectListItem> ServiceOptions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadOptionsAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var customer = await _customerRepository.GetByUserIdAsync(userId);
        if (customer is null)
        {
            return NotFound();
        }

        if (Input.Items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "At least one order item is required.");
            return Page();
        }

        var order = await _orderService.CreateOrderAsync(customer.Id, Input, userId);
        TempData["SuccessMessage"] = $"Order {order.OrderNumber} created successfully.";
        return RedirectToPage("Index");
    }

    private async Task LoadOptionsAsync()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var customer = string.IsNullOrWhiteSpace(userId)
            ? null
            : await _customerRepository.GetByUserIdAsync(userId);

        if (customer is not null)
        {
            var addresses = await _db.Addresses
                .Where(x => x.CustomerId == customer.Id)
                .Include(x => x.Village)
                .Include(x => x.SubVillage)
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Label)
                .ToListAsync();

            AddressOptions = addresses
                .Select(x => new SelectListItem($"{x.Label} - {x.Street} ({x.Village.Name}{(x.SubVillage is not null ? $"/{x.SubVillage.Name}" : string.Empty)})", x.Id.ToString()))
                .ToList();
        }

        ServiceOptions = await _db.LaundryServices
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => new SelectListItem($"{x.Name} ({x.PricePerPiece:C})", x.Id.ToString()))
            .ToListAsync();
    }
}
