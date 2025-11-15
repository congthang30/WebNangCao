using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.NVKD.Pages.Invoices
{
    [AuthorizeRole("NVKD", "Admin")]
    public class DetailsInvoiceModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailsInvoiceModel> _logger;

        public DetailsInvoiceModel(ApplicationDbContext context, ILogger<DetailsInvoiceModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Invoice Invoice { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                Invoice = await _context.Invoice
                    .Include(i => i.Order)
                        .ThenInclude(o => o.User)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.Address)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.PaymentMethod)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.Voucher)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.OrderDetails)
                            .ThenInclude(od => od.Product)
                    .Include(i => i.CreatedByUser)
                    .FirstOrDefaultAsync(i => i.InvoiceId == id);

                if (Invoice == null)
                {
                    TempData["Error"] = "Không tìm thấy hóa đơn!";
                    return RedirectToPage("ListInvoice");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading invoice details");
                TempData["Error"] = "Có lỗi hệ thống: " + ex.Message;
                return RedirectToPage("ListInvoice");
            }
        }
    }
}

