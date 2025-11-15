using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.Admin.Pages.Products
{
    [AuthorizeRole("Admin")]
    public class ListProductsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ListProductsModel> _logger;

        public ListProductsModel(ApplicationDbContext context, ILogger<ListProductsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Product> Products { get; set; } = new();
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int HiddenProducts { get; set; }
        public int OutOfStockProducts { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Products = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Cate)
                    .OrderByDescending(p => p.UpdatedAt)
                    .ToListAsync();

                TotalProducts = await _context.Products.CountAsync();
                ActiveProducts = await _context.Products.Where(p => p.StatusProduct == "presently").CountAsync();
                HiddenProducts = await _context.Products.Where(p => p.StatusProduct == "hidden").CountAsync();
                OutOfStockProducts = await _context.Products.Where(p => p.Quantity <= 0).CountAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products list");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách sản phẩm: " + ex.Message;
                Products = new List<Product>();
                return Page();
            }
        }
    }
}

