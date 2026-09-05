using eDhaq.Models.Enums;
using eDhaq.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Staff;

[Authorize(Roles = "Administrator,LaundryStaff")]
public class IndexModel : PageModel
{
    private static readonly OrderStatus[] ProcessingStages =
    [
        OrderStatus.LaundryReceived,
        OrderStatus.Sorting,
        OrderStatus.Washing,
        OrderStatus.DryCleaning,
        OrderStatus.Drying,
        OrderStatus.Ironing,
        OrderStatus.Folding,
        OrderStatus.Packaging
    ];

    private readonly IOrderRepository _orderRepository;
    private readonly Data.AppDbContext _db;

    public IndexModel(IOrderRepository orderRepository, Data.AppDbContext db)
    {
        _orderRepository = orderRepository;
        _db = db;
    }

    public int IncomingOrders { get; private set; }
    public int AwaitingIntake { get; private set; }
    public int InProcessing { get; private set; }
    public int ReadyForDelivery { get; private set; }
    public Dictionary<OrderStatus, int> WorkflowStages { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var todayOrders = await _orderRepository.GetTodaysOrdersAsync();
        IncomingOrders = todayOrders.Count();

        var statuses = ProcessingStages
            .Concat([OrderStatus.ReadyForDelivery])
            .ToArray();

        var counts = await _db.Orders
            .Where(x => statuses.Contains(x.Status) || x.Status == OrderStatus.ClothesPickedUp)
            .GroupBy(x => x.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        AwaitingIntake = counts.GetValueOrDefault(OrderStatus.ClothesPickedUp);
        InProcessing = ProcessingStages.Sum(s => counts.GetValueOrDefault(s));
        ReadyForDelivery = counts.GetValueOrDefault(OrderStatus.ReadyForDelivery);

        WorkflowStages = statuses.ToDictionary(s => s, s => counts.GetValueOrDefault(s));
    }
}
