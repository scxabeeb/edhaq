using eDhaq.Data;
using eDhaq.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace eDhaq.Web.Pages.Admin.Customers;

[Authorize(Roles = "Administrator,Manager")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<CustomerRow> Customers { get; private set; } = [];

    // Summary stats
    public int TotalCustomers { get; private set; }
    public int ActiveCustomers { get; private set; }
    public decimal TotalRevenue { get; private set; }
    public int TotalOrders { get; private set; }

    // Search + paging (read from query string only)
    public string SearchTerm { get; private set; } = string.Empty;
    public int PageNumber { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int TotalCount { get; private set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public async Task OnGetAsync()
    {
        if (int.TryParse(Request.Query["pageNumber"], out var p)) PageNumber = Math.Max(1, p);
        SearchTerm = (Request.Query["q"].ToString() ?? string.Empty).Trim();

        var query = _db.Customers
            .Include(c => c.User)
            .Include(c => c.Orders)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var s = SearchTerm.ToLower();
            query = query.Where(c =>
                c.User.FirstName.ToLower().Contains(s) ||
                c.User.LastName.ToLower().Contains(s) ||
                (c.User.Email != null && c.User.Email.ToLower().Contains(s)) ||
                (c.User.PhoneNumber != null && c.User.PhoneNumber.Contains(s)));
        }

        var all = await query.ToListAsync();

        // Summary (over the filtered set)
        TotalCustomers = all.Count;
        ActiveCustomers = all.Count(c => c.User.IsActive);
        TotalOrders = all.Sum(c => c.Orders.Count);
        TotalRevenue = all.Sum(c => c.Orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .Sum(o => o.TotalAmount));

        TotalCount = all.Count;

        Customers = all
            .OrderByDescending(c => c.Orders.Sum(o => o.TotalAmount))
            .ThenBy(c => c.User.FirstName)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .Select(c => new CustomerRow
            {
                CustomerId = c.Id,
                UserId = c.UserId,
                FullName = $"{c.User.FirstName} {c.User.LastName}".Trim(),
                Email = c.User.Email ?? string.Empty,
                Phone = c.User.PhoneNumber,
                IsActive = c.User.IsActive,
                WalletBalance = c.WalletBalance,
                ReferralCode = c.ReferralCode,
                JoinedAt = c.CreatedAt,
                OrderCount = c.Orders.Count,
                CompletedOrders = c.Orders.Count(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.CustomerConfirmed),
                CancelledOrders = c.Orders.Count(o => o.Status == OrderStatus.Cancelled),
                TotalSpent = c.Orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
                LastOrderAt = c.Orders.OrderByDescending(o => o.CreatedAt).Select(o => (DateTime?)o.CreatedAt).FirstOrDefault()
            })
            .ToList();
    }

    public class CustomerRow
    {
        public int CustomerId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public decimal WalletBalance { get; set; }
        public string? ReferralCode { get; set; }
        public DateTime JoinedAt { get; set; }
        public int OrderCount { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastOrderAt { get; set; }
    }
}
