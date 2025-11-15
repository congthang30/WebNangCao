using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Pages.Orders
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

        public List<Order> Orders { get; set; } = new List<Order>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                if (!userId.HasValue)
                {
                    TempData["Error"] = "Vui lòng đăng nhập để xem đơn hàng.";
                    return RedirectToPage("/Account/Login");
                }

                Orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                TempData["Error"] = "Có lỗi hệ thống. Vui lòng thử lại sau.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCancelOrderAsync(int orderId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                if (!userId.HasValue)
                {
                    TempData["Error"] = "Vui lòng đăng nhập.";
                    return RedirectToPage("/Account/Login");
                }

                var order = await _context.Orders
                    .Include(o => o.Voucher)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToPage();
                }

                if (order.Status != "Chờ xác nhận")
                {
                    TempData["Error"] = "Đơn hàng không thể hủy ở trạng thái hiện tại.";
                    return RedirectToPage();
                }

                order.Status = "Đã hủy";
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đơn hàng đã được hủy thành công.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling order {OrderId}", orderId);
                TempData["Error"] = "Có lỗi xảy ra khi hủy đơn hàng.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostRepeatOrderAsync(int orderId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                if (!userId.HasValue)
                {
                    TempData["Error"] = "Bạn cần đăng nhập để thực hiện chức năng này.";
                    return RedirectToPage("/Account/Login");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var oldOrder = await _context.Orders
                        .Include(o => o.OrderDetails)
                        .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

                    if (oldOrder == null)
                    {
                        TempData["Error"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.";
                        return RedirectToPage();
                    }

                    if (oldOrder.OrderDetails == null || !oldOrder.OrderDetails.Any())
                    {
                        TempData["Error"] = "Đơn hàng không có sản phẩm nào để mua lại.";
                        return RedirectToPage();
                    }

                    var cart = await _context.Carts
                        .Include(c => c.CartItems)
                        .FirstOrDefaultAsync(c => c.UserId == userId && c.IsCheckedOut == false);

                    if (cart == null)
                    {
                        cart = new Models.Cart
                        {
                            UserId = userId.Value,
                            CreatedAt = DateTime.Now,
                            IsCheckedOut = false,
                            CartItems = new List<CartItem>()
                        };
                        _context.Carts.Add(cart);
                        await _context.SaveChangesAsync();
                    }

                    foreach (var orderDetail in oldOrder.OrderDetails)
                    {
                        var product = await _context.Products
                            .FirstOrDefaultAsync(p => p.ProductId == orderDetail.ProductId && (p.Quantity ?? 0) > 0);

                        if (product == null) continue;

                        var existingItem = cart.CartItems
                            .FirstOrDefault(ci => ci.ProductId == product.ProductId);

                        int quantityToAdd = orderDetail.Quantity.HasValue
                            ? Math.Min(orderDetail.Quantity.Value, product.Quantity ?? 0)
                            : 1;

                        if (existingItem != null)
                        {
                            int totalQuantity = (existingItem.Quantity ?? 0) + quantityToAdd;
                            existingItem.Quantity = Math.Min(totalQuantity, product.Quantity ?? 0);
                            existingItem.UnitPrice = product.SellingPrice;
                        }
                        else
                        {
                            var newItem = new CartItem
                            {
                                CartId = cart.CartId,
                                ProductId = product.ProductId,
                                Quantity = quantityToAdd,
                                UnitPrice = product.SellingPrice,
                                CreatedAt = DateTime.Now
                            };
                            _context.CartItems.Add(newItem);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";
                    return RedirectToPage("/Cart/Index");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error repeating order {OrderId}", orderId);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RepeatOrder");
                TempData["Error"] = "Có lỗi xảy ra khi mua lại đơn hàng. Vui lòng thử lại sau.";
                return RedirectToPage();
            }
        }
    }
}

