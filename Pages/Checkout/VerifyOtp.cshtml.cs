using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.Services.EMAILOTP;
using System.Globalization;

namespace NewWeb.Pages.Checkout
{
    public class VerifyOtpModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<VerifyOtpModel> _logger;

        public VerifyOtpModel(ApplicationDbContext context, IEmailService emailService, ILogger<VerifyOtpModel> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public string? ErrorMessage { get; set; }
        public string? InfoMessage { get; set; }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                TempData["Error"] = "Vui lòng đăng nhập.";
                return RedirectToPage("/Account/Login");
            }

            var cartId = HttpContext.Session.GetInt32("CheckoutCartId");
            if (cartId == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin đặt hàng.";
                return RedirectToPage("/Cart/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string inputOtp)
        {
            try
            {
                var sessionOtp = HttpContext.Session.GetString("CheckoutOtp");
                var sessionOtpExpiry = HttpContext.Session.GetString("CheckoutOtpExpiry");
                var sessionOtpAttempts = HttpContext.Session.GetInt32("CheckoutOtpAttempts");
                var cartId = HttpContext.Session.GetInt32("CheckoutCartId");
                var addressId = HttpContext.Session.GetInt32("CheckoutAddressId");
                var paymentMethodId = HttpContext.Session.GetInt32("CheckoutPaymentMethodId");
                var voucherCode = HttpContext.Session.GetString("CheckoutVoucherCode");
                var voucherId = HttpContext.Session.GetInt32("CheckoutVoucherId");
                var totalAmountString = HttpContext.Session.GetString("CheckoutTotalAmount");
                var finalAmountString = HttpContext.Session.GetString("CheckoutFinalAmount");
                var discountString = HttpContext.Session.GetString("CheckoutDiscount");
                var userId = HttpContext.Session.GetInt32("UserID");

                if (!decimal.TryParse(discountString, NumberStyles.Any, CultureInfo.InvariantCulture, out var discount))
                {
                    ErrorMessage = "Không tìm thấy mã giảm giá";
                    return Page();
                }

                if (!decimal.TryParse(totalAmountString, NumberStyles.Any, CultureInfo.InvariantCulture, out var total))
                {
                    ErrorMessage = "Không tìm thấy thông tin tổng tiền.";
                    return Page();
                }

                if (string.IsNullOrEmpty(sessionOtpExpiry) || !DateTime.TryParse(sessionOtpExpiry, out var expiry) || DateTime.Now > expiry)
                {
                    ErrorMessage = "Mã OTP đã hết hạn.";
                    return Page();
                }

                if (sessionOtpAttempts.HasValue && sessionOtpAttempts.Value >= 3)
                {
                    ErrorMessage = "Bạn đã thử quá nhiều lần.";
                    return Page();
                }

                if (sessionOtp == null || inputOtp != sessionOtp)
                {
                    var attempts = (sessionOtpAttempts ?? 0) + 1;
                    HttpContext.Session.SetInt32("CheckoutOtpAttempts", attempts);
                    ErrorMessage = $"Mã OTP không đúng. Còn {3 - attempts} lần thử!";
                    return Page();
                }

                if (cartId == null || addressId == null || paymentMethodId == null || userId == null || 
                    !decimal.TryParse(finalAmountString, NumberStyles.Any, CultureInfo.InvariantCulture, out var finalAmount))
                {
                    ErrorMessage = "Thông tin không đầy đủ.";
                    return Page();
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var cartItems = await _context.CartItems
                        .Include(ci => ci.Product)
                        .Where(ci => ci.CartId == cartId.Value)
                        .ToListAsync();

                    if (!cartItems.Any())
                    {
                        ErrorMessage = "Giỏ hàng trống.";
                        return Page();
                    }

                    // Giảm số lượng voucher nếu có
                    if (voucherId.HasValue && voucherId.Value > 0)
                    {
                        var voucher = await _context.Vouchers.FindAsync(voucherId.Value);
                        if (voucher != null && voucher.Quantity > 0)
                        {
                            voucher.Quantity -= 1;
                        }
                    }

                    var productIds = cartItems.Select(c => c.ProductId).ToList();
                    var products = await _context.Products
                        .Where(p => productIds.Contains(p.ProductId))
                        .ToDictionaryAsync(p => p.ProductId, p => p.Quantity ?? 0);

                    var order = new Order
                    {
                        UserId = userId.Value,
                        AddressId = addressId.Value,
                        PaymentMethodId = paymentMethodId.Value,
                        VoucherId = voucherId == 0 ? null : voucherId,
                        TotalAmount = total,
                        FinalTotal = finalAmount,
                        Status = "Chờ xác nhận",
                        StatusPayment = "Chưa thanh toán",
                        CreatedAt = DateTime.Now,
                        OrderDetails = cartItems.Select(item => new OrderDetail
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            ExistFirst = products[item.ProductId],
                            SurviveAfter = products[item.ProductId] - (item.Quantity ?? 0)
                        }).ToList()
                    };

                    _context.Orders.Add(order);

                    foreach (var item in cartItems)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null && (product.Quantity ?? 0) < (item.Quantity ?? 0))
                        {
                            ErrorMessage = $"Sản phẩm {product.ProductName} không đủ số lượng.";
                            await transaction.RollbackAsync();
                            return Page();
                        }
                        if (product != null)
                        {
                            product.Quantity = (product.Quantity ?? 0) - (item.Quantity ?? 0);
                        }
                    }

                    var cart = await _context.Carts.FirstOrDefaultAsync(c => c.CartId == cartId.Value);
                    if (cart != null) cart.IsCheckedOut = true;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Xóa session checkout
                    ClearCheckoutSession();

                    await SendOrderConfirmationEmail(order.OrderId);

                    TempData["Success"] = "Đặt hàng thành công!";
                    return RedirectToPage("/Orders/Success");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error verifying OTP and creating order");
                    ErrorMessage = "Có lỗi xảy ra khi tạo đơn hàng.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in VerifyOtp POST");
                ErrorMessage = "Có lỗi hệ thống. Vui lòng thử lại sau.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostResendOtpAsync()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                if (userId == null)
                {
                    ErrorMessage = "Phiên đăng nhập đã hết hạn.";
                    return Page();
                }

                var cartId = HttpContext.Session.GetInt32("CheckoutCartId");
                if (cartId == null)
                {
                    ErrorMessage = "Không tìm thấy thông tin đặt hàng.";
                    return Page();
                }

                // Tạo OTP mới
                var newOtp = GenerateSecureOtp();
                var newOtpExpiry = DateTime.Now.AddMinutes(5);

                HttpContext.Session.SetString("CheckoutOtp", newOtp);
                HttpContext.Session.SetString("CheckoutOtpExpiry", newOtpExpiry.ToString("yyyy-MM-dd HH:mm:ss"));
                HttpContext.Session.SetInt32("CheckoutOtpAttempts", 0);

                // Lấy email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null || string.IsNullOrEmpty(user.Email))
                {
                    ErrorMessage = "Không tìm thấy email của bạn.";
                    return Page();
                }

                await _emailService.SendEmailAsync(user.Email, "Xác minh đơn hàng TechStore",
                    $"Mã OTP mới của bạn là: {newOtp}. Mã có hiệu lực trong 5 phút.");

                InfoMessage = "OTP mới đã được gửi đến email của bạn.";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending OTP");
                ErrorMessage = "Không thể gửi OTP. Vui lòng thử lại.";
                return Page();
            }
        }

        private void ClearCheckoutSession()
        {
            HttpContext.Session.Remove("CheckoutCartId");
            HttpContext.Session.Remove("CheckoutAddressId");
            HttpContext.Session.Remove("CheckoutPaymentMethodId");
            HttpContext.Session.Remove("CheckoutVoucherCode");
            HttpContext.Session.Remove("CheckoutTotalAmount");
            HttpContext.Session.Remove("CheckoutDiscount");
            HttpContext.Session.Remove("CheckoutFinalAmount");
            HttpContext.Session.Remove("CheckoutVoucherId");
            HttpContext.Session.Remove("CheckoutOtp");
            HttpContext.Session.Remove("CheckoutOtpExpiry");
            HttpContext.Session.Remove("CheckoutOtpAttempts");
        }

        private string GenerateSecureOtp()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var random = Math.Abs(BitConverter.ToInt32(bytes, 0));
            return (random % 900000 + 100000).ToString();
        }

        private async Task SendOrderConfirmationEmail(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Address)
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.Voucher)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null || order.User == null || string.IsNullOrEmpty(order.User.Email))
                    return;

                var emailSubject = "Xác nhận đơn hàng TechStore - Đặt hàng thành công";

                var emailBody = $@"
