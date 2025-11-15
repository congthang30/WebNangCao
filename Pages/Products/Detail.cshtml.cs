using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.ViewModels;
using NewWeb.Method;

namespace NewWeb.Pages.Products
{
    public class DetailModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailModel> _logger;

        public DetailModel(ApplicationDbContext context, ILogger<DetailModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public ProductDetailViewModel ViewModel { get; set; } = new ProductDetailViewModel();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                ViewModel.IsLoggedIn = userId.HasValue;

                // Load product with related data
                var product = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Cate)
                    .Include(p => p.Ratings.Where(r => r.IsApproved == true))
                    .ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm";
                    return RedirectToPage("/Index");
                }

                ViewModel.Product = product;
                ViewModel.ApprovedRatings = product.Ratings.ToList();

                // Check user permissions
                if (userId != null)
                {
                    ViewModel.HasPurchased = await _context.Orders
                        .AnyAsync(o => o.UserId == userId
                                    && o.Status == "Đã giao"
                                    && o.OrderDetails.Any(od => od.ProductId == id));

                    ViewModel.HasRated = await _context.Ratings
                        .AnyAsync(r => r.ProductId == id && r.UserId == userId);
                }

                // Load related products (same category, excluding current product)
                if (product.CateId.HasValue)
                {
                    ViewModel.RelatedProducts = await _context.Products
                        .Where(p => p.CateId == product.CateId 
                                 && p.ProductId != id
                                 && p.StatusProduct != "hidden"
                                 && p.Quantity > 0)
                        .OrderByDescending(p => p.IsFeatured)
                        .Take(4)
                        .ToListAsync();
                }

                // Increment view count
                product.ViewCount = (product.ViewCount ?? 0) + 1;
                await _context.SaveChangesAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details");
                TempData["Error"] = "Có lỗi hệ thống. Vui lòng thử lại sau";
                return RedirectToPage("/Index");
            }
        }

        public async Task<IActionResult> OnPostAddRatingAsync(int productId, int ratingValue, string? comment, IFormFile? image)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (userId == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để đánh giá sản phẩm";
                return RedirectToPage("/Account/Login");
            }

            try
            {
                // Check if user has purchased the product
                var hasPurchased = await _context.Orders
                    .AnyAsync(o => o.UserId == userId
                                && o.Status == "Đã giao"
                                && o.OrderDetails.Any(od => od.ProductId == productId));

                if (!hasPurchased)
                {
                    TempData["Error"] = "Bạn cần mua sản phẩm này trước khi đánh giá";
                    return RedirectToPage("/Products/Detail", new { id = productId });
                }

                // Check if user has already rated
                var existingRating = await _context.Ratings
                    .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

                if (existingRating != null)
                {
                    TempData["Error"] = "Bạn đã đánh giá sản phẩm này rồi";
                    return RedirectToPage("/Products/Detail", new { id = productId });
                }

                // Validate rating value
                if (ratingValue < 1 || ratingValue > 5)
                {
                    TempData["Error"] = "Đánh giá không hợp lệ";
                    return RedirectToPage("/Products/Detail", new { id = productId });
                }

                // Xử lý upload ảnh
                string? imagePath = null;
                try
                {
                    if (image != null && image.Length > 0)
                    {
                        imagePath = await ImageHelper.SaveImageAsync(image, "ratings");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving rating image");
                    TempData["Error"] = $"Lỗi khi upload ảnh: {ex.Message}";
                    return RedirectToPage("/Products/Detail", new { id = productId });
                }

                // Add new rating
                var rating = new Rating
                {
                    ProductId = productId,
                    UserId = userId.Value,
                    Star = ratingValue,
                    Comment = comment,
                    ImagePath = imagePath,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsApproved = false // Waiting for admin approval
                };

                _context.Ratings.Add(rating);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đánh giá của bạn đã được gửi và đang chờ duyệt";
                return RedirectToPage("/Products/Detail", new { id = productId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding rating");
                TempData["Error"] = "Có lỗi xảy ra. Vui lòng thử lại sau";
                return RedirectToPage("/Products/Detail", new { id = productId });
            }
        }
    }
}
