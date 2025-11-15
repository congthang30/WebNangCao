using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.Services.MOMO;

namespace NewWeb.Pages.Orders
{
    public class PaymentCallbackMomoModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IMomoService _momoService;
        private readonly ILogger<PaymentCallbackMomoModel> _logger;

        public PaymentCallbackMomoModel(ApplicationDbContext context, IMomoService momoService, ILogger<PaymentCallbackMomoModel> logger)
        {
            _context = context;
            _momoService = momoService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var momoResponse = _momoService.PaymentExecuteAsync(Request.Query);
                var orderId = HttpContext.Session.GetInt32("MomoOrderId");

                if (!orderId.HasValue)
                {
                    TempData["Error"] = "Không tìm thấy thông tin đơn hàng.";
                    return RedirectToPage("/Orders/Index");
                }

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToPage("/Orders/Index");
                }

                if (momoResponse != null && !string.IsNullOrEmpty(momoResponse.OrderId))
                {
                    if (order.Status == "Chờ thanh toán")
                    {
                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            // Kiểm tra tồn kho lại trước khi cập nhật
                            var orderDetails = await _context.OrderDetails
                                .Include(od => od.Product)
                                .Where(od => od.OrderId == order.OrderId)
                                .ToListAsync();

                            foreach (var detail in orderDetails)
                            {
                                var product = detail.Product;
                                if (product == null)
                                {
                                    await transaction.RollbackAsync();
                                    TempData["Error"] = "Sản phẩm không tồn tại.";
                                    return RedirectToPage("/Orders/Index");
                                }

                                if ((product.Quantity ?? 0) < (detail.Quantity ?? 0))
                                {
                                    await transaction.RollbackAsync();
                                    order.Status = "Thanh toán thất bại";
                                    order.StatusPayment = "Thanh toán thất bại";
                                    await _context.SaveChangesAsync();
                                    TempData["Error"] = $"Sản phẩm {product.ProductName} không đủ số lượng.";
                                    return RedirectToPage("/Orders/Index");
                                }

                                // Trừ số lượng sản phẩm
                                product.Quantity = (product.Quantity ?? 0) - (detail.Quantity ?? 0);
                            }

                            // Đánh dấu cart đã checkout
                            var cart = await _context.Carts
                                .FirstOrDefaultAsync(c => c.UserId == order.UserId && c.IsCheckedOut == false);
                            if (cart != null)
                            {
                                cart.IsCheckedOut = true;
                            }

                            // Cập nhật order status
                            order.Status = "Chờ xác nhận";
                            order.StatusPayment = "Đã thanh toán";
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            HttpContext.Session.Remove("MomoOrderId");
                            TempData["Success"] = "Thanh toán MoMo thành công! Đơn hàng đã được xác nhận.";
                            return RedirectToPage("/Orders/Success");
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError(ex, "Error updating order after MoMo payment success");
                            TempData["Error"] = "Có lỗi xảy ra khi cập nhật đơn hàng.";
                            return RedirectToPage("/Orders/Index");
                        }
                    }
                    else
                    {
                        TempData["Info"] = "Đơn hàng đã được xử lý trước đó.";
                        return RedirectToPage("/Orders/Index");
                    }
                }
                else
                {
                    if (order.Status == "Chờ thanh toán")
                    {
                        order.Status = "Thanh toán thất bại";
                        order.StatusPayment = "Thanh toán thất bại";
                        await _context.SaveChangesAsync();
                    }

                    HttpContext.Session.Remove("MomoOrderId");
                    TempData["Error"] = "Thanh toán MoMo thất bại. Vui lòng thử lại.";
                    return RedirectToPage("/Checkout/Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MoMo callback");
                TempData["Error"] = "Có lỗi xảy ra trong quá trình xử lý thanh toán.";
                return RedirectToPage("/Checkout/Index");
            }
        }
    }
}

