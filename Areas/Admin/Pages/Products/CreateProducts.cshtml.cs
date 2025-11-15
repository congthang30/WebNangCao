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
    public class CreateProductsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateProductsModel> _logger;

        public CreateProductsModel(ApplicationDbContext context, ILogger<CreateProductsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public string ProductName { get; set; } = string.Empty;

        [BindProperty]
        public string Barcode { get; set; } = string.Empty;

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public int Quantity { get; set; }

        [BindProperty]
        public decimal ImportPrice { get; set; }

        [BindProperty]
        public decimal SellingPrice { get; set; }

        [BindProperty]
        public string StatusProduct { get; set; } = "presently";

        [BindProperty]
        public decimal Discount { get; set; }

        [BindProperty]
        public bool IsFeatured { get; set; }

        [BindProperty]
        public IFormFile? ImagePr { get; set; }

        [BindProperty]
        public int BrandId { get; set; }

        [BindProperty]
        public int CateId { get; set; }

        public List<SelectListItem> Brands { get; set; } = new();
        public List<SelectListItem> Categories { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadSelectListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ProductName) || string.IsNullOrWhiteSpace(Barcode) ||
                    Quantity <= 0 || ImportPrice <= 0 || SellingPrice <= 0 ||
                    string.IsNullOrWhiteSpace(StatusProduct) || BrandId <= 0 || CateId <= 0)
                {
                    ErrorMessage = "Vui lòng điền đầy đủ thông tin sản phẩm.";
                    await LoadSelectListsAsync();
                    return Page();
                }

                if (await _context.Products.AnyAsync(p => p.Barcode == Barcode))
                {
                    ErrorMessage = "Barcode đã tồn tại trong hệ thống.";
                    await LoadSelectListsAsync();
                    return Page();
                }

                var product = new Product
                {
                    ProductName = ProductName,
                    Barcode = Barcode,
                    Description = Description,
                    Quantity = Quantity,
                    ImportPrice = ImportPrice,
                    SellingPrice = SellingPrice,
                    StatusProduct = StatusProduct,
                    Discount = Discount,
                    IsFeatured = IsFeatured,
                    BrandId = BrandId,
                    CateId = CateId,
                    UpdatedAt = DateTime.Now
                };

                if (ImagePr != null && ImagePr.Length > 0)
                {
                    try
                    {
                        product.ImagePr = await ImageHelper.SaveImageAsync(ImagePr, "products");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving product image");
                        ErrorMessage = "Lỗi khi upload ảnh: " + ex.Message;
                        await LoadSelectListsAsync();
                        return Page();
                    }
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo sản phẩm thành công!";
                return RedirectToPage("ListProducts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
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

