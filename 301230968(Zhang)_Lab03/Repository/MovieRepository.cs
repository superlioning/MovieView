using _301230968_Zhang__Lab03.Connector;
using _301230968_Zhang__Lab03.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;

namespace _301230968_Zhang__Lab03.Repository
{
    public class MovieRepository
    {
        private readonly AWSConnector _awsConnector;
        private readonly string _tableName = "Movie";
        private readonly Table _table;
        private readonly ILogger<MovieRepository> _logger;

        public MovieRepository(AWSConnector awsConnector, ILogger<MovieRepository> logger)
        {
            _awsConnector = awsConnector;
            _table = _awsConnector.LoadContentTable(_tableName);
            _logger = logger;
        }

        public async Task<List<Movie>> GetMoviesAsync()
        {
            DynamoDBContext context = _awsConnector.Context;
            var movies = await context.ScanAsync<Movie>(new List<ScanCondition>()).GetRemainingAsync();
            return movies;
        }

        public async Task AddMovieAsync(Movie movie)
        {
            try
            {
                var document = new Document();
                document["MovieID"] = movie.MovieID;  // Use the provided IMDb ID instead of generating new
                document["ReleaseDate"] = movie.ReleaseDate;
                document["MovieUrl"] = movie.MovieUrl;
                document["ImageUrl"] = movie.ImageUrl;
                document["Title"] = movie.Title;
                document["Genre"] = movie.Genre;

                // Convert and Save Directors as DynamoDBList
                var directorsList = new DynamoDBList();
                foreach (var director in movie.Directors)
                {
                    directorsList.Add(director);
                }
                document["Directors"] = directorsList;

                // Convert and Save Actors as DynamoDBList
                var actorsList = new DynamoDBList();
                foreach (var actor in movie.Actors)
                {
                    actorsList.Add(actor);
                }
                document["Actors"] = actorsList;

                document["Description"] = movie.Description;
                document["UserID"] = movie.UserID;
                document["AverageRating"] = 0;

                await _table.PutItemAsync(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie {MovieID}: {Message}", movie.MovieID, ex.Message);
                throw new InvalidOperationException($"Failed to add movie with ID {movie.MovieID}.", ex);
            }
        }

        public async Task<bool> MovieExistsAsync(string movieId)
        {
            try
            {
                var document = new Document();
                document["MovieID"] = movieId;

                var response = await _table.GetItemAsync(document);
                return response != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if movie exists {MovieID}: {Message}", movieId, ex.Message);
                throw new InvalidOperationException($"Failed to check if movie exists with ID {movieId}.", ex);
            }
        }

        public async Task DeleteMovieAsync(string movieId)
        {
            var deleteItemSpec = new DeleteItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
        {
            {"MovieID", new AttributeValue { S = movieId }}
        }
            };

            try
            {
                await _awsConnector.DynamoClient.DeleteItemAsync(deleteItemSpec);
            }
            catch (AmazonDynamoDBException ex)
            {
                throw new Exception($"DynamoDB error: {ex.ErrorCode} - {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"General error: {ex.Message}", ex);
            }
        }

        public async Task<Movie> GetMovieByIdAsync(string movieId)
        {
            var getItemSpec = new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
        {
            {"MovieID", new AttributeValue { S = movieId }}
        }
            };

            var result = await _awsConnector.DynamoClient.GetItemAsync(getItemSpec);

            if (result.Item == null || !result.IsItemSet)
            {
                return null; // or throw an exception if you prefer
            }

            return DocumentToMovie(Document.FromAttributeMap(result.Item));

        }

