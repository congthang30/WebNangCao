using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Data;
using NewWeb.ViewModels;

namespace NewWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public HomeViewModel ViewModel { get; set; } = new HomeViewModel();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Cate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? BrandId { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MinPrice { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal? MaxPrice { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Load categories and brands for filter
                ViewModel.Categories = await _context.Categories
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();
                
                ViewModel.Brands = await _context.Brands
                    .OrderBy(b => b.NameBrand)
                    .ToListAsync();

                // Build product query
                var productsQuery = _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Cate)
                    .Where(p => p.StatusProduct != "hidden" && p.Quantity > 0)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(Search))
                {
                    productsQuery = productsQuery.Where(p => 
                        p.ProductName!.Contains(Search) || 
                        p.Description!.Contains(Search));
                    ViewModel.SearchQuery = Search;
                }

                // Apply category filter
                if (Cate.HasValue)
                {
                    productsQuery = productsQuery.Where(p => p.CateId == Cate.Value);
                    ViewModel.SelectedCategoryId = Cate;
                }

                // Apply brand filter
                if (BrandId.HasValue)
                {
                    productsQuery = productsQuery.Where(p => p.BrandId == BrandId.Value);
                    ViewModel.SelectedBrandId = BrandId;
                }

                // Apply minimum price filter
                if (MinPrice.HasValue)
                {
                    productsQuery = productsQuery.Where(p => p.SellingPrice >= MinPrice.Value);
                    ViewModel.MinPrice = MinPrice;
                }

                // Apply maximum price filter
                if (MaxPrice.HasValue)
                {
                    productsQuery = productsQuery.Where(p => p.SellingPrice <= MaxPrice.Value);
                    ViewModel.MaxPrice = MaxPrice;
                }

                // Get total count before pagination
                ViewModel.TotalProducts = await productsQuery.CountAsync();
                ViewModel.FeaturedProductsCount = await productsQuery
                    .CountAsync(p => p.IsFeatured == true);

                // Get products (show featured first, then by date)
                ViewModel.Products = await productsQuery
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenByDescending(p => p.UpdatedAt)
                    .Take(20) // Limit to 20 products for better performance
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page");
                TempData["Error"] = "Có lỗi hệ thống. Vui lòng thử lại sau.";
                return Page();
            }
        }
    }
}
