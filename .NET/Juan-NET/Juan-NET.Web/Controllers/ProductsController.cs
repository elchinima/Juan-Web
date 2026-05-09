using Microsoft.AspNetCore.Mvc;

namespace Juan_NET.Web.Controllers
{
    public class ProductsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
