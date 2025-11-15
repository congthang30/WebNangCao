using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.Services.AUTH;

namespace NewWeb.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;

        public LoginModel(ApplicationDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [BindProperty]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (await _authService.IsAuthenticatedAsync())
            {
                return RedirectToPage("/Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                bool isEmail = Name.Contains("@");
                var user = isEmail
                    ? _context.Users.FirstOrDefault(u => u.Email == Name)
                    : _context.Users.FirstOrDefault(u => u.Phone == Name);

                if (user == null)
                {
                    ErrorMessage = "Tài khoản không tồn tại";
                    return Page();
                }

                // Kiểm tra khóa tài khoản
                if (user.AccountStatus == "Locked" && user.LockedAt != null)
                {
                    var minutesLocked = (DateTime.Now - user.LockedAt.Value).TotalMinutes;
                    if (minutesLocked >= 15)
                    {
                        user.AccountStatus = "Active";
                        user.FailedLoginCount = 0;
                        user.LockedAt = null;
                        _context.SaveChanges();
                    }
                    else
                    {
                        ErrorMessage = $"Tài khoản bị khóa. Vui lòng thử lại sau {Math.Ceiling(15 - minutesLocked)} phút.";
                        return Page();
                    }
                }
                else if (user.AccountStatus == "Locked")
                {
                    ErrorMessage = "Tài khoản đã bị khóa.";
                    return Page();
                }

                // Kiểm tra mật khẩu
                if (BCrypt.Net.BCrypt.Verify(Password, user.Password))
                {
                    var loginSuccess = await _authService.LoginAsync(user.Email ?? "", Password);

                    if (loginSuccess)
                    {
                        // Reset failed login
                        user.FailedLoginCount = 0;
                        _context.SaveChanges();

                        // Ghi log
                        var log = new LogActivity
                        {
                            UserId = user.UserId,
                            Action = "Đăng nhập",
                            Timestamp = DateTime.Now,
                        };
                        _context.Add(log);
                        _context.SaveChanges();

                        // Redirect theo role
                        // TODO: Tạo các admin pages sau
                        if (user.Role == "NVKD")
                        {
                            return Redirect("/NVKD/Index");
                        }
                        if (user.Role == "NVKho")
                        {
                            return Redirect("/NVKho/Index");
                        }
                        if (user.Role == "NVKT" || user.Role == "NVMKT")
                        {
                            return Redirect("/NVMKT/Index");
                        }
                        if (user.Role == "Admin")
                        {
                            return Redirect("/Admin/Index");
                        }
                        return RedirectToPage("/Index");
                    }
                    else
                    {
                        ErrorMessage = "Đăng nhập thất bại. Vui lòng thử lại.";
                        return Page();
                    }
                }
                else
                {
                    // Mật khẩu sai
                    user.FailedLoginCount++;

                    if (user.FailedLoginCount >= 3)
                    {
                        user.AccountStatus = "Locked";
                        user.LockedAt = DateTime.Now;
                        ErrorMessage = "Tài khoản đã bị khóa do đăng nhập sai nhiều lần.";
                    }
                    else
                    {
                        ErrorMessage = "Mật khẩu không chính xác.";
                    }

                    _context.SaveChanges();
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Đã xảy ra lỗi trong quá trình đăng nhập: " + ex.Message;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            try
            {
                await _authService.LogoutAsync();
                Response.Cookies.Delete(".AspNetCore.Session");
                Response.Cookies.Delete(".AspNetCore.Cookies");
                return RedirectToPage("/Account/Login");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Đăng xuất thất bại: " + ex.Message;
                return RedirectToPage("/Index");
            }
        }
    }
}

