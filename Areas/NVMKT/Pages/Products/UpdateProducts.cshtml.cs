using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.Method;

namespace NewWeb.Areas.NVMKT.Pages.Products
{
    [AuthorizeRole("NVMKT", "Admin")]
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

                await LoadSelectListsAsync();

                if (ProductName != null)
                    Product.ProductName = ProductName;
                if (Barcode != null)
                    Product.Barcode = Barcode;
                if (Description != null)
                    Product.Description = Description;
                if (Quantity != null)
                    Product.Quantity = Quantity;
                if (ImportPrice != null)
                    Product.ImportPrice = ImportPrice;
                if (SellingPrice != null)
                    Product.SellingPrice = SellingPrice;
                if (StatusProduct != null)
                    Product.StatusProduct = StatusProduct;
                if (Discount != null)
                    Product.Discount = Discount;
                if (IsFeatured != null)
                    Product.IsFeatured = IsFeatured.Value;
                if (BrandId != null)
                    Product.BrandId = BrandId.Value;
                if (CateId != null)
                    Product.CateId = CateId.Value;

                if (ImagePr != null && ImagePr.Length > 0)
                {
                    try
                    {
                        Product.ImagePr = await ImageHelper.SaveImageAsync(ImagePr, "products");
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = "Lỗi ảnh: " + ex.Message;
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

