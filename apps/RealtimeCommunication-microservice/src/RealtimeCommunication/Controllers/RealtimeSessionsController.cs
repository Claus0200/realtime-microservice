using Microsoft.AspNetCore.Mvc;

namespace RealtimeCommunication.Controllers
{
    public class RealtimeSessionsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
