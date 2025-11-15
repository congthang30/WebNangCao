using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.NVMKT.Pages.Vouchers
{
    [AuthorizeRole("NVMKT", "Admin")]
    public class UpdateVoucherModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UpdateVoucherModel> _logger;

        public UpdateVoucherModel(ApplicationDbContext context, ILogger<UpdateVoucherModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Voucher Voucher { get; set; } = default!;

        [BindProperty]
        public string? Code { get; set; }

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public string? DiscountType { get; set; }

        [BindProperty]
        public decimal? DiscountValue { get; set; }

        [BindProperty]
        public decimal? MaxDiscountAmount { get; set; }

        [BindProperty]
        public decimal? MinOrderAmount { get; set; }

        [BindProperty]
        public int? Quantity { get; set; }

        [BindProperty]
        public DateOnly? StartDate { get; set; }

        [BindProperty]
        public DateOnly? EndDate { get; set; }

        [BindProperty]
        public bool? IsActive { get; set; }

        [BindProperty]
        public bool? IsPublic { get; set; }

        [BindProperty]
        public string? CreatedBy { get; set; }

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Voucher = await _context.Vouchers.FindAsync(id);
            if (Voucher == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            try
            {
                Voucher = await _context.Vouchers.FindAsync(id);
                if (Voucher == null)
                {
                    return NotFound();
                }

                // Validate
                if (string.IsNullOrWhiteSpace(Code))
                {
                    ModelState.AddModelError("Code", "Mã voucher không được để trống.");
                }
                if (DiscountValue == null || DiscountValue <= 0)
                {
                    ModelState.AddModelError("DiscountValue", "Giá trị giảm giá phải lớn hơn 0.");
                }
                if (StartDate != null && EndDate != null && EndDate < StartDate)
                {
                    ModelState.AddModelError("", "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
                }

                if (!ModelState.IsValid)
                {
                    return Page();
                }

                // Cập nhật các trường
                Voucher.Code = Code?.Trim() ?? Voucher.Code;
                Voucher.Description = Description ?? Voucher.Description;
                Voucher.DiscountType = DiscountType ?? Voucher.DiscountType;
                Voucher.DiscountValue = DiscountValue ?? Voucher.DiscountValue;
                Voucher.MaxDiscountAmount = MaxDiscountAmount ?? Voucher.MaxDiscountAmount;
                Voucher.MinOrderAmount = MinOrderAmount ?? Voucher.MinOrderAmount;
                Voucher.Quantity = Quantity ?? Voucher.Quantity;
                Voucher.StartDate = StartDate ?? Voucher.StartDate;
                Voucher.EndDate = EndDate ?? Voucher.EndDate;
                Voucher.IsActive = IsActive ?? Voucher.IsActive;
                Voucher.IsPublic = IsPublic ?? Voucher.IsPublic;
                Voucher.CreatedBy = CreatedBy ?? Voucher.CreatedBy;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật voucher thành công!";
                return RedirectToPage("ListVouchers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating voucher");
                ErrorMessage = "Có lỗi xảy ra: " + ex.Message;
                return Page();
            }
        }
    }
}

