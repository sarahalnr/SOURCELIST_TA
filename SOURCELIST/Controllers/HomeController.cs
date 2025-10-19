using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sourcelist.Models;
using sourcelist.Models.ViewModels;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using sourcelist.Data;
using Microsoft.Extensions.Logging;

namespace sourcelist.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSourcelistChartData(int year)
        {
            if (year == 0)
            {
                year = DateTime.Now.Year;
            }

            var viewModel = new ChartDataViewModel();
            for (int month = 1; month <= 12; month++)
            {
                viewModel.Labels.Add(CultureInfo.GetCultureInfo("id-ID").DateTimeFormat.GetAbbreviatedMonthName(month));
                viewModel.ApproveData.Add(0); // Completed
                viewModel.RejectData.Add(0);  // Pending
                viewModel.RejectedData.Add(0); // Rejected
                viewModel.TotalData.Add(0);
            }

            try
            {
                var rawDataForYear = await _context.Sourcelists
                    .Where(req => req.SubmittedDate.Year == year)
                    .Select(req => new {
                        req.SubmittedDate,
                        req.ApprovalStatus
                    })
                    .ToListAsync();

                var groupedDataStatus = rawDataForYear
                    .Select(req => new {
                        req.SubmittedDate,
                        CleanStatus = req.ApprovalStatus.Trim().ToUpper()
                    })
                    .Where(req => req.CleanStatus == "COMPLETED" || req.CleanStatus == "PENDING" || req.CleanStatus == "REJECTED")
                    .GroupBy(req => req.SubmittedDate.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        ApproveCount = g.Count(req => req.CleanStatus == "COMPLETED"),
                        PendingCount = g.Count(req => req.CleanStatus == "PENDING"),
                        RejectedCount = g.Count(req => req.CleanStatus == "REJECTED")
                    });

                var groupedDataTotal = rawDataForYear
                    .GroupBy(req => req.SubmittedDate.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        TotalCount = g.Count()
                    });

                foreach (var item in groupedDataStatus)
                {
                    int index = item.Month - 1;
                    viewModel.ApproveData[index] = item.ApproveCount;
                    viewModel.RejectData[index] = item.PendingCount;
                    viewModel.RejectedData[index] = item.RejectedCount;
                }

                foreach (var item in groupedDataTotal)
                {
                    int index = item.Month - 1;
                    viewModel.TotalData[index] = item.TotalCount;
                }

                return Json(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error di GetSourcelistChartData");
                return StatusCode(500, new { message = "Terjadi error di server.", details = ex.Message });
            }
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