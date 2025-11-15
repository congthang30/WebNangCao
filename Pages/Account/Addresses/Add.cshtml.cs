using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.Method;

namespace NewWeb.Pages.Account.Addresses
{
    public class AddModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ValidationHelper _method = new ValidationHelper();

        public AddModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string City { get; set; } = string.Empty;

        [BindProperty]
        public string District { get; set; } = string.Empty;

        [BindProperty]
        public string Ward { get; set; } = string.Empty;

        [BindProperty]
        public string Street { get; set; } = string.Empty;

        [BindProperty]
        public string Phone { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetInt32("UserID") == null)
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
                var user = _context.Users.Find(userId);
                if (user == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                // Validation
                if (_method.IsEmpty(City) || _method.IsEmpty(District) || _method.IsEmpty(Ward) || 
                    _method.IsEmpty(Street) || _method.IsEmpty(Phone))
                {
                    ErrorMessage = "Các trường không được bỏ trống";
                    return Page();
                }

                if (!_method.IsValidVietnamPhoneNumber(Phone))
                {
                    ErrorMessage = "Số điện thoại không hợp lệ";
                    return Page();
                }

                // Tạo địa chỉ mới
                var address = new Address
                {
                    City = City,
                    District = District,
                    Ward = Ward,
                    Street = Street,
                    Phone = Phone,
                    CreatedAt = DateTime.Now,
                    UserId = userId!.Value
                };

                _context.Add(address);
                _context.SaveChanges();

                TempData["Success"] = "Thêm địa chỉ thành công!";
                return RedirectToPage("/Account/Addresses/Index");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Lỗi hệ thống: " + ex.Message;
                return Page();
            }
        }
    }
}

