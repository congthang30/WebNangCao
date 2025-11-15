using Microsoft.AspNetCore.Mvc;

namespace NewWeb.ViewComponents
{
    public class StatsSectionViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}

