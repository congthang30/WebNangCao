using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.NVKho.Pages.Export
{
    [AuthorizeRole("NVKho", "Admin")]
    public class ListInvoiceWarehouseModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ListInvoiceWarehouseModel> _logger;

        public ListInvoiceWarehouseModel(ApplicationDbContext context, ILogger<ListInvoiceWarehouseModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<InvoiceWareHouse> Invoices { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Invoices = await _context.InvoiceWareHouses
                    .Include(i => i.Order)
                        .ThenInclude(o => o.User)
                    .Include(i => i.Staff)
                    .OrderByDescending(i => i.ExportDate)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading warehouse invoices");
                ErrorMessage = "Có lỗi xảy ra khi tải danh sách phiếu xuất kho.";
                Invoices = new List<InvoiceWareHouse>();
                return Page();
            }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnGetInvoiceDetailsAsync(int invoiceId)
        {
            try
            {
                var invoice = await _context.InvoiceWareHouses
                    .Include(i => i.Order)
                        .ThenInclude(o => o.User)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.Address)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.OrderDetails)
                            .ThenInclude(od => od.Product)
                                .ThenInclude(p => p.Brand)
                    .Include(i => i.Staff)
                    .FirstOrDefaultAsync(i => i.InvoiceWareHouseID == invoiceId);

                if (invoice == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy phiếu xuất kho." });
                }

                var data = new
                {
                    invoiceId = invoice.InvoiceWareHouseID,
                    createdAt = invoice.ExportDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    customerName = invoice.Order?.User?.FullName,
                    address = invoice.Order?.Address?.FullAddress,
                    phone = invoice.Order?.User?.Phone,
                    staffName = invoice.Staff?.FullName,
                    products = invoice.Order?.OrderDetails.Select(od => new
                    {
                        productName = od.Product?.ProductName,
                        brand = od.Product?.Brand?.NameBrand,
                        quantity = od.Quantity,
                        unitPrice = od.UnitPrice
                    }).ToList()
                };

                return new JsonResult(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoice details");
                return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi tải chi tiết phiếu xuất kho." });
            }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostPrintWarehouseDetailAsync(int warehouseid)
        {
            try
            {
                var invoice = await _context.InvoiceWareHouses
                    .Include(i => i.Order)
                        .ThenInclude(o => o.User)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.Address)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.OrderDetails)
                            .ThenInclude(od => od.Product)
                                .ThenInclude(p => p.Brand)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.PaymentMethod)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.Voucher)
                    .Include(i => i.Staff)
                    .FirstOrDefaultAsync(i => i.InvoiceWareHouseID == warehouseid);

                if (invoice == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy phiếu xuất kho." });
                }

                var bill = GenerateWarehouseBill(invoice);

                return new JsonResult(new
                {
                    success = true,
                    message = "Tạo phiếu xuất kho thành công!",
                    billContent = bill
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing warehouse detail");
                return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi tạo phiếu xuất kho: " + ex.Message });
            }
        }

        private string GenerateWarehouseBill(InvoiceWareHouse invoice)
        {
            var order = invoice.Order;
            var customer = order?.User;
            var address = order?.Address;
            var staff = invoice.Staff;
            var orderDetails = order?.OrderDetails;

            var bill = $@"
╔══════════════════════════════════════════════════════════════╗
║                    PHIẾU XUẤT KHO                           ║
╠══════════════════════════════════════════════════════════════╣
║ Mã phiếu: {invoice.InvoiceWareHouseID,-45} ║
║ Ngày xuất: {invoice.ExportDate:dd/MM/yyyy HH:mm,-40} ║
║ Nhân viên: {(staff?.FullName ?? "N/A"),-42} ║
╠══════════════════════════════════════════════════════════════╣
║ THÔNG TIN KHÁCH HÀNG                                        ║
║ Tên: {(customer?.FullName ?? "N/A"),-50} ║
║ SĐT: {(customer?.Phone ?? "N/A"),-50} ║
║ Email: {(customer?.Email ?? "N/A"),-47} ║
║ Địa chỉ: {(address?.FullAddress ?? "N/A"),-45} ║
╠══════════════════════════════════════════════════════════════╣
║ CHI TIẾT SẢN PHẨM                                           ║
╠══════════════════════════════════════════════════════════════╣";

            if (orderDetails != null && orderDetails.Any())
            {
                decimal totalAmount = 0;
                int totalQuantity = 0;

                foreach (var detail in orderDetails)
                {
                    var product = detail.Product;
                    var quantity = detail.Quantity ?? 0;
                    var unitPrice = detail.UnitPrice ?? 0;
                    var totalPrice = quantity * unitPrice;

                    totalAmount += totalPrice;
                    totalQuantity += quantity;

                    bill += $@"
║ {(product?.ProductName ?? "N/A"),-30} x{quantity,-3} {unitPrice:N0}₫ {totalPrice:N0}₫ ║";
                }

                bill += $@"
╠══════════════════════════════════════════════════════════════╣
║ Tổng số lượng: {totalQuantity,-40} ║
║ Tổng tiền: {totalAmount:N0}₫,-40}} ║
╚══════════════════════════════════════════════════════════════╝
";

                if (order?.PaymentMethod != null)
                {
                    bill += $@"
║ Phương thức thanh toán: {order.PaymentMethod.MethodName,-30} ║
";
                }

                if (order?.Voucher != null)
                {
                    var discount = (order.TotalAmount ?? 0) - (order.FinalTotal ?? 0);
                    bill += $@"
║ Mã giảm giá: {order.Voucher.Code,-40} ║
║ Giảm giá: {discount:N0}₫,-40}} ║
";
                }
            }
            else
            {
                bill += $@"
║ Không có sản phẩm nào trong đơn hàng                        ║
╚══════════════════════════════════════════════════════════════╝
";
            }

            bill += $@"

Cảm ơn quý khách đã mua hàng!
Hẹn gặp lại!

Ngày in: {DateTime.Now:dd/MM/yyyy HH:mm}
";

            return bill;
        }
    }
}

