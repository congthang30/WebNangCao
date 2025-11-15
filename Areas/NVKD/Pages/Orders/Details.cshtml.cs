using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.Services.EMAILOTP;

namespace NewWeb.Areas.NVKD.Pages.Orders
{
    [AuthorizeRole("NVKD", "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(ApplicationDbContext context, IEmailService emailService, ILogger<DetailsModel> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public Order Order { get; set; } = default!;
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                Order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Address)
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.Voucher)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (Order == null)
                {
                    return NotFound();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details");
                ErrorMessage = "Có lỗi hệ thống";
                return Page();
            }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostExportInvoiceAsync(int orderId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                if (userId == null)
                {
                    return new JsonResult(new { success = false, message = "Vui lòng đăng nhập" });
                }

                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .Include(o => o.User)
                    .Include(o => o.Address)
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.Voucher)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    return new JsonResult(new { success = false, message = "Đơn hàng không tồn tại" });
                }

                var invoice = new Invoice
                {
                    OrderId = order.OrderId,
                    CreatedBy = userId.Value,
                    UpdatedAt = null,
                    CreatedAt = DateTime.Now,
                    InvoiceDate = DateTime.Now
                };

                order.Status = "Đã xác nhận";

                _context.Invoice.Add(invoice);
                await _context.SaveChangesAsync();

                await SendRequestWarehouse(orderId);

                return new JsonResult(new { success = true, message = "Tạo hóa đơn thành công!", invoiceId = invoice.InvoiceId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting invoice");
                return new JsonResult(new { success = false, message = "Có lỗi hệ thống: " + ex.Message });
            }
        }

        private async Task SendRequestWarehouse(int orderId)
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

                if (order == null)
                    return;

                var warehouseEmail = "thangcong30x@gmail.com";
                var emailSubject = $"[Yêu cầu xuất kho] Đơn hàng #{order.OrderId}";

                var emailBody = $"Xin chào đội kho,\n\n" +
                    $"Đơn hàng #{order.OrderId} của khách hàng {order.User?.FullName} " +
                    $"đã được xác nhận và cần chuẩn bị đóng gói, xuất kho.\n\n" +
                    $"Địa chỉ giao hàng: {order.Address?.FullAddress}\n" +
                    $"Phương thức thanh toán: {order.PaymentMethod?.MethodName}\n" +
                    $"Voucher: {(order.Voucher != null ? order.Voucher.Code : "Không áp dụng")}\n\n" +
                    $"Danh sách sản phẩm:\n";

                foreach (var item in order.OrderDetails)
                {
                    emailBody += $"- {item.Product?.ProductName} x {item.Quantity}\n";
                }

                emailBody += "\nVui lòng kiểm tra và xử lý sớm nhất.\n\nTrân trọng.";

                await _emailService.SendEmailAsync(warehouseEmail, emailSubject, emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending warehouse email for order #{orderId}");
            }
        }
    }
}

