using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.NVKho.Pages.Export
{
    [AuthorizeRole("NVKho", "Admin")]
    public class DetailsInvoiceWarehouseModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailsInvoiceWarehouseModel> _logger;

        public DetailsInvoiceWarehouseModel(ApplicationDbContext context, ILogger<DetailsInvoiceWarehouseModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public InvoiceWareHouse Invoice { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                Invoice = await _context.InvoiceWareHouses
                    .Include(i => i.Order)
                        .ThenInclude(o => o.User)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.Address)
                    .Include(i => i.Order)
                        .ThenInclude(o => o.OrderDetails)
                            .ThenInclude(od => od.Product)
                                .ThenInclude(p => p.Brand)
                    .Include(i => i.Staff)
                    .FirstOrDefaultAsync(i => i.InvoiceWareHouseID == id);

                if (Invoice == null)
                {
                    return NotFound();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading invoice warehouse details");
                return NotFound();
            }
        }
    }
}

