using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NewWeb.Data;
using NewWeb.Models;
using NewWeb.Method;

namespace NewWeb.Pages.Account.Addresses
{
    public class UpdateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ValidationHelper _method = new ValidationHelper();

        public UpdateModel(ApplicationDbContext context)
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

        [BindProperty]
        public int AddressId { get; set; }

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            var address = _context.Addresses.FirstOrDefault(a => a.AddressId == id && a.UserId == userId.Value);
            if (address == null)
            {
                TempData["Error"] = "Không tìm thấy địa chỉ hoặc bạn không có quyền sửa.";
                return RedirectToPage("/Account/Addresses/Index");
            }

            // Load dữ liệu
            AddressId = address.AddressId;
            City = address.City ?? "";
            District = address.District ?? "";
            Ward = address.Ward ?? "";
            Street = address.Street ?? "";
            Phone = address.Phone ?? "";

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                if (userId == null)
                {
                    return RedirectToPage("/Account/Login");
                }

                var address = _context.Addresses.FirstOrDefault(a => a.AddressId == id && a.UserId == userId.Value);
                if (address == null)
                {
                    TempData["Error"] = "Không tìm thấy địa chỉ hoặc bạn không có quyền sửa.";
                    return RedirectToPage("/Account/Addresses/Index");
                }

                // Validation
                if (_method.IsEmpty(City) || _method.IsEmpty(District) || _method.IsEmpty(Ward) || 
                    _method.IsEmpty(Street) || _method.IsEmpty(Phone))
                {
                    ErrorMessage = "Các trường không được bỏ trống";
                    AddressId = id;
                    return Page();
                }

                if (!_method.IsValidVietnamPhoneNumber(Phone))
                {
                    ErrorMessage = "Số điện thoại không hợp lệ";
                    AddressId = id;
                    return Page();
                }

                // Cập nhật thông tin
                address.City = City;
                address.District = District;
                address.Ward = Ward;
                address.Street = Street;
                address.Phone = Phone;

                _context.SaveChanges();
                TempData["Success"] = "Cập nhật địa chỉ thành công!";
                return RedirectToPage("/Account/Addresses/Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi hệ thống: " + ex.Message;
                return RedirectToPage("/Account/Addresses/Index");
            }
        }
    }
}

