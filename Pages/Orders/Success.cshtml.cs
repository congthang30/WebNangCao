using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NewWeb.Pages.Orders
{
    public class SuccessModel : PageModel
    {
        public IActionResult OnGet()
        {
            return Page();
        }
    }
}

