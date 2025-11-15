using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.NVKD.Pages
{
    [AuthorizeRole("NVKD", "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public int NewOrdersToday { get; set; }
        public int ConfirmedOrdersToday { get; set; }
        public int NewOrdersThisMonth { get; set; }
        public int ConfirmedOrdersThisMonth { get; set; }
        public decimal TotalRevenueThisMonth { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public List<object> OrderStatusStats { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var today = DateTime.Today;
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                // Đếm đơn hàng mới hôm nay (Chờ xác nhận)
                NewOrdersToday = await _context.Orders
                    .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Date == today && o.Status == "Chờ xác nhận")
                    .CountAsync();

                // Đếm đơn hàng đã xác nhận hôm nay (Đã xác nhận)
                ConfirmedOrdersToday = await _context.Orders
                    .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Date == today && o.Status == "Đã xác nhận")
                    .CountAsync();

                // Đếm đơn hàng mới trong tháng (Chờ xác nhận)
                NewOrdersThisMonth = await _context.Orders
                    .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Month == currentMonth && o.CreatedAt.Value.Year == currentYear && o.Status == "Chờ xác nhận")
                    .CountAsync();

                // Đếm đơn hàng đã xác nhận trong tháng (Đã xác nhận)
                ConfirmedOrdersThisMonth = await _context.Orders
                    .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Month == currentMonth && o.CreatedAt.Value.Year == currentYear && o.Status == "Đã xác nhận")
                    .CountAsync();

                // Tính tổng doanh thu tháng này (từ đơn hàng đã xác nhận)
                TotalRevenueThisMonth = await _context.Orders
                    .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Month == currentMonth && o.CreatedAt.Value.Year == currentYear && o.Status == "Đã xác nhận")
                    .SumAsync(o => o.TotalAmount ?? 0);

                // Lấy 5 đơn hàng mới nhất (Chờ xác nhận)
                RecentOrders = await _context.Orders
                    .Include(o => o.User)
                    .Where(o => o.Status == "Chờ xác nhận")
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Lấy thống kê đơn hàng theo trạng thái
                OrderStatusStats = await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key ?? "N/A", Count = g.Count() })
                    .Cast<object>()
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading NVKD dashboard data");
                ErrorMessage = "Có lỗi xảy ra khi tải dữ liệu: " + ex.Message;
                return Page();
            }
        }
    }
}

