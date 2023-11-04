﻿using Amazon.DynamoDBv2.DataModel;
using COMP306_MVC_Lab3.Areas.Identity.Data;
using COMP306_MVC_Lab3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;

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
            // get all movies
            var movies = await _context.ScanAsync<Movie>(default).GetRemainingAsync();
            ViewData["UserId"] = _userManager.GetUserId(this.User);
            return View(movies);
        }

        public async Task<IActionResult> Upsert(string? id)
        {
            if (string.IsNullOrEmpty(id)) 
            {
                // add movie view
                var newMovie = new Movie();
                newMovie.UserID = _userManager.GetUserId(this.User);
                return View(newMovie);
            }
            else
            {
                // edit movie view
                Movie? movieRetrieved = await _context.LoadAsync<Movie>(id);
                if (movieRetrieved != null) 
                {
                    var userId = _userManager.GetUserId(this.User);
                    if (userId != movieRetrieved.UserID)
                    {
                        return Unauthorized();
                    }
                    return View(movieRetrieved);
                }
                else
                {
                    return NotFound();
                }
            }
        }
        [HttpPost]
        public async Task<IActionResult> Upsert(Movie movie)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(movie.Id))
                {
                    // for adding movie
                    movie.Id = Guid.NewGuid().ToString();
                    await _context.SaveAsync(movie);
                    return RedirectToAction("Index");
                }
                else
                {
                    // for updating movie
                    var userId = _userManager.GetUserId(this.User);
                    if (userId != movie.UserID)
                    {
                        return Unauthorized();
                    }
                    await _context.SaveAsync(movie);
                    return RedirectToAction("Index");
                }
            }
            else
            {
                return View(movie);
            }
        }

        public async Task<IActionResult> Delete(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            else
            {
                Movie? movieRetrieved = await _context.LoadAsync<Movie>(id);
                if (movieRetrieved != null)
                {
                    var userId = _userManager.GetUserId(this.User);
                    if (userId != movieRetrieved.UserID)
                    {
                        return Unauthorized();
                    }
                    return View(movieRetrieved);
                }
                else
                {
                    return NotFound();
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Movie movie)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(movie.Id))
                {
                    return NotFound();
                }
                else
                {
                    var userId = _userManager.GetUserId(this.User);
                    if (userId != movie.UserID)
                    {
                        return Unauthorized();
                    }
                    await _context.DeleteAsync<Movie>(movie.Id);
                    return RedirectToAction("Index");
                }
            }
            else
            {
                return View("Index");
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}