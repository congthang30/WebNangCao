using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.NVKho.Pages.Export
{
    [AuthorizeRole("NVKho", "Admin")]
    public class ListOrderConfirmModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ListOrderConfirmModel> _logger;

        public ListOrderConfirmModel(ApplicationDbContext context, ILogger<ListOrderConfirmModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Order> Orders { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Lấy danh sách OrderID đã có phiếu xuất kho
                var orderIdsWithInvoice = await _context.InvoiceWareHouses
                    .Select(i => i.OrderID)
                    .ToListAsync();

                Orders = await _context.Orders
                    .Where(o => o.Status == "Đã xác nhận" && !orderIdsWithInvoice.Contains(o.OrderId))
                    .Include(o => o.OrderDetails)
                    .Include(o => o.User)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading confirmed orders");
                ErrorMessage = "Có lỗi hệ thống";
                return Page();
            }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnGetOrderDetailsAsync(int orderId)
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
                            .ThenInclude(p => p.Brand)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    return new JsonResult(new { success = false, message = "Đơn hàng không tồn tại" });
                }

                var orderData = new
                {
                    orderId = order.OrderId,
                    customerName = order.User?.FullName,
                    customerPhone = order.User?.Phone,
                    customerEmail = order.User?.Email,
                    address = order.Address?.FullAddress,
                    createdAt = order.CreatedAt?.ToString("dd/MM/yyyy HH:mm"),
                    status = order.Status,
                    statusPayment = order.StatusPayment,
                    paymentMethod = order.PaymentMethod?.MethodName,
                    totalAmount = order.TotalAmount,
                    finalTotal = order.FinalTotal ?? order.TotalAmount,
                    voucher = order.Voucher != null ? new
                    {
                        code = order.Voucher.Code,
                        discountAmount = (order.TotalAmount ?? 0) - (order.FinalTotal ?? 0)
                    } : null,
                    orderDetails = order.OrderDetails.Select(od => new
                    {
                        productName = od.Product?.ProductName,
                        brandName = od.Product?.Brand?.NameBrand,
                        imageUrl = od.Product?.ImagePr,
                        unitPrice = od.UnitPrice,
                        quantity = od.Quantity,
                        totalPrice = (od.UnitPrice ?? 0) * (od.Quantity ?? 0) - (od.Discount ?? 0)
                    }).ToList()
                };

                return new JsonResult(new { success = true, data = orderData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order details");
                return new JsonResult(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostMakeInvoiceWarehouseAsync([FromForm] int orderId)
        {
            try
            {
                int? staffid = HttpContext.Session.GetInt32("UserID");
                if (staffid == null)
                {
                    return new JsonResult(new { success = false, message = "Vui lòng đăng nhập" });
                }

                // Kiểm tra xem đã có phiếu xuất kho cho đơn hàng này chưa
                var existingInvoice = await _context.InvoiceWareHouses
                    .FirstOrDefaultAsync(i => i.OrderID == orderId);

                if (existingInvoice != null)
                {
                    return new JsonResult(new { success = false, message = "Đơn hàng này đã có phiếu xuất kho rồi (Mã phiếu: #" + existingInvoice.InvoiceWareHouseID + ")" });
                }

                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    return new JsonResult(new { success = false, message = "Đơn hàng không tồn tại" });
                }

                // Kiểm tra trạng thái đơn hàng
                if (order.Status != "Đã xác nhận")
                {
                    return new JsonResult(new { success = false, message = "Chỉ có thể tạo phiếu xuất kho cho đơn hàng đã xác nhận. Trạng thái hiện tại: " + order.Status });
                }

                // Kiểm tra UserID
                if (order.UserId == null || order.UserId == 0)
                {
                    return new JsonResult(new { success = false, message = "Đơn hàng không có thông tin khách hàng hợp lệ" });
                }

                // Kiểm tra StaffID có tồn tại không
                var staffExists = await _context.Users.AnyAsync(u => u.UserId == staffid.Value);
                if (!staffExists)
                {
                    return new JsonResult(new { success = false, message = "Thông tin nhân viên không hợp lệ" });
                }

                // Kiểm tra đơn hàng có sản phẩm không
                if (!order.OrderDetails.Any())
                {
                    return new JsonResult(new { success = false, message = "Đơn hàng không có sản phẩm nào" });
                }

                var invoice = new InvoiceWareHouse
                {
                    ExportDate = DateTime.Now,
                    UserID = order.UserId.Value,
                    StaffID = staffid.Value,
                    OrderID = order.OrderId,
                    TotalQuantity = order.OrderDetails.Sum(od => od.Quantity ?? 0)
                };

                await _context.InvoiceWareHouses.AddAsync(invoice);
                order.Status = "Chờ giao hàng";
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Tạo phiếu xuất kho thành công. Mã phiếu: #" + invoice.InvoiceWareHouseID, invoiceId = invoice.InvoiceWareHouseID });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error creating warehouse invoice for order {OrderId}", orderId);
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                
                // Kiểm tra lỗi duplicate
                if (innerMessage.Contains("duplicate") || innerMessage.Contains("UNIQUE") || innerMessage.Contains("PRIMARY KEY"))
                {
                    return new JsonResult(new { success = false, message = "Đơn hàng này đã có phiếu xuất kho rồi" });
                }
                
                // Kiểm tra lỗi foreign key
                if (innerMessage.Contains("FOREIGN KEY") || innerMessage.Contains("constraint"))
                {
                    return new JsonResult(new { success = false, message = "Lỗi dữ liệu: Thông tin khách hàng hoặc nhân viên không hợp lệ" });
                }

                return new JsonResult(new { success = false, message = "Lỗi CSDL: " + innerMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating warehouse invoice for order {OrderId}", orderId);
                return new JsonResult(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}

