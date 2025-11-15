using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.Admin.Pages.Users
{
    [AuthorizeRole("Admin")]
    public class EditUserModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditUserModel> _logger;

        public EditUserModel(ApplicationDbContext context, ILogger<EditUserModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public User User { get; set; } = default!;

        [BindProperty]
        public string? Name { get; set; }

        [BindProperty]
        public string? Password { get; set; }

        [BindProperty]
        public string? Phone { get; set; }

        [BindProperty]
        public string? AccountStatus { get; set; }

        [BindProperty]
        public string? Email { get; set; }

        [BindProperty]
        public string? Role { get; set; }

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            User = await _context.Users.FindAsync(id);
            if (User == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            try
            {
                User = await _context.Users.FindAsync(id);
                if (User == null)
                {
                    return NotFound();
                }

                if (!string.IsNullOrEmpty(Name))
                {
                    User.FullName = Name;
                }
                if (!string.IsNullOrEmpty(Password))
                {
                    User.Password = BCrypt.Net.BCrypt.HashPassword(Password);
                }
                if (!string.IsNullOrEmpty(Phone))
                {
                    if (await _context.Users.AnyAsync(u => u.Phone == Phone && u.UserId != id))
                    {
                        ErrorMessage = "SDT đã được sử dụng bởi người dùng khác.";
                        return Page();
                    }
                    User.Phone = Phone;
                }
                if (!string.IsNullOrEmpty(AccountStatus))
                {
                    User.AccountStatus = AccountStatus;
                }
                if (!string.IsNullOrEmpty(Email))
                {
                    if (await _context.Users.AnyAsync(u => u.Email == Email && u.UserId != id))
                    {
                        ErrorMessage = "Email đã được sử dụng bởi người dùng khác.";
                        return Page();
                    }
                    User.Email = Email;
                }
                if (!string.IsNullOrEmpty(Role))
                {
                    User.Role = Role;
                }
                _context.Users.Update(User);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật người dùng thành công!";
                return RedirectToPage("ListUsers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                ErrorMessage = "Đã có lỗi xảy ra trong quá trình cập nhật người dùng. Vui lòng thử lại sau.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    TempData["ErrorMessage"] = "ID người dùng không hợp lệ";
                    return RedirectToPage("ListUsers");
                }
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy người dùng";
                    return RedirectToPage("ListUsers");
                }
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Xóa người dùng thành công!";
                return RedirectToPage("ListUsers");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Error deleting user - foreign key constraint");
                TempData["ErrorMessage"] = "Không thể xóa người dùng vì đang được sử dụng ở bảng khác.";
                return RedirectToPage("ListUsers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                TempData["ErrorMessage"] = "Đã có lỗi xảy ra trong quá trình xóa người dùng. Vui lòng thử lại sau.";
                return RedirectToPage("ListUsers");
            }
        }
    }
}