        private Movie DocumentToMovie(Document document)
        {
            // Convert the Document to a Movie object
            var movie = new Movie
            {
                MovieID = document["MovieID"].AsString(),
                Title = document["Title"].AsString(),
                Genre = document["Genre"].AsString(),
                MovieUrl = document["MovieUrl"].AsString(),
                ImageUrl = document["ImageUrl"].AsString(),
                Description = document["Description"].AsString(),
                UserID = document["UserID"].AsString(),

            };
            if (document.ContainsKey("AverageRating"))
            {
                movie.AverageRating = document["AverageRating"].AsInt();
            }

            if (document.ContainsKey("Ratings") && document["Ratings"].AsListOfDocument().Any())
            {
                movie.Ratings = document["Ratings"]
                    .AsListOfDocument()
                    .Select(doc => new Rating
                    {
                        UserID = doc["UserID"].AsString(),
                        RateValue = doc["RateValue"].AsInt(),
                        RatingDateTime = DateTime.Parse(doc["RatingDateTime"].AsString())
                    })
                    .ToList();
            }

            if (document.ContainsKey("Comments") && document["Comments"].AsListOfDocument().Any())
            {
                movie.Comments = document["Comments"]
                    .AsListOfDocument()
                    .Select(doc => new Comment
                    {
                        UserID = doc["UserID"].AsString(),
                        Message = doc["Message"].AsString(),
                        CommentDateTime = DateTime.Parse(doc["CommentDateTime"].AsString(), null, System.Globalization.DateTimeStyles.RoundtripKind) // ISO8601 parsing
                    })
                    .ToList();
            }


            // Convert the Directors from Document list to List<string>
            // Check if the Directors key exists in the document
            if (document.ContainsKey("Directors"))
            {
                try
                {
                    // Cast the Directors value to DynamoDBList
                    var directorsDynamoList = document["Directors"] as DynamoDBList;
                    if (directorsDynamoList != null)
                    {
                        // Ensure movie.Directors is initialized
                        if (movie.Directors == null)
                        {
                            movie.Directors = new List<string>();
                        }

                        // Clear any existing data in movie.Directors if necessary
                        movie.Directors.Clear();

                        foreach (var directorEntry in directorsDynamoList.Entries)
                        {
                            var directorString = directorEntry as Primitive;
                            if (directorString != null)
                            {
                                movie.Directors.Add(directorString.AsString());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    // Handle the error if it's not a list or any other issues
                }
            }

            if (document.ContainsKey("Actors"))
            {
                try
                {
                    // Cast the Actors value to DynamoDBList
                    var actorsDynamoList = document["Actors"] as DynamoDBList;
                    if (actorsDynamoList != null)
                    {
                        // Ensure movie.Actors is initialized
                        if (movie.Actors == null)
                        {
                            movie.Actors = new List<string>();
                        }

                        // Clear any existing data in movie.Actors if necessary
                        movie.Actors.Clear();

                        foreach (var actorEntry in actorsDynamoList.Entries)
                        {
                            var actorString = actorEntry as Primitive;
                            if (actorString != null)
                            {
                                movie.Actors.Add(actorString.AsString());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    // Handle the error if it's not a list or any other issues
                }
            }
            return movie;
        }


        public async Task UpdateMovieAsync(Movie movie)
        {
            var document = new Document();
            document["MovieID"] = movie.MovieID;
            document["MovieUrl"] = movie.MovieUrl;
            document["ImageUrl"] = movie.ImageUrl;
            document["Title"] = movie.Title;
            document["Genre"] = movie.Genre;

            // Update Directors
            if (movie.Directors != null)
            {
                var directorsList = new DynamoDBList();
                foreach (var director in movie.Directors)
                {
                    directorsList.Add(director);
                }
                document["Directors"] = directorsList;
            }

            // Update Actors
            if (movie.Actors != null)
            {
                var actorsList = new DynamoDBList();
                foreach (var actor in movie.Actors)
                {
                    actorsList.Add(actor);
                }
                document["Actors"] = actorsList;
            }


            document["Description"] = movie.Description;
            document["UserID"] = movie.UserID;

            await _table.PutItemAsync(document);
        }


        public async Task<List<Movie>> GetMoviesByMinRatingAsync(double minRating)
        {
            var context = _awsConnector.Context;

            var conditions = new List<ScanCondition>
    {
        new ScanCondition("AverageRating", ScanOperator.GreaterThanOrEqual, minRating)
    };

            var searchResults = await context.QueryAsync<Movie>(conditions, new DynamoDBOperationConfig
            {
                IndexName = "MovieRating-index", // Specify the GSI name here            
            }).GetRemainingAsync();

            return searchResults.ToList();
        }

        public async Task<List<Movie>> GetMoviesByGenreAsync(string genre)
        {
            var context = _awsConnector.Context;

            var conditions = new List<ScanCondition>
    {
        new ScanCondition("Genre", ScanOperator.Equal, genre)
    };

            var searchResults = await context.QueryAsync<Movie>(conditions, new DynamoDBOperationConfig
            {
                IndexName = "MovieGenre-index", // Specify the GSI name here
                OverrideTableName = "Movie"
            }).GetRemainingAsync();

            return searchResults.ToList();
        }

        public async Task SaveMovieAsync(Movie movie)
        {
            var request = new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
        {
            { "MovieID", new AttributeValue { S = movie.MovieID } }
            // Add sort key here if you have one
        },
                AttributeUpdates = new Dictionary<string, AttributeValueUpdate>()
            };

            // Handle Ratings
            if (movie.Ratings != null && movie.Ratings.Any())
            {
                var ratingsList = movie.Ratings.Select(rating => new AttributeValue
                {
                    M = new Dictionary<string, AttributeValue>
            {
                { "UserID", new AttributeValue { S = rating.UserID } },
                { "RateValue", new AttributeValue { N = rating.RateValue.ToString() } },
                { "RatingDateTime", new AttributeValue { S = rating.RatingDateTime.ToString("o") } }
            }
                }).ToList();
                request.AttributeUpdates["Ratings"] = new AttributeValueUpdate
                {
                    Value = new AttributeValue { L = ratingsList },
                    Action = "PUT"
                };
            }
            else
            {
                // Remove the Ratings attribute if the list is empty
                request.AttributeUpdates["Ratings"] = new AttributeValueUpdate { Action = "DELETE" };
            }

            // Handle Comments
            if (movie.Comments != null && movie.Comments.Any())
            {
                var commentsList = movie.Comments.Select(comment => new AttributeValue
                {
                    M = new Dictionary<string, AttributeValue>
            {
                { "UserID", new AttributeValue { S = comment.UserID } },
                { "Message", new AttributeValue { S = comment.Message } },
                { "CommentDateTime", new AttributeValue { S = comment.CommentDateTime.ToString("o") } } // ISO8601 format
            }
                }).ToList();
                request.AttributeUpdates["Comments"] = new AttributeValueUpdate
                {
                    Value = new AttributeValue { L = commentsList },
                    Action = "PUT"
                };
            }
            else
            {
                // Remove the Comments attribute if the list is empty
                request.AttributeUpdates["Comments"] = new AttributeValueUpdate { Action = "DELETE" };
            }

            // Update AverageRating
            request.AttributeUpdates["AverageRating"] = new AttributeValueUpdate
            {
                Value = new AttributeValue { N = movie.AverageRating.ToString() },
                Action = "PUT"
            };

            await _awsConnector.DynamoClient.UpdateItemAsync(request);
        }

        public Task<List<string>> GetGenresAsync()
        {
            return Task.FromResult(new List<string>
            {
                "Action",
                "Adventure",
                "Animation",
                "Comedy",
                "Crime",
                "Documentary",
                "Drama",
                "Fantasy",
                "History",
                "Horror",
                "Sci-Fi",
                "Sport",
                "Thriller"
            });
        }
    }
}
