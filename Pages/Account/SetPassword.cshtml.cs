using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.Method;

namespace NewWeb.Pages.Account
{
    public class SetPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ValidationHelper _method = new ValidationHelper();

        public SetPasswordModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            // Kiểm tra xem có thông tin Google trong session không
            Email = HttpContext.Session.GetString("GoogleEmail") ?? "";
            UserName = HttpContext.Session.GetString("GoogleName") ?? "";

            if (string.IsNullOrEmpty(Email))
            {
                TempData["Error"] = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToPage("/Account/Login");
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            try
            {
                // Lấy thông tin từ session
                Email = HttpContext.Session.GetString("GoogleEmail") ?? "";
                UserName = HttpContext.Session.GetString("GoogleName") ?? "";

                if (string.IsNullOrEmpty(Email))
                {
                    TempData["Error"] = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.";
                    return RedirectToPage("/Account/Login");
                }

                // Validation
                if (_method.IsEmpty(Password) || _method.IsEmpty(ConfirmPassword))
                {
                    ErrorMessage = "Vui lòng nhập đầy đủ thông tin.";
                    return Page();
                }

                if (Password != ConfirmPassword)
                {
                    ErrorMessage = "Mật khẩu xác nhận không khớp.";
                    return Page();
                }

                if (!_method.IsValidPassword(Password))
                {
                    ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa và chữ thường.";
                    return Page();
                }

                // Kiểm tra email đã tồn tại chưa (tránh trường hợp user tạo account trong khi đang đặt password)
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == Email);
                if (existingUser != null)
                {
                    TempData["Error"] = "Email này đã được sử dụng. Vui lòng đăng nhập bình thường.";
                    return RedirectToPage("/Account/Login");
                }

                // Tạo user mới
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(Password);
                var newUser = new User
                {
                    Email = Email,
                    FullName = UserName,
                    Phone = null,
                    Password = passwordHash,
                    Role = "Customer",
                    AccountStatus = "Active",
                    FailedLoginCount = 0,
                    CreatedAt = DateTime.Now,
                };

                _context.Add(newUser);
                _context.SaveChanges();

                // Đăng nhập tự động
                HttpContext.Session.Clear();
                HttpContext.Session.SetInt32("UserID", newUser.UserId);
                HttpContext.Session.SetString("Phone", "");
                HttpContext.Session.SetString("UserName", newUser.FullName ?? "");
                HttpContext.Session.SetString("Role", newUser.Role);

                // Ghi log đăng ký
                var logRegister = new LogActivity
                {
                    UserId = newUser.UserId,
                    Action = "Đăng ký bằng Google",
                    Timestamp = DateTime.Now,
                };
                _context.Add(logRegister);
                _context.SaveChanges();

                TempData["Success"] = "Tạo tài khoản thành công! Chào mừng bạn đến với TechStore.";
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Đã xảy ra lỗi: " + ex.Message;
                return Page();
            }
        }
    }
}

