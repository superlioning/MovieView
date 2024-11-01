using Amazon.S3.Transfer;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.DynamoDBv2;
using _301230968_Zhang__Lab03.Connector;
using _301230968_Zhang__Lab03.Models;
using _301230968_Zhang__Lab03.Repository;

namespace _301230968_Zhang__Lab03.Service
{
    public class MovieService
    {
        private readonly MovieRepository _movieRepository;
        private readonly AWSConnector _awsConnector;
        private readonly ILogger<MovieService> _logger;
        private const string BucketName = "pdf-repository-for-assignment-2";

        public MovieService(MovieRepository movieRepository, AWSConnector awsConnector, ILogger<MovieService> logger)
        {
            _movieRepository = movieRepository ?? throw new ArgumentNullException(nameof(movieRepository));
            _awsConnector = awsConnector ?? throw new ArgumentNullException(nameof(awsConnector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Movie>> GetMoviesAsync()
        {
            return await _movieRepository.GetMoviesAsync();
        }

        public async Task<bool> MovieExistsAsync(string movieId)
        {
            return await _movieRepository.MovieExistsAsync(movieId);
        }

        public async Task AddMovieAsync(MovieViewModel model)
        {
            try
            {
                var movieUrl = model.MovieFile != null ? await UploadFileAsync(model.MovieFile) : null;
                var imageUrl = model.ImageFile != null ? await UploadFileAsync(model.ImageFile) : null;

                var movie = new Movie
                {
                    MovieID = model.MovieID,
                    Title = model.Title,
                    Genre = model.Genre,
                    Description = model.Description,
                    Directors = model.Directors ?? new List<string>(),
                    Actors = model.Actors ?? new List<string>(),
                    MovieUrl = movieUrl,
                    ImageUrl = imageUrl,
                    UserID = model.UserID,
                    ReleaseDate = model.ReleaseDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd")
                };

                await _movieRepository.AddMovieAsync(movie);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie {Title}", model.Title);
                throw new InvalidOperationException("Failed to add movie.", ex);
            }
        }

        private async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null", nameof(file));
            }

            var fileName = Path.GetFileName(file.FileName);
            var key = $"{Guid.NewGuid()}-{fileName}";

            try
            {
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = file.OpenReadStream(),
                    Key = key,
                    BucketName = BucketName,
                    CannedACL = S3CannedACL.PublicRead
                };

                var fileTransferUtility = new TransferUtility(_awsConnector.S3Client);
                await fileTransferUtility.UploadAsync(uploadRequest);

                return $"https://{BucketName}.s3.amazonaws.com/{key}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName}", fileName);
                throw new InvalidOperationException("Failed to upload file.", ex);
            }
        }

        public async Task DeleteMovieAsync(string movieId)
        {
            try
            {
                // Retrieve the movie by ID
                var movie = await _movieRepository.GetMovieByIdAsync(movieId);

                if (movie == null)
                {
                    throw new Exception("Movie not found!");
                }

                // Delete from S3
                await DeleteFileFromS3Async(movie.MovieUrl);
                await DeleteFileFromS3Async(movie.ImageUrl);

                // Delete from DynamoDB
                await _movieRepository.DeleteMovieAsync(movieId);
            }
            catch (AmazonS3Exception s3Ex)
            {
                // Handle S3 exceptions, check if it's a permission issue
                if (s3Ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new Exception($"S3 Exception. {s3Ex.Message}", s3Ex);
                }
                throw; // Re-throw the original exception if it's not a Forbidden status
            }
            catch (AmazonDynamoDBException dbEx)
            {
                // Handle DynamoDB exceptions, check if it's a permission issue
                if (dbEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    throw new Exception($"DynamoDB Exception. {dbEx.Message}", dbEx);
                }
                throw; // Re-throw the original exception if it's not a Forbidden status
            }
            catch (Exception ex)
            {
                // General exception handling
                throw new Exception($"An error occurred: {ex.Message}", ex);
            }
        }


