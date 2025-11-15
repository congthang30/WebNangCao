using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.NVKD.Pages.Orders
{
    [AuthorizeRole("NVKD", "Admin")]
    public class ListOrderConfirmedModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ListOrderConfirmedModel> _logger;

        public ListOrderConfirmedModel(ApplicationDbContext context, ILogger<ListOrderConfirmedModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Order> Orders { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Orders = await _context.Orders
                    .Where(o => o.Status == "Đã xác nhận")
                    .Include(o => o.User)
                    .Include(o => o.Address)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading confirmed orders");
                ErrorMessage = "Có lỗi hệ thống";
                return Page();
            }
        }
    }
}

