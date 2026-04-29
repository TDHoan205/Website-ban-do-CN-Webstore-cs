using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;
using Webstore.Services;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IStatisticsService _statisticsService;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IStatisticsService statisticsService)
        {
            _logger = logger;
            _context = context;
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
