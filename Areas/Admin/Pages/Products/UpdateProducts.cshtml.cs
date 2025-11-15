using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.Method;

namespace NewWeb.Areas.Admin.Pages.Products
{
    [AuthorizeRole("Admin")]
    public class UpdateProductsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UpdateProductsModel> _logger;

        public UpdateProductsModel(ApplicationDbContext context, ILogger<UpdateProductsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Product Product { get; set; } = default!;

        [BindProperty]
        public string? ProductName { get; set; }

        [BindProperty]
        public string? Barcode { get; set; }

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public int? Quantity { get; set; }

        [BindProperty]
        public decimal? ImportPrice { get; set; }

        [BindProperty]
        public decimal? SellingPrice { get; set; }

        [BindProperty]
        public string? StatusProduct { get; set; }

        [BindProperty]
        public decimal? Discount { get; set; }

        [BindProperty]
        public bool? IsFeatured { get; set; }

        [BindProperty]
        public IFormFile? ImagePr { get; set; }

        [BindProperty]
        public int? BrandId { get; set; }

        [BindProperty]
        public int? CateId { get; set; }

        public List<SelectListItem> Brands { get; set; } = new();
        public List<SelectListItem> Categories { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Product = await _context.Products.FindAsync(id);
            if (Product == null)
            {
                return NotFound();
            }

            await LoadSelectListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            try
            {
                Product = await _context.Products.FindAsync(id);
                if (Product == null)
                {
                    return NotFound();
                }

                if (!string.IsNullOrWhiteSpace(ProductName))
                    Product.ProductName = ProductName;

                if (!string.IsNullOrWhiteSpace(Barcode))
                {
                    if (await _context.Products.AnyAsync(p => p.Barcode == Barcode && p.ProductId != id))
                    {
                        ErrorMessage = "Barcode đã tồn tại trong hệ thống.";
                        await LoadSelectListsAsync();
                        return Page();
                    }
                    Product.Barcode = Barcode;
                }

                if (!string.IsNullOrWhiteSpace(Description))
                    Product.Description = Description;

                if (Quantity.HasValue)
                    Product.Quantity = Quantity.Value;

                if (ImportPrice.HasValue)
                    Product.ImportPrice = ImportPrice.Value;

                if (SellingPrice.HasValue)
                    Product.SellingPrice = SellingPrice.Value;

                if (!string.IsNullOrWhiteSpace(StatusProduct))
                    Product.StatusProduct = StatusProduct;

                if (Discount.HasValue)
                    Product.Discount = Discount.Value;

                if (IsFeatured.HasValue)
                    Product.IsFeatured = IsFeatured.Value;

                if (BrandId.HasValue)
                    Product.BrandId = BrandId.Value;

                if (CateId.HasValue)
                    Product.CateId = CateId.Value;

                if (ImagePr != null && ImagePr.Length > 0)
                {
                    try
                    {
                        Product.ImagePr = await ImageHelper.SaveImageAsync(ImagePr, "products");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving product image");
                        ErrorMessage = "Lỗi khi upload ảnh: " + ex.Message;
                        await LoadSelectListsAsync();
                        return Page();
                    }
                }

                Product.UpdatedAt = DateTime.Now;
                _context.Products.Update(Product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToPage("ListProducts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                ErrorMessage = "Có lỗi xảy ra: " + ex.Message;
                await LoadSelectListsAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
                return RedirectToPage("ListProducts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                TempData["ErrorMessage"] = "Không thể xóa sản phẩm: " + ex.Message;
                return RedirectToPage("ListProducts");
            }
        }

        private async Task LoadSelectListsAsync()
        {
            Brands = await _context.Brands
                .Select(b => new SelectListItem { Value = b.BrandId.ToString(), Text = b.NameBrand })
                .ToListAsync();

            Categories = await _context.Categories
                .Select(c => new SelectListItem { Value = c.CateId.ToString(), Text = c.CategoryName })
                .ToListAsync();
        }
    }
}

