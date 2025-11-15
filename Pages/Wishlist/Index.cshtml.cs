using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Pages.Wishlist
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

        public List<Product> WishlistProducts { get; set; } = new List<Product>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                if (!userId.HasValue)
                {
                    TempData["Error"] = "Vui lòng đăng nhập để xem danh sách yêu thích.";
                    return RedirectToPage("/Account/Login");
                }

                var wishlistProductIds = await _context.Wishlists
                    .Where(w => w.UserId == userId)
                    .Select(w => w.ProductId)
                    .ToListAsync();

                WishlistProducts = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Cate)
                    .Where(p => wishlistProductIds.Contains(p.ProductId))
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading wishlist");
                TempData["Error"] = "Có lỗi hệ thống. Vui lòng thử lại sau.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAddWishListAsync(int productId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                if (!userId.HasValue)
                {
                    TempData["Error"] = "Vui lòng đăng nhập để sử dụng tính năng này.";
                    return RedirectToPage("/Account/Login");
                }

                var existingItem = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (existingItem == null)
                {
                    var wishlist = new Models.Wishlist
                    {
                        ProductId = productId,
                        UserId = userId.Value,
                        CreatedAt = DateTime.Now,
                    };
                    _context.Wishlists.Add(wishlist);
                    TempData["Success"] = "Đã thêm vào yêu thích!";
                }
                else
                {
                    _context.Wishlists.Remove(existingItem);
                    TempData["Success"] = "Đã xóa khỏi yêu thích!";
                }

                await _context.SaveChangesAsync();

                // Redirect về trang trước đó
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    return Redirect(referer);
                }
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding/removing wishlist item");
                TempData["Error"] = "Có lỗi hệ thống. Vui lòng thử lại sau.";
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    return Redirect(referer);
                }
                return RedirectToPage("/Index");
            }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostAddWishListAjaxAsync(int productId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                if (!userId.HasValue)
                {
                    return new JsonResult(new { 
                        success = false, 
                        message = "Vui lòng đăng nhập để sử dụng tính năng này", 
                        requireLogin = true 
                    });
                }

                var existingItem = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                bool added = false;
                if (existingItem == null)
                {
                    var wishlist = new Models.Wishlist
                    {
                        ProductId = productId,
                        UserId = userId.Value,
                        CreatedAt = DateTime.Now,
                    };
                    _context.Wishlists.Add(wishlist);
                    added = true;
                }
                else
                {
                    _context.Wishlists.Remove(existingItem);
                    added = false;
                }

                await _context.SaveChangesAsync();

                return new JsonResult(new { 
                    success = true, 
                    added = added,
                    message = added ? "Đã thêm vào yêu thích!" : "Đã xóa khỏi yêu thích!"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddWishListAjax");
                return new JsonResult(new { 
                    success = false, 
                    message = "Có lỗi hệ thống. Vui lòng thử lại sau" 
                });
            }
        }

        public async Task<IActionResult> OnPostRemoveWishListAsync(int productId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                if (!userId.HasValue)
                {
                    TempData["Error"] = "Vui lòng đăng nhập.";
                    return RedirectToPage("/Account/Login");
                }

                var wishlistItem = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (wishlistItem != null)
                {
                    _context.Wishlists.Remove(wishlistItem);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Đã xóa khỏi yêu thích!";
                }

                // Redirect về trang trước đó
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    return Redirect(referer);
                }
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing wishlist item");
                TempData["Error"] = "Có lỗi hệ thống. Vui lòng thử lại sau.";
                var referer = Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    return Redirect(referer);
                }
                return RedirectToPage();
            }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnGetCheckWishlistStatusAsync(int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                return new JsonResult(new { inWishlist = false });
            }

            var existingItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            return new JsonResult(new { inWishlist = existingItem != null });
        }
    }
}

