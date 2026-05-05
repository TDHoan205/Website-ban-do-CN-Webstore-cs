using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Webstore.Data;
using Webstore.Models;
using Webstore.Services;

namespace Webstore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly IStatisticsService _statisticsService;

        public DashboardController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }

        public async Task<IActionResult> Index()
        {
            var model = await _statisticsService.GetDashboardStatsAsync();
            return View(model);
        }
    }
}
