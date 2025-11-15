using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Authentication.Google;  // Cần cài package: Microsoft.AspNetCore.Authentication.Google
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.Method;
using System.Security.Claims;

namespace NewWeb.Pages.Account
{
    public class GoogleLoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ValidationHelper _method = new ValidationHelper();

        public GoogleLoginModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Login Handler - Redirect to Google
        public async Task<IActionResult> OnGetLoginAsync()
        {
            var redirectUrl = Url.Page("/Account/GoogleLogin", pageHandler: "Callback", values: null, protocol: Request.Scheme);
            await HttpContext.ChallengeAsync(
                "Google",
                new AuthenticationProperties
                {
                    RedirectUri = redirectUrl
                });
            return new EmptyResult();
        }

        // Callback Handler - Process Google response
        public async Task<IActionResult> OnGetCallbackAsync()
        {
            try
            {
                var result = await HttpContext.AuthenticateAsync("Google"); // GoogleDefaults.AuthenticationScheme
                if (!result.Succeeded)
                {
                    TempData["Error"] = "Đăng nhập Google thất bại. Vui lòng thử lại.";
                    return RedirectToPage("/Account/Login");
                }

                var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
                var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var fullName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

                if (string.IsNullOrEmpty(email))
                {
                    TempData["Error"] = "Không thể lấy thông tin email từ Google. Vui lòng thử lại.";
                    return RedirectToPage("/Account/Login");
                }

                // Kiểm tra user đã tồn tại
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == email);

                if (existingUser != null)
                {
                    // Kiểm tra trạng thái tài khoản
                    if (existingUser.AccountStatus == "Locked" && existingUser.LockedAt != null)
                    {
                        var minutesLocked = (DateTime.Now - existingUser.LockedAt.Value).TotalMinutes;
                        if (minutesLocked >= 15)
                        {
                            existingUser.AccountStatus = "Active";
                            existingUser.FailedLoginCount = 0;
                            existingUser.LockedAt = null;
                            _context.SaveChanges();
                        }
                        else
                        {
                            TempData["Error"] = $"Tài khoản bị khóa. Vui lòng thử lại sau {Math.Ceiling(15 - minutesLocked)} phút.";
                            return RedirectToPage("/Account/Login");
                        }
                    }
                    else if (existingUser.AccountStatus == "Locked")
                    {
                        TempData["Error"] = "Tài khoản đã bị khóa.";
                        return RedirectToPage("/Account/Login");
                    }

                    // Đăng nhập thành công
                    HttpContext.Session.Clear();
                    HttpContext.Session.SetInt32("UserID", existingUser.UserId);
                    HttpContext.Session.SetString("Phone", existingUser.Phone ?? "");
                    HttpContext.Session.SetString("UserName", existingUser.FullName ?? "");
                    HttpContext.Session.SetString("Role", existingUser.Role);

                    // Reset failed login count
                    existingUser.FailedLoginCount = 0;

                    // Ghi log đăng nhập
                    var log = new LogActivity
                    {
                        UserId = existingUser.UserId,
                        Action = "Đăng nhập bằng Google",
                        Timestamp = DateTime.Now,
                    };
                    _context.Add(log);
                    _context.SaveChanges();

                    // Redirect theo role
                    // TODO: Tạo các admin pages sau
                    if (existingUser.Role == "NVKD")
                    {
                        return Redirect("~/NVKD/Home/Index"); // Chưa tạo page này
                    }
                    if (existingUser.Role == "NVKho")
                    {
                        return Redirect("~/NVKho/Home/Index"); // Chưa tạo page này
                    }
                    if (existingUser.Role == "NVKT")
                    {
                        return Redirect("~/NVMKT/Home/Index"); // Chưa tạo page này
                    }
                    if (existingUser.Role == "Admin")
                    {
                        return Redirect("~/Admin/Home/Index"); // Chưa tạo page này
                    }
                    return RedirectToPage("/Index");
                }
                else
                {
                    // Lưu thông tin Google vào session để sử dụng khi đặt mật khẩu
                    string emailName = string.IsNullOrEmpty(fullName) ? email.Split('@')[0] : fullName;
                    HttpContext.Session.SetString("GoogleEmail", email);
                    HttpContext.Session.SetString("GoogleName", emailName);

                    // Redirect đến trang đặt mật khẩu
                    return RedirectToPage("/Account/SetPassword");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Đã xảy ra lỗi trong quá trình đăng nhập Google: " + ex.Message;
                return RedirectToPage("/Account/Login");
            }
        }
    }
}

