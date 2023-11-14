using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using COMP306_MVC_Lab3.Areas.Identity.Data;
using COMP306_MVC_Lab3.Models;
using COMP306_MVC_Lab3.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net;

namespace COMP306_MVC_Lab3.Controllers
{
    [Authorize]
    public class AppController : Controller
    {
        // used to retrieve the logged in user's information
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDynamoDBContext _context;
        private readonly IAmazonS3 _s3Client;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly String BucketName = "lab3-mvc-group15";

        public AppController(
            IDynamoDBContext context,
            UserManager<ApplicationUser> userManager,
            IAmazonS3 s3Client,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _context = context;
            _s3Client = s3Client;
            _webHostEnvironment = webHostEnvironment;
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
                // adding image file
                IFormFile? imgFile = Request.Form.Files["img_file"];
                if (imgFile != null)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imgFile.FileName);
                    string movieImagePath = Path.Combine(wwwRootPath, @"images\movie");

                    if (!string.IsNullOrEmpty(movie.ImagePath))
                    {
                        //delete the old image
                        var oldImagePath =
                            Path.Combine(wwwRootPath, movie.ImagePath.TrimStart('\\'));

                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(movieImagePath, fileName), FileMode.Create))
                    {
                        imgFile.CopyTo(fileStream);
                    }

                    movie.ImagePath = @"\images\movie\" + fileName;
                }
                // adding movie file (S3)
                IFormFile? movieFile = Request.Form.Files["movie_file"];
                if (movieFile != null)
                {
                    var bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, BucketName);
                    if (!bucketExists)
                    {
                        var bucketRequest = new PutBucketRequest()
                        {
                            BucketName = this.BucketName,
                            UseClientRegion = true,
                        };
                        await _s3Client.PutBucketAsync(bucketRequest);
                    }
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(movieFile.FileName);
                    string fileExtension = Path.GetExtension(movieFile.FileName);
                    string uniqueId = Guid.NewGuid().ToString();
                    string uniqueFileName = $"{fileNameWithoutExtension}-{uniqueId}{fileExtension}";
                    var objectRequest = new PutObjectRequest()
                    {
                        BucketName = this.BucketName,
                        Key = uniqueFileName,
                        InputStream = movieFile.OpenReadStream(),
                    };
                    await _s3Client.PutObjectAsync(objectRequest);
                    movie.FilePath = uniqueFileName;
                }

                // for adding movie
                if (string.IsNullOrEmpty(movie.Id))
                {
                    movie.Id = Guid.NewGuid().ToString();
                    await _context.SaveAsync(movie);
                    return RedirectToAction("Index");
                }
                // for updating movie
                else
                {
                    var userId = _userManager.GetUserId(this.User);
                    if (userId != movie.UserID)
                    {
                        return Unauthorized();
                    }
                    await _context.SaveAsync(movie);
                    return RedirectToAction("Index");
                }
            }
            // ModelState is invalid
            else
                return View(movie);
        }
        
        public async Task<IActionResult> GetFile(string? objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return NotFound();

            var bucketExists = await AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, BucketName);
            if (!bucketExists)
            {
                var bucketRequest = new PutBucketRequest()
                {
                    BucketName = this.BucketName,
                    UseClientRegion = true,
                };
                await _s3Client.PutBucketAsync(bucketRequest);
            }
            try
            {
                var response = await _s3Client.GetObjectAsync(this.BucketName, objectName);
                return File(response.ResponseStream, response.Headers.ContentType, objectName);

            } catch (Exception)
            {
                return NotFound();
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

        public async Task<IActionResult> ViewMovieDetail(string? id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            Movie? movieRetrieved = await _context.LoadAsync<Movie>(id);
            if (movieRetrieved is null)
                return NotFound();

            List<ScanCondition> conditions = new List<ScanCondition>
            {
                new ScanCondition("MovieID", ScanOperator.Equal, id)
            };
            List<Review>? reviews = await _context.ScanAsync<Review>(conditions).GetRemainingAsync();

            ViewData["UserId"] = _userManager.GetUserId(this.User);
            MovieVM movieVM = new MovieVM()
            {
                Movie = movieRetrieved,
                Reviews = reviews
            };
            return View(movieVM);            
        }

        public async Task<IActionResult> ReviewMovie(string? movieId, string? reviewId)
        {
            if (string.IsNullOrEmpty(movieId))
            {
                return NotFound();
            }

            Movie? movieRetrieved = await _context.LoadAsync<Movie>(movieId);
            if (movieRetrieved is null)
                return NotFound();

            var userId = _userManager.GetUserId(this.User);
            // create review
            if (reviewId == null)
            {
                Review newReview = new Review()
                {
                    MovieID = movieId,
                    UserID = userId,
                };
                return View(newReview);
            }
            // edit review
            else
            {
                Review? reviewRetrieved = await _context.LoadAsync<Review>(reviewId);
                if (reviewRetrieved != null)
                {
                    if (userId != reviewRetrieved.UserID)
                    {
                        return Unauthorized();
                    }
                    return View(reviewRetrieved);
                }
                else
                {
                    return NotFound();
                }
            }
        }
        [HttpPost]
        public async Task<IActionResult> ReviewMovie(Review review)
        {
            if (ModelState.IsValid)
            {
                // for adding review
                if (string.IsNullOrEmpty(review.Id))
                {
                    review.Id = Guid.NewGuid().ToString();
                    await _context.SaveAsync(review);
                }
                // for updating movie
                else
                {
                    var userId = _userManager.GetUserId(this.User);
                    if (userId != review.UserID)
                    {
                        return Unauthorized();
                    }
                    await _context.SaveAsync(review);
                }
                return RedirectToAction("ViewMovieDetail", new { id = review.MovieID });
            }
            // modelState is invalid
            return View(review);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}