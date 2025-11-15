using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NewWeb.Areas.Admin.Attributes;
using NewWeb.Data;
using NewWeb.Models;

namespace NewWeb.Areas.Admin.Pages.Users
{
    [AuthorizeRole("Admin")]
    public class ListUsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ListUsersModel> _logger;

        public ListUsersModel(ApplicationDbContext context, ILogger<ListUsersModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<User> Users { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Users = await _context.Users.ToListAsync();
            return Page();
        }
    }
}

