using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Pages.Cart
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

        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            if (!userId.HasValue)
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem giỏ hàng.";
                return RedirectToPage("/Account/Login");
            }

            try
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.Brand)
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.Cate)
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.IsCheckedOut == false);

                CartItems = cart?.CartItems?.ToList() ?? new List<CartItem>();

                // Calculate totals
                TotalItems = CartItems.Sum(ci => ci.Quantity ?? 0);
                TotalAmount = CartItems.Sum(ci => (ci.Quantity ?? 0) * (ci.UnitPrice ?? 0));

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart for user ID: {UserId}", userId);
                TempData["Error"] = "Có lỗi hệ thống khi tải giỏ hàng. Vui lòng thử lại sau.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAddToCartAsync(int productId, int quantity = 1)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");

                if (!userId.HasValue)
                {
                    TempData["Error"] = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng.";
                    return RedirectToPage("/Account/Login");
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.IsCheckedOut == false);

                if (cart == null)
                {
                    cart = new Models.Cart
                    {
                        UserId = userId.Value,
                        IsCheckedOut = false,
                        CreatedAt = DateTime.Now
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm.";
                    return RedirectToPage("/Index");
                }

                // Kiểm tra tồn kho
                if (product.Quantity <= 0)
                {
                    TempData["Error"] = "Sản phẩm đã hết hàng.";
                    return RedirectToPage("/Products/Detail", new { id = productId });
                }

                int addQuantity = quantity > 0 ? quantity : 1;

                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == cart.CartId && ci.ProductId == productId);

                if (existingItem != null)
                {
                    // Kiểm tra số lượng tối đa
                    if ((existingItem.Quantity ?? 0) + addQuantity > product.Quantity)
                    {
                        TempData["Error"] = $"Không đủ hàng trong kho. Còn lại: {product.Quantity}";
                        return RedirectToPage("/Products/Detail", new { id = productId });
                    }
                    existingItem.Quantity = (existingItem.Quantity ?? 0) + addQuantity;
                }
                else
                {
                    if (addQuantity > product.Quantity)
                    {
                        TempData["Error"] = $"Không đủ hàng trong kho. Còn lại: {product.Quantity}";
                        return RedirectToPage("/Products/Detail", new { id = productId });
                    }

                    var cartItem = new CartItem
                    {
                        CartId = cart.CartId,
                        ProductId = productId,
                        Quantity = addQuantity,
                        UnitPrice = product.SellingPrice ?? 0,
                    };
                    _context.CartItems.Add(cartItem);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";

                return RedirectToPage("/Cart/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cart", productId);
                TempData["Error"] = "Có lỗi hệ thống. Vui lòng thử lại sau.";
                return RedirectToPage("/Index");
            }
        }

        public async Task<IActionResult> OnPostRemoveAsync(int productId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");

                if (!userId.HasValue)
                {
                    TempData["Error"] = "Vui lòng đăng nhập.";
                    return RedirectToPage("/Account/Login");
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.IsCheckedOut == false);

                if (cart == null)
                {
                    TempData["Error"] = "Không tìm thấy giỏ hàng.";
                    return RedirectToPage();
                }

                var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (item == null)
                {
                    TempData["Error"] = "Sản phẩm không có trong giỏ hàng.";
                    return RedirectToPage();
                }

                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing product {ProductId} from cart", productId);
                TempData["Error"] = "Có lỗi hệ thống. Vui lòng thử lại sau.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync(int productId, int quantity)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");

                if (!userId.HasValue)
                {
                    TempData["Error"] = "Vui lòng đăng nhập.";
                    return RedirectToPage("/Account/Login");
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.IsCheckedOut == false);

                if (cart == null)
                {
                    TempData["Error"] = "Không tìm thấy giỏ hàng.";
                    return RedirectToPage();
                }

                var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
                if (item == null)
                {
                    TempData["Error"] = "Sản phẩm không tồn tại trong giỏ hàng.";
                    return RedirectToPage();
                }

                // Kiểm tra tồn kho
                var product = await _context.Products.FindAsync(productId);
                if (product != null && quantity > product.Quantity)
                {
                    TempData["Error"] = $"Không đủ hàng trong kho. Còn lại: {product.Quantity}";
                    return RedirectToPage();
                }

                if (quantity <= 0)
                {
                    _context.CartItems.Remove(item);
                    TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng!";
                }
                else
                {
                    item.Quantity = quantity;
                    TempData["Success"] = "Đã cập nhật số lượng!";
                }

                await _context.SaveChangesAsync();
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item for product {ProductId}", productId);
                TempData["Error"] = "Có lỗi hệ thống. Vui lòng thử lại sau.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostClearAsync()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");

                if (!userId.HasValue)
                {
                    return RedirectToPage("/Account/Login");
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.IsCheckedOut == false);

                if (cart != null && cart.CartItems.Any())
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Đã xóa tất cả sản phẩm khỏi giỏ hàng!";
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                TempData["Error"] = "Có lỗi hệ thống. Vui lòng thử lại sau.";
                return RedirectToPage();
            }
        }
    }
}

