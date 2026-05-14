namespace Juan_NET.Web.Controllers;

public class ErrorController : Controller
{
    [Route("Error/{statusCode:int?}")]
    public IActionResult Index(int? statusCode)
    {
        var code = statusCode.GetValueOrDefault(500);

        Response.StatusCode = code;
        ViewData["Title"] = code == 404 ? "Page not found" : "Something went wrong";
        ViewData["ErrorCode"] = code;

        return View();
    }
}
