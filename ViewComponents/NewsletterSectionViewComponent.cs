using Microsoft.AspNetCore.Mvc;

namespace NewWeb.ViewComponents
{
    public class NewsletterSectionViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}

