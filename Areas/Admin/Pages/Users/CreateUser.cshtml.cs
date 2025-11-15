using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.Admin.Pages.Users
{
    [AuthorizeRole("Admin")]
    public class CreateUserModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateUserModel> _logger;

        public CreateUserModel(ApplicationDbContext context, ILogger<CreateUserModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public string Phone { get; set; } = string.Empty;

        [BindProperty]
        public string AccountStatus { get; set; } = "Active";

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Role { get; set; } = "Customer";

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Phone == Phone))
                {
                    ErrorMessage = "Số điện thoại đã được sử dụng bởi người dùng khác.";
                    return Page();
                }
                if (await _context.Users.AnyAsync(u => u.Email == Email))
                {
                    ErrorMessage = "Email đã được sử dụng bởi người dùng khác.";
                    return Page();
                }
                if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(Phone) ||
                    string.IsNullOrEmpty(AccountStatus) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Role))
                {
                    ErrorMessage = "Vui lòng điền đầy đủ thông tin người dùng.";
                    return Page();
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(Password);

                var newUser = new User
                {
                    FullName = Name,
                    Password = hashedPassword,
                    Phone = Phone,
                    AccountStatus = AccountStatus,
                    Email = Email,
                    Role = Role,
                    FailedLoginCount = 0,
                    LockedAt = null,
                    CreatedAt = DateTime.Now
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo người dùng thành công!";
                return RedirectToPage("ListUsers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                ErrorMessage = "Đã có lỗi xảy ra trong quá trình tạo người dùng. Vui lòng thử lại sau.";
                return Page();
            }
        }
    }
}

