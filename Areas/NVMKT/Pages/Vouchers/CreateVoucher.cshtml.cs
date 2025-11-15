using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.NVMKT.Pages.Vouchers
{
    [AuthorizeRole("NVMKT", "Admin")]
    public class CreateVoucherModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateVoucherModel> _logger;

        public CreateVoucherModel(ApplicationDbContext context, ILogger<CreateVoucherModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public string Code { get; set; } = string.Empty;

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

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Code))
                {
                    ErrorMessage = "Mã voucher không được để trống.";
                    return Page();
                }
                if (DiscountValue == null || DiscountValue <= 0)
                {
                    ErrorMessage = "Giá trị giảm giá phải lớn hơn 0.";
                    return Page();
                }
                if (StartDate != null && EndDate != null && EndDate < StartDate)
                {
                    ErrorMessage = "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.";
                    return Page();
                }

                var voucher = new Voucher
                {
                    Code = Code.Trim(),
                    Description = Description,
                    DiscountType = DiscountType,
                    DiscountValue = DiscountValue,
                    MaxDiscountAmount = MaxDiscountAmount,
                    MinOrderAmount = MinOrderAmount,
                    Quantity = Quantity,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    IsActive = IsActive ?? true,
                    IsPublic = IsPublic ?? true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = CreatedBy
                };

                await _context.Vouchers.AddAsync(voucher);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo voucher thành công!";
                return RedirectToPage("ListVouchers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating voucher");
                ErrorMessage = "Có lỗi hệ thống. Vui lòng thử lại sau.";
                return Page();
            }
        }
    }
}

