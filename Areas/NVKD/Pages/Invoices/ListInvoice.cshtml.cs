using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.NVKD.Pages.Invoices
{
    [AuthorizeRole("NVKD", "Admin")]
    public class ListInvoiceModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ListInvoiceModel> _logger;

        public ListInvoiceModel(ApplicationDbContext context, ILogger<ListInvoiceModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Invoice> Invoices { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Invoices = await _context.Invoice
                    .Include(i => i.Order)
                        .ThenInclude(o => o.User)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.Address)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.PaymentMethod)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.OrderDetails)
                            .ThenInclude(od => od.Product)
                    .Include(i => i.CreatedByUser)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading invoices");
                ErrorMessage = "Có lỗi hệ thống: " + ex.Message;
                Invoices = new List<Invoice>();
                return Page();
            }
        }
    }
}

