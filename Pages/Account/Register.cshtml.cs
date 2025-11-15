using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.Method;

namespace NewWeb.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ValidationHelper _method = new ValidationHelper();

        public RegisterModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Phone { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public string RePassword { get; set; } = string.Empty;

        [BindProperty]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetInt32("UserID") != null)
            {
                return RedirectToPage("/Index");
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            try
            {
                // Validation
                if (_method.IsEmpty(Phone) || _method.IsEmpty(Password) || _method.IsEmpty(RePassword) || _method.IsEmpty(Name) || _method.IsEmpty(Email))
                {
                    ErrorMessage = "Các trường không được để trống";
                    return Page();
                }

                if (!_method.IsValidPassword(Password))
                {
                    ErrorMessage = "Mật khẩu phải lớn hơn 8 ký tự và có chữ hoa chữ thường";
                    return Page();
                }

                if (!_method.IsValidVietnamPhoneNumber(Phone))
                {
                    ErrorMessage = "Số điện thoại không hợp lệ";
                    return Page();
                }

                if (Password != RePassword)
                {
                    ErrorMessage = "Mật khẩu nhập lại không đúng";
                    return Page();
                }

                if (!_method.IsValidName(Name))
                {
                    ErrorMessage = "Tên không được chứa số hay ký tự đặc biệt";
                    return Page();
                }

                if (!_method.IsValidEmail(Email))
                {
                    ErrorMessage = "Email không hợp lệ";
                    return Page();
                }

                if (_context.Users.Any(u => u.Phone == Phone))
                {
                    ErrorMessage = "Số điện thoại đã có người sử dụng";
                    return Page();
                }

                if (_context.Users.Any(u => u.Email == Email))
                {
                    ErrorMessage = "Email đã có người sử dụng";
                    return Page();
                }

                // Tạo user mới
                var user = new User
                {
                    Phone = Phone,
                    Email = Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(Password),
                    FullName = Name,
                    FailedLoginCount = 0,
                    AccountStatus = "Active",
                    Role = "Customer",
                    CreatedAt = DateTime.Now
                };

                _context.Add(user);
                _context.SaveChanges();

                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToPage("/Account/Login");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau. " + ex.Message;
                return Page();
            }
        }
    }
}

