using Microsoft.AspNetCore.Mvc;

namespace NewWeb.ViewComponents
{
    public class HeroSectionViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}

