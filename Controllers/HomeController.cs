using Graduation_Project_Backend.Models.ViewModels;
using Graduation_Project_Backend.Service.Session;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project_Backend.Controllers
{
    public sealed class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
            => Redirect("/portal/login");

        [HttpGet("test-console")]
        public IActionResult TestConsole()
        {
            var model = new TestConsoleViewModel
            {
                SessionHeaderName = SessionConstants.HeaderName,
                PointsStreamEndpoint = "/api/realtime/points-stream"
            };

            return View("Index", model);
        }
    }
}
