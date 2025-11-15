using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Pages.Orders
{
    public class DetailModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailModel> _logger;

        public DetailModel(ApplicationDbContext context, ILogger<DetailModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Order? Order { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserID");
                if (!userId.HasValue)
                {
                    TempData["Error"] = "Vui lòng đăng nhập để xem chi tiết đơn hàng.";
                    return RedirectToPage("/Account/Login");
                }

                Order = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.Address)
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.Voucher)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == id && o.UserId == userId);

                if (Order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng.";
                    return RedirectToPage("/Orders/Index");
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order detail {OrderId}", id);
                TempData["Error"] = "Có lỗi xảy ra khi xem chi tiết đơn hàng.";
                return RedirectToPage("/Orders/Index");
            }
        }
    }
}

