using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.NVKho.Pages.Import
{
    [AuthorizeRole("NVKho", "Admin")]
    public class ListWarehouseReceiptModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ListWarehouseReceiptModel> _logger;

        public ListWarehouseReceiptModel(ApplicationDbContext context, ILogger<ListWarehouseReceiptModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<WarehouseReceipt> WarehouseReceipts { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                await EnsureTablesExist();

                WarehouseReceipts = await _context.WarehouseReceipts
                    .Include(wr => wr.Staff)
                    .Include(wr => wr.ReceiptDetails)
                    .OrderByDescending(wr => wr.ReceiptDate)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading warehouse receipts");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải danh sách phiếu nhập kho.";
                WarehouseReceipts = new List<WarehouseReceipt>();
                return Page();
            }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnGetReceiptDetailsAsync(int receiptId)
        {
            try
            {
                var receipt = await _context.WarehouseReceipts
                    .Include(wr => wr.Staff)
                    .Include(wr => wr.ReceiptDetails)
                    .FirstOrDefaultAsync(wr => wr.ReceiptID == receiptId);

                if (receipt == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy phiếu nhập kho." });
                }

                var data = new
                {
                    receiptId = receipt.ReceiptID,
                    receiptNumber = receipt.ReceiptNumber,
                    receiptDate = receipt.ReceiptDate.ToString("dd/MM/yyyy HH:mm"),
                    supplierName = receipt.SupplierName,
                    staffName = receipt.Staff?.FullName ?? "Không xác định",
                    totalAmount = receipt.TotalAmount.ToString("N0"),
                    notes = receipt.Notes,
                    receiptDetails = receipt.ReceiptDetails.Select(rd => new
                    {
                        productName = rd.ProductName,
                        barcode = rd.Barcode,
                        quantity = rd.Quantity,
                        unitPrice = rd.UnitPrice.ToString("N0"),
                        totalPrice = rd.TotalPrice.ToString("N0")
                    }).ToList()
                };

                return new JsonResult(new { success = true, data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting receipt details");
                return new JsonResult(new { success = false, message = "Có lỗi xảy ra khi tải chi tiết phiếu nhập kho." });
            }
        }

        private async Task EnsureTablesExist()
        {
            try
            {
                try
                {
                    await _context.Database.ExecuteSqlRawAsync("SELECT TOP 1 * FROM WarehouseReceipts");
                    return;
                }
                catch
                {
                    // Bảng chưa tồn tại, tạo mới
                }

                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE WarehouseReceipts (
                        ReceiptID INT IDENTITY(1,1) PRIMARY KEY,
                        ReceiptNumber NVARCHAR(20) NOT NULL,
                        ReceiptDate DATETIME2 NOT NULL,
                        CreatedByStaff INT,
                        SupplierName NVARCHAR(100),
                        Notes NVARCHAR(MAX),
                        TotalQuantity INT NOT NULL,
                        TotalAmount DECIMAL(18,2) NOT NULL
                    )");

                await _context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE WarehouseReceiptDetails (
                        DetailID INT IDENTITY(1,1) PRIMARY KEY,
                        ReceiptID INT NOT NULL,
                        Barcode NVARCHAR(50) NOT NULL,
                        ProductName NVARCHAR(100),
                        Quantity INT NOT NULL,
                        UnitPrice DECIMAL(18,2) NOT NULL,
                        TotalPrice AS (Quantity * UnitPrice) PERSISTED
                    )");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not ensure tables exist");
                // Không throw exception để tránh crash ứng dụng
            }
        }
    }
}

