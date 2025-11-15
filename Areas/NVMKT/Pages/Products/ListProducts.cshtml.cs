using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.NVMKT.Pages.Products
{
    [AuthorizeRole("NVMKT", "Admin")]
    public class ListProductsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ListProductsModel> _logger;

        public ListProductsModel(ApplicationDbContext context, ILogger<ListProductsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string? ProductName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Barcode { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Description { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Quantity { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? ImportPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? SellingPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusProduct { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? Discount { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsFeatured { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BrandId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CateId { get; set; }

        public List<Product> Products { get; set; } = new();
        public int TotalProducts { get; set; }
        public int FeaturedProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Brands { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Cate)
                    .Where(p => p.StatusProduct != "hidden")
                    .AsQueryable();

                // Filter theo tên sản phẩm
                if (!string.IsNullOrWhiteSpace(ProductName))
                    query = query.Where(p => p.ProductName != null && p.ProductName.Contains(ProductName));

                // Filter theo barcode
                if (!string.IsNullOrWhiteSpace(Barcode))
                    query = query.Where(p => p.Barcode != null && p.Barcode.Contains(Barcode));

                // Filter theo mô tả
                if (!string.IsNullOrWhiteSpace(Description))
                    query = query.Where(p => p.Description != null && p.Description.Contains(Description));

                // Filter theo số lượng
                if (Quantity.HasValue)
                    query = query.Where(p => p.Quantity == Quantity.Value);

                // Filter theo giá nhập
                if (ImportPrice.HasValue)
                    query = query.Where(p => p.ImportPrice == ImportPrice.Value);

                // Filter theo giá bán
                if (SellingPrice.HasValue)
                    query = query.Where(p => p.SellingPrice == SellingPrice.Value);

                // Filter theo trạng thái sản phẩm
                if (!string.IsNullOrWhiteSpace(StatusProduct))
                    query = query.Where(p => p.StatusProduct == StatusProduct);

                // Filter theo giảm giá
                if (Discount.HasValue)
                    query = query.Where(p => p.Discount == Discount.Value);

                // Filter theo sản phẩm nổi bật
                if (IsFeatured.HasValue)
                    query = query.Where(p => p.IsFeatured == IsFeatured.Value);

                // Filter theo thương hiệu
                if (BrandId.HasValue && BrandId.Value > 0)
                    query = query.Where(p => p.BrandId == BrandId.Value);

                // Filter theo danh mục
                if (CateId.HasValue && CateId.Value > 0)
                    query = query.Where(p => p.CateId == CateId.Value);

                // Sắp xếp theo thời gian cập nhật mới nhất
                Products = await query
                    .OrderByDescending(p => p.UpdatedAt)
                    .ToListAsync();

                // Thống kê nhanh
                TotalProducts = await _context.Products.Where(p => p.StatusProduct != "hidden").CountAsync();
                FeaturedProducts = await _context.Products.Where(p => p.IsFeatured == true && p.StatusProduct != "hidden").CountAsync();
                OutOfStockProducts = await _context.Products.Where(p => (p.Quantity ?? 0) <= 0 && p.StatusProduct != "hidden").CountAsync();
                LowStockProducts = await _context.Products.Where(p => (p.Quantity ?? 0) <= 10 && (p.Quantity ?? 0) > 0 && p.StatusProduct != "hidden").CountAsync();

                // Load danh sách categories và brands cho dropdown
                Categories = await _context.Categories
                    .Select(c => new SelectListItem { Value = c.CateId.ToString(), Text = c.CategoryName })
                    .ToListAsync();

                Brands = await _context.Brands
                    .Select(b => new SelectListItem { Value = b.BrandId.ToString(), Text = b.NameBrand })
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products list");
                ErrorMessage = "Có lỗi xảy ra khi tải danh sách sản phẩm: " + ex.Message;
                Products = new List<Product>();
                await LoadSelectListsAsync();
                return Page();
            }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                product.StatusProduct = product.StatusProduct == "presently" ? "hidden" : "presently";
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Thay đổi trạng thái sản phẩm thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling product status");
                return new JsonResult(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        private async Task LoadSelectListsAsync()
        {
            Categories = await _context.Categories
                .Select(c => new SelectListItem { Value = c.CateId.ToString(), Text = c.CategoryName })
                .ToListAsync();

            Brands = await _context.Brands
                .Select(b => new SelectListItem { Value = b.BrandId.ToString(), Text = b.NameBrand })
                .ToListAsync();
        }
    }
}

