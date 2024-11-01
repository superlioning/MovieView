using _301230968_Zhang__Lab03.Connector;
using _301230968_Zhang__Lab03.Models;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Mvc;
using _301230968_Zhang__Lab03.Service;
using Amazon.S3.Model;
using Amazon.S3;
using _301230968_Zhang__Lab03.Repository;

namespace _301230968_Zhang__Lab03.Controllers
{
    public class MovieController : Controller
    {
        private readonly MovieService _movieService;
        private readonly ILogger<MovieController> _logger;
        private readonly string[] allowedVideoExtensions = { ".mp4", ".avi", ".mov", ".wmv" };
        private readonly string[] allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

        public MovieController(MovieService movieService, ILogger<MovieController> logger)
        {
            _movieService = movieService ?? throw new ArgumentNullException(nameof(movieService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Message"] = "Please login to continue.";
                return RedirectToAction("Login", "User");
            }

            try
            {
                var movies = await _movieService.GetMoviesAsync();
                return View(movies);
            }
            catch (AmazonDynamoDBException ex)
            {
                _logger.LogError(ex, "DynamoDB error while retrieving movies");
                TempData["Message"] = "Unable to retrieve movies. Please try again later.";
                return View(new List<Movie>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving movies");
                TempData["Message"] = "An unexpected error occurred. Please try again later.";
                return View(new List<Movie>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> AddMovie()
        {
            try
            {
                if (HttpContext.Session.GetString("UserId") == null)
                {
                    return RedirectToAction("Login", "User");
                }

                var genres = await _movieService.GetGenresAsync();
                if (genres == null)
                {
                    throw new InvalidOperationException("Failed to load genres");
                }

                ViewBag.Genres = genres;
                return View(new MovieViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading AddMovie page");
                TempData["Message"] = "An error occurred while loading the page. Please try again.";
                return View(new MovieViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddMovie(MovieViewModel model)
        {
            try
            {
                // Validate files before model state validation
                if (!ValidateFileTypes(model))
                {
                    ViewBag.Genres = await _movieService.GetGenresAsync();
                    return View(model);
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.Genres = await _movieService.GetGenresAsync();
                    return View(model);
                }

                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Message"] = "Please login to continue.";
                    return RedirectToAction("Login", "User");
                }

                // Check if movie already exists
                if (await _movieService.MovieExistsAsync(model.MovieID))
                {
                    TempData["MovieExists"] = true;
                    TempData["ExistingMovieId"] = model.MovieID;
                    ViewBag.Genres = await _movieService.GetGenresAsync();
                    return View(model);
                }

                model.UserID = userId;
                await _movieService.AddMovieAsync(model);
                TempData["Message"] = "Movie added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred while adding the movie.");
                ViewBag.Genres = await _movieService.GetGenresAsync();
                return View(model);
            }
        }

        private bool ValidateFileTypes(MovieViewModel model)
        {
            bool isValid = true;

            if (model.MovieFile != null)
            {
                var extension = Path.GetExtension(model.MovieFile.FileName).ToLowerInvariant();
                if (!allowedVideoExtensions.Contains(extension))
                {
                    ModelState.AddModelError("MovieFile", "Please upload a valid video file (mp4, avi, mov, or wmv)");
                    isValid = false;
                }
            }

            if (model.ImageFile != null)
            {
                var extension = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                if (!allowedImageExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ImageFile", "Please upload a valid image file (jpg, jpeg, png, or gif)");
                    isValid = false;
                }
            }

            return isValid;
        }


        [HttpPost]
        public IActionResult CancelAddMovie()
        {
            return Json(new { redirectToUrl = Url.Action("Index") });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
            {
                return Json(new { success = false, message = "Please login to continue." });
            }

            try
            {
                await _movieService.DeleteMovieAsync(id);
                return Json(new { success = true, message = "Movie deleted successfully" });
            }
            catch (AmazonDynamoDBException ex)
            {
                _logger.LogError(ex, "DynamoDB error while deleting movie {MovieId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = "Database error occurred. Please try again." });
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "S3 error while deleting movie files {MovieId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = "File storage error occurred. Please try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting movie {MovieId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditMovie(string id)
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login");
            }

            try
            {
                var movie = await _movieService.GetMovieByIdAsync(id);
                Console.WriteLine($"MovieID: {movie.MovieID}, Directors count: {movie.Directors?.Count ?? 0}");

                var model = new MovieViewModel
                {
                    MovieID = movie.MovieID,
                    Title = movie.Title,
                    Genre = movie.Genre,
                    Description = movie.Description,
                    Directors = movie.Directors ?? new List<string>(),
                    Actors = movie.Actors ?? new List<string>(),
                    ReleaseDate = movie.ReleaseDate
                };
                //get genre for dropdown
                List<string> genres = await _movieService.GetGenresAsync();
                ViewBag.Genres = genres;


                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Error retrieving movie details.");
                return View(new MovieViewModel()); // Return an empty model to the view if there's an error
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditMovie(MovieViewModel model)
        {
            model.UserID = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(model.UserID))
            {
                return RedirectToAction("Login");
            }

            if (!ValidateFileTypes(model))
            {
                ViewBag.Genres = await _movieService.GetGenresAsync();
                return View(model);
            }

            try
            {
                await _movieService.UpdateMovieAsync(model);
                TempData["Message"] = "Movie updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unknown error occurred while updating the movie.");
                ViewBag.Genres = await _movieService.GetGenresAsync();
                return View(model);
            }
        }

        //Show details of the movie
        [HttpGet]
        public async Task<IActionResult> ShowDetail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound("Movie ID is required.");
            }

            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Login", "User");
            }

            try
            {
                Movie movie = await _movieService.GetMovieByIdAsync(id);
                if (movie == null)
                {
                    return NotFound("Movie not found.");
                }
                return View(movie);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListMovieRating(double? minRating = null)
        {
            if (HttpContext.Session.GetString("UserId") != null)
            {
                List<Movie> movies = await _movieService.GetMoviesByRatingAsync(minRating);
                return View(movies);
            }
            else
            {
                return RedirectToAction("Login", "User");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListMovieGenre(string genre)
        {
            if (HttpContext.Session.GetString("UserId") != null)
            {
                // Filter movies by genre if a genre parameter is provided
                List<Movie> movies = await _movieService.GetMoviesByGenreAsync(genre);
                //get genre for dropdown
                List<string> genres = await _movieService.GetGenresAsync();
                ViewBag.Genres = genres;

                return View(movies);
            }
            else
            {
                return RedirectToAction("Login", "User");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(string movieId, string message)
        {
            // Create the new comment
            var comment = new Comment
            {
                UserID = HttpContext.Session.GetString("UserId"),
                Message = message,
                CommentDateTime = DateTime.Now
            };

            try
            {
                await _movieService.AddCommentToMovieAsync(movieId, comment);
                return Ok();
            }
            catch (Exception ex)
            {
                // Handle error appropriately, for simplicity I'm just returning a BadRequest here
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditComment(string movieId, string originalMessage, string editedMessage, string commentDateTime)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Please login to continue.");
            }

            try
            {
                var movie = await _movieService.GetMovieByIdAsync(movieId);
                if (movie == null)
                    return NotFound();

                DateTime originalCommentDateTime = DateTime.Parse(commentDateTime);

                var comment = movie.Comments.FirstOrDefault(c =>
                    c.UserID == userId &&
                    c.Message == originalMessage &&
                    c.CommentDateTime == originalCommentDateTime);

                if (comment != null)
                {
                    if ((DateTime.Now - comment.CommentDateTime).TotalHours > 24)
                    {
                        return BadRequest("Cannot edit comments older than 24 hours.");
                    }

                    comment.Message = editedMessage;
                    comment.CommentDateTime = DateTime.Now;

                    await _movieService.EditCommentMovieAsync(movie);

                    return Ok();
                }
                else
                {
                    return NotFound("Comment not found.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to edit the comment: " + ex.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteComment(string movieId, string message, string commentDateTime)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Please login to continue.");
            }

            try
            {
                var movie = await _movieService.GetMovieByIdAsync(movieId);
                if (movie == null)
                    return NotFound();

                DateTime originalCommentDateTime = DateTime.Parse(commentDateTime, null, System.Globalization.DateTimeStyles.RoundtripKind);

                var comment = movie.Comments.FirstOrDefault(c =>
                    c.UserID == userId &&
                    c.Message == message &&
                    c.CommentDateTime == originalCommentDateTime);

                if (comment != null)
                {
                    if ((DateTime.Now - comment.CommentDateTime).TotalHours > 24)
                    {
                        return BadRequest("Cannot delete comments older than 24 hours.");
                    }

                    movie.Comments.Remove(comment);

                    await _movieService.EditCommentMovieAsync(movie);

                    return Ok();
                }
                else
                {
                    return NotFound("Comment not found.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to delete the comment: " + ex.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> UpdateAverageRating(string movieId, string userId, int userRating)
        {
            try
            {
                var movie = await _movieService.GetMovieByIdAsync(movieId);
                if (movie == null)
                    return NotFound("Movie not found.");


                // If Ratings was null, initialize it
                if (movie.Ratings == null)
                {
                    movie.Ratings = new List<Rating>();
                    var ratings = new Rating
                    {
                        UserID = userId,
                        RateValue = userRating,
                        RatingDateTime = DateTime.Now
                    };
                    movie.Ratings.Add(ratings);
                }

                var existingRating = movie.Ratings.FirstOrDefault(r => r.UserID == userId);

                if (existingRating != null)
                {
                    existingRating.RateValue = userRating;
                    existingRating.RatingDateTime = DateTime.Now;
                }
                else
                {
                    movie.Ratings.Add(new Rating { UserID = userId, RateValue = userRating, RatingDateTime = DateTime.Now });
                }
                var totalRatingValue = movie.Ratings.Sum(r => r.RateValue);
                var totalRatingsCount = movie.Ratings.Count();
                movie.AverageRating = totalRatingValue / totalRatingsCount;

                await _movieService.EditCommentMovieAsync(movie);

                return Ok(); // Return success
            }
            catch (Exception ex)
            {
                // Handle any errors
                return BadRequest($"Failed to update the rating: {ex.Message}");
            }
        }

        public async Task<IActionResult> DownloadMovie(string movieId)
        {
            // Fetch the movie details based on the movieId
            var movie = await _movieService.GetMovieByIdAsync(movieId);

            if (movie == null || string.IsNullOrEmpty(movie.MovieUrl))
            {
                return NotFound();
            }

            // Download the movie file from AWS S3
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(movie.MovieUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var stream = await response.Content.ReadAsStreamAsync();
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                var fileName = Path.GetFileName(new Uri(movie.MovieUrl).LocalPath);

                return File(stream, contentType, fileName);
            }
        }
    }
}