        public async Task DeleteFileFromS3Async(string fileUrl)
        {
            var key = Path.GetFileName(new Uri(fileUrl).AbsolutePath);
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = BucketName,
                Key = key
            };

            await _awsConnector.S3Client.DeleteObjectAsync(deleteObjectRequest);
        }

        public async Task<Movie> GetMovieByIdAsync(string movieId)
        {
            return await _movieRepository.GetMovieByIdAsync(movieId);
        }
        
        public async Task UpdateMovieAsync(MovieViewModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            try
            {
                var existingMovie = await _movieRepository.GetMovieByIdAsync(model.MovieID);
                if (existingMovie == null)
                {
                    _logger.LogWarning("Attempt to update non-existent movie with ID {MovieId}", model.MovieID);
                    throw new InvalidOperationException($"Movie with ID {model.MovieID} not found.");
                }

                if (model.MovieFile != null)
                {
                    try
                    {
                        await DeleteFileFromS3Async(existingMovie.MovieUrl);
                        var movieUrl = await UploadFileAsync(model.MovieFile);
                        model.MovieFileUrl = movieUrl;
                    }
                    catch (AmazonS3Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update movie file for movie {MovieId}", model.MovieID);
                        throw new InvalidOperationException("Failed to update movie file.", ex);
                    }
                }

                if (model.ImageFile != null)
                {
                    try
                    {
                        await DeleteFileFromS3Async(existingMovie.ImageUrl);
                        var imageUrl = await UploadFileAsync(model.ImageFile);
                        model.ImageFileUrl = imageUrl;
                    }
                    catch (AmazonS3Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update image file for movie {MovieId}", model.MovieID);
                        throw new InvalidOperationException("Failed to update image file.", ex);
                    }
                }

                // Map the ViewModel to the Domain Model
                var movieToUpdate = new Movie
                {
                    MovieID = model.MovieID,
                    Title = model.Title,
                    Genre = model.Genre,
                    Description = model.Description,
                    Directors = model.Directors,
                    Actors = model.Actors,
                    MovieUrl = model.MovieFileUrl ?? existingMovie.MovieUrl,
                    ImageUrl = model.ImageFileUrl ?? existingMovie.ImageUrl,
                    UserID = model.UserID,
                    ReleaseDate = existingMovie.ReleaseDate
                };

                await _movieRepository.UpdateMovieAsync(movieToUpdate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update movie {MovieId}", model.MovieID);
                throw new InvalidOperationException("Failed to update movie.", ex);
            }
        }

        public async Task<List<Movie>> GetMoviesByRatingAsync(double? minRating = null)
        {
            if (minRating.HasValue)
            {
                return await _movieRepository.GetMoviesByMinRatingAsync(minRating.Value);
            }
            else
            {
                return await _movieRepository.GetMoviesAsync();
            }
        }

        public async Task<List<Movie>> GetMoviesByGenreAsync(string genre)
        {
            if (!string.IsNullOrWhiteSpace(genre))
            {
                return await _movieRepository.GetMoviesByGenreAsync(genre);
            }
            else
            {
                return await _movieRepository.GetMoviesAsync();
            }
        }

        public async Task AddCommentToMovieAsync(string movieId, Comment comment)
        {
            var movie = await _movieRepository.GetMovieByIdAsync(movieId);
            if (movie == null)
                throw new Exception("Movie not found");

            movie.Comments = movie.Comments ?? new List<Comment>();
            movie.Comments.Add(comment);

            await _movieRepository.SaveMovieAsync(movie);
        }

        public async Task EditCommentMovieAsync(Movie movie)
        {

            await _movieRepository.SaveMovieAsync(movie);
        }

        public async Task<List<string>> GetGenresAsync()
        {
            return await _movieRepository.GetGenresAsync();
        }
    }
}
