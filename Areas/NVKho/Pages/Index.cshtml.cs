using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.NVKho.Pages
{
    [AuthorizeRole("NVKho", "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public int PendingOrdersToday { get; set; }
        public int ProcessedOrdersToday { get; set; }
        public int PendingOrdersThisMonth { get; set; }
        public int ProcessedOrdersThisMonth { get; set; }
        public decimal TotalRevenueThisMonth { get; set; }
        public List<Order> RecentPendingOrders { get; set; } = new();
        public List<Product> LowStockProducts { get; set; } = new();
        public List<object> OrderStatusStats { get; set; } = new();
        public List<object> CategoryStats { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var today = DateTime.Today;
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                // Đếm đơn hàng chờ xử lý hôm nay
                PendingOrdersToday = await _context.Orders
                    .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Date == today && o.Status == "Đã xác nhận")
                    .CountAsync();

                // Đếm đơn hàng đã xử lý hôm nay
                ProcessedOrdersToday = await _context.Orders
                    .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Date == today && o.Status == "Đã giao")
                    .CountAsync();

                // Đếm đơn hàng chờ xử lý trong tháng
                PendingOrdersThisMonth = await _context.Orders
                    .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Month == currentMonth && o.CreatedAt.Value.Year == currentYear && o.Status == "Đã xác nhận")
                    .CountAsync();

                // Đếm đơn hàng đã xử lý trong tháng
                ProcessedOrdersThisMonth = await _context.Orders
                    .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Month == currentMonth && o.CreatedAt.Value.Year == currentYear && o.Status == "Đã giao")
                    .CountAsync();

                // Tính tổng doanh thu tháng này (từ đơn hàng đã giao)
                TotalRevenueThisMonth = await _context.Orders
                    .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Month == currentMonth && o.CreatedAt.Value.Year == currentYear && o.Status == "Đã giao")
                    .SumAsync(o => o.TotalAmount ?? 0);

                // Lấy 5 đơn hàng chờ xử lý mới nhất
                RecentPendingOrders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Address)
                    .Where(o => o.Status == "Đã xác nhận")
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Lấy thống kê sản phẩm tồn kho
                LowStockProducts = await _context.Products
                    .Where(p => p.Quantity <= 10 && p.StatusProduct != "hidden")
                    .Take(5)
                    .ToListAsync();

                // Lấy thống kê đơn hàng theo trạng thái
                OrderStatusStats = await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key ?? "N/A", Count = g.Count() })
                    .Cast<object>()
                    .ToListAsync();

                // Thống kê sản phẩm theo danh mục
                CategoryStats = await _context.Products
                    .Include(p => p.Cate)
                    .Where(p => p.Cate != null)
                    .GroupBy(p => p.Cate!.CategoryName)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .Cast<object>()
                    .Take(5)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading NVKho dashboard data");
                ErrorMessage = "Có lỗi xảy ra khi tải dữ liệu: " + ex.Message;
                return Page();
            }
        }
    }
}