🎉 ĐƠN HÀNG CỦA BẠN ĐÃ ĐƯỢC XÁC NHẬN!

Xin chào {order.User.FullName},

Cảm ơn bạn đã mua sắm tại TechStore! Đơn hàng của bạn đã được xác nhận thành công.

=========================================
📋 THÔNG TIN ĐƠN HÀNG
=========================================
Mã đơn hàng: #{order.OrderId}
Ngày đặt: {order.CreatedAt:dd/MM/yyyy HH:mm}
Trạng thái: {order.Status}
Trạng thái thanh toán: {order.StatusPayment}
Phương thức thanh toán: {order.PaymentMethod?.MethodName}

=========================================
📍 ĐỊA CHỈ GIAO HÀNG
=========================================
{order.Address?.Street}, {order.Address?.Ward}, {order.Address?.District}, {order.Address?.City}
Người nhận: {order.User.FullName}
Số điện thoại: {order.Address?.Phone}

=========================================
🛍️ CHI TIẾT SẢN PHẨM
=========================================";

                foreach (var item in order.OrderDetails)
                {
                    var itemTotal = (item.UnitPrice ?? 0) * (item.Quantity ?? 0);
                    emailBody += $@"
• {item.Product?.ProductName}
  Số lượng: {item.Quantity}
  Đơn giá: {item.UnitPrice:N0} VNĐ
  Thành tiền: {itemTotal:N0} VNĐ
";
                }

                decimal subtotal = order.OrderDetails.Sum(od => (od.UnitPrice ?? 0) * (od.Quantity ?? 0));
                decimal discount = subtotal - (order.FinalTotal ?? order.TotalAmount ?? 0);

                emailBody += $@"
-----------------------------------------
Tạm tính: {subtotal:N0} VNĐ";

                if (discount > 0)
                {
                    var voucherInfo = order.Voucher != null ? $" ({order.Voucher.Code})" : "";
                    emailBody += $@"
Giảm giá{voucherInfo}: -{discount:N0} VNĐ";
                }

                emailBody += $@"
-----------------------------------------
TỔNG CỘNG: {(order.FinalTotal ?? order.TotalAmount ?? 0):N0} VNĐ
=========================================

Cảm ơn bạn đã tin tưởng và lựa chọn TechStore! 💚
";

                await _emailService.SendEmailAsync(order.User.Email, emailSubject, emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation email for order {OrderId}", orderId);
            }
        }
    }
}

