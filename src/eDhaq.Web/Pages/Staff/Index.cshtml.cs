using eDhaq.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace eDhaq.Web.Pages.Staff;

[Authorize(Roles = "Administrator,LaundryStaff")]
public class IndexModel : PageModel
{
    private readonly IOrderRepository _orderRepository;

    public IndexModel(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public int IncomingOrders { get; private set; }

    public async Task OnGetAsync()
    {
        var orders = await _orderRepository.GetTodaysOrdersAsync();
        IncomingOrders = orders.Count();
    }
}
