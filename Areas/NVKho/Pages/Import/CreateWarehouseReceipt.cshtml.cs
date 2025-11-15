using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.NVKho.Pages.Import
{
    [AuthorizeRole("NVKho", "Admin")]
    public class CreateWarehouseReceiptModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateWarehouseReceiptModel> _logger;

        public CreateWarehouseReceiptModel(ApplicationDbContext context, ILogger<CreateWarehouseReceiptModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public string ReceiptNumber { get; set; } = string.Empty;

        [BindProperty]
        public DateTime ReceiptDate { get; set; } = DateTime.Now;

        [BindProperty]
        public string? SupplierName { get; set; }

        [BindProperty]
        public string? Notes { get; set; }

        [BindProperty]
        public List<int> ProductIds { get; set; } = new();

        [BindProperty]
        public List<string> Barcodes { get; set; } = new();

        [BindProperty]
        public List<string> ProductNames { get; set; } = new();

        [BindProperty]
        public List<int> Quantities { get; set; } = new();

        [BindProperty]
        public List<decimal> UnitPrices { get; set; } = new();

        public List<Product> Products { get; set; } = new();
        public List<User> StaffList { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                await LoadDataAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create warehouse receipt page");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải trang tạo phiếu nhập kho.";
                return RedirectToPage("ListWarehouseReceipt");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                int? createdByStaff = HttpContext.Session.GetInt32("UserID");
                if (createdByStaff == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                await EnsureTablesExist();

                if (string.IsNullOrWhiteSpace(ReceiptNumber))
                {
                    ErrorMessage = "Số phiếu không được để trống.";
                    await LoadDataAsync();
                    return Page();
                }

                if (ProductIds == null || ProductIds.Count == 0)
                {
                    ErrorMessage = "Phải có ít nhất một sản phẩm trong phiếu nhập kho.";
                    await LoadDataAsync();
                    return Page();
                }

                var existingReceipt = await _context.WarehouseReceipts
                    .FirstOrDefaultAsync(wr => wr.ReceiptNumber == ReceiptNumber);
                if (existingReceipt != null)
                {
                    ErrorMessage = "Số phiếu đã tồn tại. Vui lòng chọn số phiếu khác.";
                    await LoadDataAsync();
                    return Page();
                }

                var warehouseReceipt = new WarehouseReceipt
                {
                    ReceiptNumber = ReceiptNumber,
                    ReceiptDate = ReceiptDate,
                    CreatedByStaff = createdByStaff,
                    SupplierName = SupplierName ?? "",
                    Notes = Notes ?? "",
                    TotalQuantity = Quantities.Sum(),
                    TotalAmount = Quantities.Zip(UnitPrices, (qty, price) => qty * price).Sum()
                };

                _context.WarehouseReceipts.Add(warehouseReceipt);
                await _context.SaveChangesAsync();

                for (int i = 0; i < ProductIds.Count; i++)
                {
                    var detail = new WarehouseReceiptDetail
                    {
                        ReceiptID = warehouseReceipt.ReceiptID,
                        Barcode = Barcodes[i],
                        ProductName = ProductNames[i],
                        Quantity = Quantities[i],
                        UnitPrice = UnitPrices[i]
                    };

                    _context.WarehouseReceiptDetails.Add(detail);
                    var product = await _context.Products
                        .FirstOrDefaultAsync(p => p.ProductId == ProductIds[i]);

                    if (product != null)
                    {
                        product.Quantity = (product.Quantity ?? 0) + Quantities[i];
                        _context.Products.Update(product);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã tạo phiếu nhập kho {ReceiptNumber} thành công!";
                return RedirectToPage("ListWarehouseReceipt");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating warehouse receipt");
                ErrorMessage = "Có lỗi xảy ra khi tạo phiếu nhập kho: " + ex.Message;
                await LoadDataAsync();
                return Page();
            }
        }

        private async Task LoadDataAsync()
        {
            Products = await _context.Products
                .Where(p => p.StatusProduct != "hidden")
                .ToListAsync();

            StaffList = await _context.Users.ToListAsync();
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

