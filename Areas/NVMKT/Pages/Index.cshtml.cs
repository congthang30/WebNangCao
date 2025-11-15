using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.NVMKT.Pages
{
    [AuthorizeRole("NVMKT", "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public int TotalProducts { get; set; }
        public int FeaturedProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public List<Product> TopViewedProducts { get; set; } = new();
        public List<Product> FeaturedProductsList { get; set; } = new();
        public List<Product> LowStockProductsList { get; set; } = new();
        public List<object> CategoryStats { get; set; } = new();
        public List<object> BrandStats { get; set; } = new();
        public List<object> OrderStatusStats { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Đếm tổng số sản phẩm
                TotalProducts = await _context.Products
                    .Where(p => p.StatusProduct != "hidden")
                    .CountAsync();

                // Đếm sản phẩm nổi bật
                FeaturedProducts = await _context.Products
                    .Where(p => p.IsFeatured == true && p.StatusProduct != "hidden")
                    .CountAsync();

                // Đếm sản phẩm hết hàng
                OutOfStockProducts = await _context.Products
                    .Where(p => (p.Quantity ?? 0) <= 0 && p.StatusProduct != "hidden")
                    .CountAsync();

                // Đếm sản phẩm sắp hết hàng (≤ 10)
                LowStockProducts = await _context.Products
                    .Where(p => (p.Quantity ?? 0) <= 10 && (p.Quantity ?? 0) > 0 && p.StatusProduct != "hidden")
                    .CountAsync();

                // Lấy 5 sản phẩm có lượt xem cao nhất
                TopViewedProducts = await _context.Products
                    .Where(p => p.StatusProduct != "hidden")
                    .OrderByDescending(p => p.ViewCount)
                    .Take(5)
                    .ToListAsync();

                // Lấy 5 sản phẩm nổi bật
                FeaturedProductsList = await _context.Products
                    .Where(p => p.IsFeatured == true && p.StatusProduct != "hidden")
                    .Take(5)
                    .ToListAsync();

                // Lấy 5 sản phẩm sắp hết hàng
                LowStockProductsList = await _context.Products
                    .Where(p => (p.Quantity ?? 0) <= 10 && (p.Quantity ?? 0) > 0 && p.StatusProduct != "hidden")
                    .OrderBy(p => p.Quantity)
                    .Take(5)
                    .ToListAsync();

                // Thống kê sản phẩm theo danh mục
                CategoryStats = await _context.Products
                    .Include(p => p.Cate)
                    .Where(p => p.Cate != null && p.StatusProduct != "hidden")
                    .GroupBy(p => p.Cate!.CategoryName)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .Cast<object>()
                    .Take(5)
                    .ToListAsync();

                // Thống kê sản phẩm theo thương hiệu
                BrandStats = await _context.Products
                    .Include(p => p.Brand)
                    .Where(p => p.Brand != null && p.StatusProduct != "hidden")
                    .GroupBy(p => p.Brand!.NameBrand)
                    .Select(g => new { Brand = g.Key, Count = g.Count() })
                    .Cast<object>()
                    .Take(5)
                    .ToListAsync();

                // Thống kê đơn hàng theo trạng thái
                OrderStatusStats = await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key ?? "N/A", Count = g.Count() })
                    .Cast<object>()
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading NVMKT dashboard data");
                ErrorMessage = "Có lỗi xảy ra khi tải dữ liệu: " + ex.Message;
                return Page();
            }
        }
    }
}

