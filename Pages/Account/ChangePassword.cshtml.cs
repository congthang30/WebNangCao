using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.Method;

namespace NewWeb.Pages.Account
{
    public class ChangePasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ValidationHelper _method = new ValidationHelper();

        public ChangePasswordModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public User CurrentUser { get; set; } = default!;

        [BindProperty]
        public string CurrentPassword { get; set; } = string.Empty;

        [BindProperty]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

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

                // Validation
                if (_method.IsEmpty(NewPassword) || _method.IsEmpty(ConfirmPassword) || _method.IsEmpty(CurrentPassword))
                {
                    ErrorMessage = "Các trường không được để trống";
                    return Page();
                }

                if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, CurrentUser.Password))
                {
                    ErrorMessage = "Mật khẩu hiện tại không đúng.";
                    return Page();
                }

                if (NewPassword != ConfirmPassword)
                {
                    ErrorMessage = "Mật khẩu nhập lại không đúng";
                    return Page();
                }

                if (!_method.IsValidPassword(NewPassword))
                {
                    ErrorMessage = "Mật khẩu phải lớn hơn 8 ký tự và có chữ hoa chữ thường";
                    return Page();
                }

                // Cập nhật mật khẩu
                CurrentUser.Password = BCrypt.Net.BCrypt.HashPassword(NewPassword);
                _context.SaveChanges();

                SuccessMessage = "Đổi mật khẩu thành công!";
                
                // Clear form
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
                
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Lỗi hệ thống: " + ex.Message;
                return Page();
            }
        }
    }
}

