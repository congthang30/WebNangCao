using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.NVMKT.Pages.Vouchers
{
    [AuthorizeRole("NVMKT", "Admin")]
    public class ListVouchersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ListVouchersModel> _logger;

        public ListVouchersModel(ApplicationDbContext context, ILogger<ListVouchersModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string? Code { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? DiscountType { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsActive { get; set; }

        public List<Voucher> Vouchers { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            try
            {
                var query = _context.Vouchers.AsQueryable();

                if (!string.IsNullOrWhiteSpace(Code))
                    query = query.Where(v => v.Code != null && v.Code.Contains(Code));

                if (!string.IsNullOrWhiteSpace(DiscountType))
                    query = query.Where(v => v.DiscountType == DiscountType);

                if (IsActive.HasValue)
                    query = query.Where(v => v.IsActive == IsActive);

                Vouchers = query.OrderByDescending(v => v.CreatedAt).ToList();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading vouchers list");
                ErrorMessage = "Có lỗi xảy ra khi tải danh sách voucher: " + ex.Message;
                Vouchers = new List<Voucher>();
                return Page();
            }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            try
            {
                var voucher = await _context.Vouchers.FindAsync(id);
                if (voucher == null)
                {
                    return new JsonResult(new { success = false, message = "Không tìm thấy voucher" });
                }

                voucher.IsActive = !(voucher.IsActive ?? false);
                await _context.SaveChangesAsync();

                var message = voucher.IsActive == true ? "Voucher đã được kích hoạt." : "Voucher đã bị ẩn.";
                return new JsonResult(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling voucher status");
                return new JsonResult(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}

