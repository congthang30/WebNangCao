using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.Services.VNPAY;

namespace NewWeb.Pages.Orders
{
    public class PaymentCallbackVnpayModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly ILogger<PaymentCallbackVnpayModel> _logger;

        public PaymentCallbackVnpayModel(ApplicationDbContext context, IVnPayService vnPayService, ILogger<PaymentCallbackVnpayModel> logger)
        {
            _context = context;
            _vnPayService = vnPayService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var response = _vnPayService.PaymentExecute(Request.Query);
                if (response.Success)
                {
                    var userId = HttpContext.Session.GetInt32("UserID");
                    var addressId = HttpContext.Session.GetInt32("TempAddressId");
                    var paymentMethodId = HttpContext.Session.GetInt32("TempPaymentMethodId");
                    var voucherCode = HttpContext.Session.GetString("TempVoucherCode");

                    if (!userId.HasValue || !addressId.HasValue || !paymentMethodId.HasValue)
                    {
                        TempData["Error"] = "Thiếu thông tin thanh toán.";
                        return RedirectToPage("/Checkout/Index");
                    }

                    var cart = await _context.Carts
                        .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
                        .FirstOrDefaultAsync(c => c.UserId == userId && c.IsCheckedOut == false);

                    if (cart == null)
                    {
                        TempData["Error"] = "Không tìm thấy giỏ hàng.";
                        return RedirectToPage("/Cart/Index");
                    }

                    // Kiểm tra tồn kho trước khi tạo đơn hàng
                    foreach (var item in cart.CartItems)
                    {
                        var product = item.Product;
                        if (product == null)
                        {
                            TempData["Error"] = "Sản phẩm không tồn tại.";
                            return RedirectToPage("/Cart/Index");
                        }

                        if ((product.Quantity ?? 0) < (item.Quantity ?? 0))
                        {
                            TempData["Error"] = $"Sản phẩm {product.ProductName} không đủ số lượng. Còn lại: {product.Quantity}";
                            return RedirectToPage("/Cart/Index");
                        }

                        if ((product.Quantity ?? 0) <= 0)
                        {
                            TempData["Error"] = $"Sản phẩm {product.ProductName} đã hết hàng.";
                            return RedirectToPage("/Cart/Index");
                        }
                    }

                    decimal total = cart.CartItems.Sum(item => (item.UnitPrice ?? 0) * (item.Quantity ?? 0));
                    decimal discount = 0;
                    int? voucherId = null;

                    if (!string.IsNullOrEmpty(voucherCode))
                    {
                        var today = DateOnly.FromDateTime(DateTime.Now);
                        var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == voucherCode &&
                                                                v.IsActive == true &&
                                                                v.Quantity > 0 &&
                                                                v.StartDate <= today &&
                                                                v.EndDate >= today);
                        if (voucher != null)
                        {
                            voucherId = voucher.VoucherId;
                            if (voucher.DiscountType == "percent")
                                discount = total * ((voucher.DiscountValue ?? 0) / 100);
                            else discount = voucher.DiscountValue ?? 0;

                            if (voucher.MaxDiscountAmount.HasValue && discount > voucher.MaxDiscountAmount.Value)
                                discount = voucher.MaxDiscountAmount.Value;

                            voucher.Quantity -= 1;
                        }
                    }

                    decimal finalAmount = total - discount;

                    var productIds = cart.CartItems.Select(c => c.ProductId).ToList();
                    var products = await _context.Products
                        .Where(p => productIds.Contains(p.ProductId))
                        .ToDictionaryAsync(p => p.ProductId, p => p.Quantity ?? 0);

                    var order = new Order
                    {
                        UserId = userId.Value,
                        AddressId = addressId.Value,
                        PaymentMethodId = paymentMethodId.Value,
                        VoucherId = voucherId,
                        TotalAmount = finalAmount,
                        FinalTotal = finalAmount,
                        Status = "Chờ xác nhận",
                        StatusPayment = "Đã thanh toán",
                        CreatedAt = DateTime.Now,
                        OrderDetails = cart.CartItems.Select(item => new OrderDetail
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            ExistFirst = products[item.ProductId],
                            SurviveAfter = products[item.ProductId] - (item.Quantity ?? 0)
                        }).ToList()
                    };

                    _context.Orders.Add(order);

                    cart.IsCheckedOut = true;
                    foreach (var item in cart.CartItems)
                    {
                        var product = item.Product;
                        if (product != null && product.Quantity.HasValue)
                            product.Quantity -= item.Quantity ?? 0;
                    }

                    await _context.SaveChangesAsync();

                    // Xóa session tạm thời
                    HttpContext.Session.Remove("TempAddressId");
                    HttpContext.Session.Remove("TempPaymentMethodId");
                    HttpContext.Session.Remove("TempVoucherCode");

                    TempData["Success"] = "Thanh toán VNPay thành công!";
                    return RedirectToPage("/Orders/Success");
                }
                else
                {
                    TempData["Error"] = "Thanh toán thất bại!";
                    return RedirectToPage("/Checkout/Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay callback");
                TempData["Error"] = "Có lỗi xảy ra trong quá trình thanh toán.";
                return RedirectToPage("/Checkout/Index");
            }
        }
    }
}

