using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.Method;

namespace NewWeb.Pages.Account
{
    public class UpdateAccountModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ValidationHelper _method = new ValidationHelper();

        public UpdateAccountModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public User CurrentUser { get; set; } = default!;

        [BindProperty]
        public string? Name { get; set; }

        [BindProperty]
        public string? Email { get; set; }

        [BindProperty]
        public string? Phone { get; set; }

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            CurrentUser = _context.Users.Find(userId.Value)!;
            if (CurrentUser == null)
            {
                return RedirectToPage("/Account/Login");
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                if (userId == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                CurrentUser = _context.Users.Find(userId.Value)!;
                if (CurrentUser == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                // Kiểm tra mật khẩu
                if (_method.IsEmpty(Password))
                {
                    ErrorMessage = "Bạn phải nhập mật khẩu để xác nhận thay đổi.";
                    return Page();
                }

                if (!BCrypt.Net.BCrypt.Verify(Password, CurrentUser.Password))
                {
                    ErrorMessage = "Mật khẩu không đúng.";
                    return Page();
                }

                // Cập nhật các trường
                if (!_method.IsEmpty(Name))
                {
                    CurrentUser.FullName = Name;
                }

                if (!_method.IsEmpty(Email))
                {
                    if (!_method.IsValidEmail(Email))
                    {
                        ErrorMessage = "Email không hợp lệ";
                        return Page();
                    }
                    if (_context.Users.Any(u => u.Email == Email && u.UserId != CurrentUser.UserId))
                    {
                        ErrorMessage = "Email đã có người sử dụng";
                        return Page();
                    }
                    CurrentUser.Email = Email;
                }

                if (!_method.IsEmpty(Phone))
                {
                    if (!_method.IsValidVietnamPhoneNumber(Phone))
                    {
                        ErrorMessage = "Số điện thoại không hợp lệ";
                        return Page();
                    }
                    if (_context.Users.Any(u => u.Phone == Phone && u.UserId != CurrentUser.UserId))
                    {
                        ErrorMessage = "Số điện thoại đã có người sử dụng";
                        return Page();
                    }
                    CurrentUser.Phone = Phone;
                }

                _context.SaveChanges();
                SuccessMessage = "Cập nhật tài khoản thành công!";
                
                // Clear input fields after success
                Name = null;
                Email = null;
                Phone = null;
                Password = string.Empty;
                
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Có lỗi hệ thống: " + ex.Message;
                return Page();
            }
        }
    }
}

