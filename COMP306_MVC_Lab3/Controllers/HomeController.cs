using Amazon.DynamoDBv2.DataModel;
using COMP306_MVC_Lab3.Areas.Identity.Data;
using COMP306_MVC_Lab3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace COMP306_MVC_Lab3.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        // used to retrieve the logged in user's information
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IDynamoDBContext _context;

        public HomeController(ILogger<HomeController> logger,
            IDynamoDBContext context,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {

            var conditions = new List<ScanCondition>();
            var movies = await _context.ScanAsync<Movie>(conditions).GetRemainingAsync();
            ViewData["UserId"] = _userManager.GetUserId(this.User);
            return View(movies);
        }

        public IActionResult Upsert(string? id)
        {
            if (string.IsNullOrEmpty(id)) 
            { 
                // add movie
                ViewData["UserId"] = _userManager.GetUserId(this.User);
                return View(new Movie());
            }
            else
            {
                // edit movie
                return NotFound();
            }
        }
        [HttpPost]
        public IActionResult Upsert(Movie movie)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            else
            {
                return View(movie);
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