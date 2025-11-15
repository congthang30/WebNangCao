using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.Models.VNPAY;
using NewWeb.Services.EMAILOTP;
using NewWeb.Services.VNPAY;
using NewWeb.Services.MOMO;
using System.Globalization;

namespace NewWeb.Pages.Checkout
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IVnPayService _vnPayService;
        private readonly IMomoService _momoService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ApplicationDbContext context,
            IEmailService emailService,
            IVnPayService vnPayService,
            IMomoService momoService,
            ILogger<IndexModel> logger)
        {
            _context = context;
            _emailService = emailService;
            _vnPayService = vnPayService;
            _momoService = momoService;
            _logger = logger;
        }

        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public List<Address> Addresses { get; set; } = new List<Address>();
        public List<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalAmount { get; set; }
        public string? VoucherCode { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                TempData["Error"] = "Vui lòng đăng nhập để thanh toán.";
                return RedirectToPage("/Account/Login");
            }

            try
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.IsCheckedOut == false);

                if (cart == null || !cart.CartItems.Any())
                {
                    TempData["Error"] = "Giỏ hàng trống!";
                    return RedirectToPage("/Cart/Index");
                }

                CartItems = cart.CartItems.ToList();
                TotalAmount = CartItems.Sum(item => (item.UnitPrice ?? 0) * (item.Quantity ?? 0));

                Addresses = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .ToListAsync();

                PaymentMethods = await _context.PaymentMethods.ToListAsync();

                // Lấy voucher code từ session nếu có
                VoucherCode = HttpContext.Session.GetString("CheckoutVoucherCode");
                if (!string.IsNullOrEmpty(VoucherCode))
                {
                    await CalculateDiscount(VoucherCode, TotalAmount);
                }
                else
                {
                    FinalAmount = TotalAmount;
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading checkout page");
                TempData["Error"] = "Có lỗi hệ thống. Vui lòng thử lại sau.";
                return RedirectToPage("/Cart/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(int addressId, int paymentMethodId, string? voucherCode)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                if (!userId.HasValue)
                {
                    TempData["Error"] = "Vui lòng đăng nhập để thanh toán.";
                    return RedirectToPage("/Account/Login");
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.IsCheckedOut == false);

                if (cart == null || cart.CartItems.Count == 0)
                {
                    TempData["Error"] = "Giỏ hàng trống!";
                    return RedirectToPage("/Cart/Index");
                }

                // Kiểm tra tồn kho trước khi checkout
                foreach (var item in cart.CartItems)
                {
                    var product = item.Product;
                    if (product == null)
                    {
                        TempData["Error"] = $"Sản phẩm không tồn tại.";
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

                // Validate địa chỉ
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == userId);
                if (address == null)
                {
                    TempData["Error"] = "Địa chỉ không hợp lệ.";
                    return RedirectToPage();
                }

                // Validate phương thức thanh toán
                var paymentMethod = await _context.PaymentMethods
                    .FirstOrDefaultAsync(pm => pm.PaymentMethodId == paymentMethodId);
                if (paymentMethod == null)
                {
                    TempData["Error"] = "Phương thức thanh toán không hợp lệ.";
                    return RedirectToPage();
                }

                decimal total = cart.CartItems.Sum(item => (item.UnitPrice ?? 0) * (item.Quantity ?? 0));
                decimal discount = 0;
                int? voucherId = null;

                // Validate và tính discount
                if (!string.IsNullOrEmpty(voucherCode))
                {
                    var today = DateOnly.FromDateTime(DateTime.Now);
                    var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == voucherCode &&
                                                                v.IsActive == true &&
                                                                v.Quantity > 0 &&
                                                                v.StartDate <= today &&
                                                                v.EndDate >= today);

                    if (voucher == null)
                    {
                        TempData["Error"] = "Mã giảm giá không hợp lệ.";
                        return RedirectToPage();
                    }

                    if (total < voucher.MinOrderAmount)
                    {
                        TempData["Error"] = $"Đơn hàng tối thiểu {voucher.MinOrderAmount:#,##0} VNĐ.";
                        return RedirectToPage();
                    }

                    voucherId = voucher.VoucherId;

                    if (voucher.DiscountType == "percent")
                        discount = total * ((voucher.DiscountValue ?? 0) / 100);
                    else
                        discount = voucher.DiscountValue ?? 0;

                    if (voucher.MaxDiscountAmount.HasValue && discount > voucher.MaxDiscountAmount.Value)
                        discount = voucher.MaxDiscountAmount.Value;
                }

                decimal finalAmount = total - discount;

                // Lưu thông tin vào Session
                HttpContext.Session.SetInt32("CheckoutCartId", cart.CartId);
                HttpContext.Session.SetInt32("CheckoutAddressId", addressId);
                HttpContext.Session.SetInt32("CheckoutPaymentMethodId", paymentMethodId);
                HttpContext.Session.SetString("CheckoutVoucherCode", voucherCode ?? "");
                HttpContext.Session.SetString("CheckoutTotalAmount", total.ToString(CultureInfo.InvariantCulture));
                HttpContext.Session.SetString("CheckoutDiscount", discount.ToString(CultureInfo.InvariantCulture));
                HttpContext.Session.SetString("CheckoutFinalAmount", finalAmount.ToString(CultureInfo.InvariantCulture));
                HttpContext.Session.SetInt32("CheckoutVoucherId", voucherId ?? 0);

                // Xử lý theo phương thức thanh toán
                if (paymentMethodId == 1) // COD
                {
                    var otp = GenerateSecureOtp();
                    var otpExpiry = DateTime.Now.AddMinutes(5);
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

                    if (user == null || string.IsNullOrEmpty(user.Email))
                    {
                        TempData["Error"] = "Không tìm thấy email của bạn.";
                        return RedirectToPage();
                    }

                    HttpContext.Session.SetString("CheckoutOtp", otp);
                    HttpContext.Session.SetString("CheckoutOtpExpiry", otpExpiry.ToString("yyyy-MM-dd HH:mm:ss"));
                    HttpContext.Session.SetInt32("CheckoutOtpAttempts", 0);

                    try
                    {
                        await _emailService.SendEmailAsync(user.Email, "Xác minh đơn hàng TechStore",
                            $"Mã OTP của bạn là: {otp}. Mã có hiệu lực trong 5 phút.");
                        TempData["Info"] = "Mã OTP đã được gửi đến email của bạn.";
                        return RedirectToPage("/Checkout/VerifyOtp");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending OTP email");
                        TempData["Error"] = "Không thể gửi OTP.";
                        return RedirectToPage();
                    }
                }
                else if (paymentMethodId == 2) // VNPay
                {
                    // Lưu thông tin tạm thời cho callback
                    HttpContext.Session.SetInt32("TempAddressId", addressId);
                    HttpContext.Session.SetInt32("TempPaymentMethodId", paymentMethodId);
                    HttpContext.Session.SetString("TempVoucherCode", voucherCode ?? "");

                    var paymentModel = new PaymentInformationModel
                    {
                        OrderId = 0,
                        OrderType = "other",
                        Name = "Thanh toán giỏ hàng",
                        OrderDescription = "Thanh toán giỏ hàng TechStore",
                        Amount = (double)finalAmount
                    };

                    var paymentUrl = _vnPayService.CreatePaymentUrl(paymentModel, HttpContext);
                    return Redirect(paymentUrl);
                }
                else if (paymentMethodId == 1002) // MoMo
                {
                    // Tạo order trước với status "Chờ thanh toán"
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var productIds = cart.CartItems.Select(c => c.ProductId).ToList();
                        var products = await _context.Products
                            .Where(p => productIds.Contains(p.ProductId))
                            .ToDictionaryAsync(p => p.ProductId, p => p.Quantity ?? 0);

                        // Kiểm tra tồn kho lại trước khi tạo order
                        foreach (var item in cart.CartItems)
                        {
                            if (products[item.ProductId] < (item.Quantity ?? 0))
                            {
                                await transaction.RollbackAsync();
                                TempData["Error"] = $"Sản phẩm không đủ số lượng.";
                                return RedirectToPage("/Cart/Index");
                            }
                        }

                        var order = new Order
                        {
                            UserId = userId.Value,
                            AddressId = addressId,
                            PaymentMethodId = paymentMethodId,
                            VoucherId = voucherId == 0 ? null : voucherId,
                            TotalAmount = total,
                            FinalTotal = finalAmount,
                            Status = "Chờ thanh toán",
                            StatusPayment = "Chưa thanh toán",
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
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        // Giảm số lượng voucher nếu có
                        if (voucherId.HasValue && voucherId.Value > 0)
                        {
                            var voucher = await _context.Vouchers.FindAsync(voucherId.Value);
                            if (voucher != null && voucher.Quantity > 0)
                            {
                                voucher.Quantity -= 1;
                                await _context.SaveChangesAsync();
                            }
                        }

                        // Tạo payment MoMo
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                        var momoModel = new OrderInfo
                        {
                            FullName = user?.FullName ?? "Khách hàng",
                            OrderInfomation = $"Thanh toán đơn hàng #{order.OrderId}",
                            Amount = (double)finalAmount
                        };

                        var momoResponse = await _momoService.CreatePaymentMomo(momoModel);
                        if (momoResponse != null && !string.IsNullOrEmpty(momoResponse.PayUrl))
                        {
                            // Lưu orderId vào session để callback xử lý
                            HttpContext.Session.SetInt32("MomoOrderId", order.OrderId);
                            return Redirect(momoResponse.PayUrl);
                        }

                        // Nếu tạo payment thất bại, cập nhật order status
                        order.Status = "Thanh toán thất bại";
                        order.StatusPayment = "Thanh toán thất bại";
                        await _context.SaveChangesAsync();

                        TempData["Error"] = "Tạo link thanh toán MoMo thất bại.";
                        return RedirectToPage();
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error creating order for MoMo payment");
                        TempData["Error"] = "Có lỗi hệ thống khi tạo đơn hàng.";
                        return RedirectToPage();
                    }
                }

                TempData["Error"] = "Phương thức thanh toán không hợp lệ.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing checkout");
                TempData["Error"] = "Có lỗi hệ thống!";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnGetValidateVoucherAsync(string code, string total)
        {
            if (!decimal.TryParse(total, out var parsedTotal))
            {
                return new JsonResult(new { success = false, message = "Tổng tiền không hợp lệ." });
            }

            var today = DateOnly.FromDateTime(DateTime.Now);

            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Code == code &&
                                          v.IsActive == true &&
                                          v.Quantity > 0 &&
                                          v.StartDate <= today &&
                                          v.EndDate >= today);

            if (voucher == null)
            {
                return new JsonResult(new { success = false, message = "Mã giảm giá không hợp lệ hoặc đã hết hạn." });
            }

            if (parsedTotal < voucher.MinOrderAmount)
            {
                return new JsonResult(new { success = false, message = $"Đơn hàng cần tối thiểu {voucher.MinOrderAmount:N0} VNĐ." });
            }

            decimal discount = 0;
            if (voucher.DiscountType == "percent")
                discount = parsedTotal * (voucher.DiscountValue ?? 0) / 100;
            else if (voucher.DiscountType == "amount")
                discount = voucher.DiscountValue ?? 0;

            if (voucher.MaxDiscountAmount.HasValue && discount > voucher.MaxDiscountAmount.Value)
                discount = voucher.MaxDiscountAmount.Value;

            return new JsonResult(new { success = true, discount });
        }

        public IActionResult OnPostCancelCheckout()
        {
            try
            {
                // Xóa tất cả session liên quan đến checkout
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
                HttpContext.Session.Remove("TempAddressId");
                HttpContext.Session.Remove("TempPaymentMethodId");
                HttpContext.Session.Remove("TempVoucherCode");
                HttpContext.Session.Remove("MomoOrderId");

                return RedirectToPage("/Cart/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling checkout");
                TempData["Error"] = "Có lỗi xảy ra khi hủy đặt hàng.";
                return RedirectToPage("/Cart/Index");
            }
        }

        private async Task CalculateDiscount(string voucherCode, decimal total)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == voucherCode &&
                                                            v.IsActive == true &&
                                                            v.Quantity > 0 &&
                                                            v.StartDate <= today &&
                                                            v.EndDate >= today);

            if (voucher != null && total >= voucher.MinOrderAmount)
            {
                if (voucher.DiscountType == "percent")
                    Discount = total * ((voucher.DiscountValue ?? 0) / 100);
                else
                    Discount = voucher.DiscountValue ?? 0;

                if (voucher.MaxDiscountAmount.HasValue && Discount > voucher.MaxDiscountAmount.Value)
                    Discount = voucher.MaxDiscountAmount.Value;

                FinalAmount = total - Discount;
            }
            else
            {
                FinalAmount = total;
            }
        }

        private string GenerateSecureOtp()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var random = Math.Abs(BitConverter.ToInt32(bytes, 0));
            return (random % 900000 + 100000).ToString();
        }
    }
}

