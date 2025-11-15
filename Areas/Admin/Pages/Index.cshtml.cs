using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.Admin.Pages
{
    [AuthorizeRole("Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int NewOrdersThisMonth { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public int PendingOrders { get; set; }
        public int ConfirmedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int FeaturedProducts { get; set; }
        public int AdminUsers { get; set; }
        public int NVKDUsers { get; set; }
        public int NVKhoUsers { get; set; }
        public int NVMKTUsers { get; set; }
        public int CustomerUsers { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public List<Product> TopViewedProducts { get; set; } = new();
        public List<object> MonthlyRevenue { get; set; } = new();
        public List<object> CategoryStats { get; set; } = new();
        public List<object> BrandStats { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var today = DateTime.Today;
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;

                // Thống kê tổng quan hệ thống
                TotalUsers = await _context.Users.CountAsync();
                TotalProducts = await _context.Products.Where(p => p.StatusProduct != "hidden").CountAsync();
                TotalOrders = await _context.Orders.CountAsync();
                TotalRevenue = await _context.Orders
                    .Where(o => o.Status == "Đã giao")
                    .SumAsync(o => o.TotalAmount ?? 0);

                // Thống kê theo thời gian
                NewUsersThisMonth = await _context.Users
                    .Where(u => u.CreatedAt.HasValue && u.CreatedAt.Value.Month == currentMonth && u.CreatedAt.Value.Year == currentYear)
                    .CountAsync();
                NewOrdersThisMonth = await _context.Orders
                    .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Month == currentMonth && o.CreatedAt.Value.Year == currentYear)
                    .CountAsync();
                RevenueThisMonth = await _context.Orders
                    .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Month == currentMonth && o.CreatedAt.Value.Year == currentYear && o.Status == "Đã giao")
                    .SumAsync(o => o.TotalAmount ?? 0);

                // Thống kê đơn hàng theo trạng thái
                PendingOrders = await _context.Orders.Where(o => o.Status == "Chờ xác nhận").CountAsync();
                ConfirmedOrders = await _context.Orders.Where(o => o.Status == "Đã xác nhận").CountAsync();
                DeliveredOrders = await _context.Orders.Where(o => o.Status == "Đã giao").CountAsync();
                CancelledOrders = await _context.Orders.Where(o => o.Status == "Đã hủy").CountAsync();

                // Thống kê sản phẩm
                OutOfStockProducts = await _context.Products.Where(p => p.Quantity <= 0 && p.StatusProduct != "hidden").CountAsync();
                LowStockProducts = await _context.Products.Where(p => p.Quantity <= 10 && p.Quantity > 0 && p.StatusProduct != "hidden").CountAsync();
                FeaturedProducts = await _context.Products.Where(p => p.IsFeatured == true && p.StatusProduct != "hidden").CountAsync();

                // Thống kê người dùng theo vai trò
                AdminUsers = await _context.Users.Where(u => u.Role == "Admin").CountAsync();
                NVKDUsers = await _context.Users.Where(u => u.Role == "NVKD").CountAsync();
                NVKhoUsers = await _context.Users.Where(u => u.Role == "NVKho").CountAsync();
                NVMKTUsers = await _context.Users.Where(u => u.Role == "NVMKT").CountAsync();
                CustomerUsers = await _context.Users.Where(u => u.Role == "Customer").CountAsync();

                // Đơn hàng gần đây
                RecentOrders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Address)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Sản phẩm bán chạy (theo lượt xem)
                TopViewedProducts = await _context.Products
                    .Where(p => p.StatusProduct != "hidden")
                    .OrderByDescending(p => p.ViewCount)
                    .Take(5)
                    .ToListAsync();

                // Thống kê doanh thu theo tháng (6 tháng gần nhất)
                MonthlyRevenue = new List<object>();
                for (int i = 5; i >= 0; i--)
                {
                    var month = DateTime.Now.AddMonths(-i);
                    var revenue = await _context.Orders
                        .Where(o => o.CreatedAt.HasValue && 
                                   o.CreatedAt.Value.Month == month.Month && 
                                   o.CreatedAt.Value.Year == month.Year && 
                                   o.Status == "Đã giao")
                        .SumAsync(o => o.TotalAmount ?? 0);
                    MonthlyRevenue.Add(new { Month = month.ToString("MM/yyyy"), Revenue = revenue });
                }

                // Thống kê danh mục sản phẩm
                CategoryStats = await _context.Products
                    .Include(p => p.Cate)
                    .Where(p => p.Cate != null && p.StatusProduct != "hidden")
                    .GroupBy(p => p.Cate!.CategoryName)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .Cast<object>()
                    .Take(5)
                    .ToListAsync();

                // Thống kê thương hiệu
                BrandStats = await _context.Products
                    .Include(p => p.Brand)
                    .Where(p => p.Brand != null && p.StatusProduct != "hidden")
                    .GroupBy(p => p.Brand!.NameBrand)
                    .Select(g => new { Brand = g.Key, Count = g.Count() })
                    .Cast<object>()
                    .Take(5)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                ErrorMessage = "Có lỗi xảy ra khi tải dữ liệu: " + ex.Message;
                return Page();
            }
        }
    }
}

