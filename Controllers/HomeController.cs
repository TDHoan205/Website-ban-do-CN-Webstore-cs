using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Webstore.Models;
using Webstore.Services;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IStatisticsService _statisticsService;

        public HomeController(ILogger<HomeController> logger, IStatisticsService statisticsService)
        {
            _logger = logger;
            _statisticsService = statisticsService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _statisticsService.GetDashboardStatsAsync();
            return View(model);
        }

        public IActionResult Landing()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
