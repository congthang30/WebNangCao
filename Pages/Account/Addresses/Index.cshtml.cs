using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Pages.Account.Addresses
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Address> Addresses { get; set; } = new List<Address>();

        public IActionResult OnGet()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            Addresses = _context.Addresses
                .Where(a => a.UserId == userId.Value)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();

            return Page();
        }

        public IActionResult OnPostDelete(int id)
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
                    TempData["Error"] = "Không tìm thấy địa chỉ hoặc bạn không có quyền xóa.";
                    return RedirectToPage();
                }

                _context.Addresses.Remove(address);
                _context.SaveChanges();
                
                TempData["Success"] = "Xóa địa chỉ thành công!";
                return RedirectToPage();
            }
            catch
            {
                TempData["Error"] = "Không thể xóa địa chỉ này";
                return RedirectToPage();
            }
        }
    }
}

