using Amazon.DynamoDBv2.DataModel;

namespace _301230968_Zhang__Lab03.Models
{
    [DynamoDBTable("Movie")]
    public class Movie
    {
        [DynamoDBHashKey] // Partition Key
        public string? MovieID { get; set; }

        [DynamoDBProperty("ReleaseDate")]
        public String ReleaseDate { get; set; }

        [DynamoDBProperty("MovieUrl")]
        public string? MovieUrl { get; set; }

        [DynamoDBProperty("ImageUrl")]
        public string? ImageUrl { get; set; }

        [DynamoDBProperty("Title")]
        public string? Title { get; set; }

        [DynamoDBProperty("Genre")]
        public string? Genre { get; set; }

        [DynamoDBProperty("Directors")]
        public List<string>? Directors { get; set; }

        [DynamoDBProperty("Actors")]
        public List<string>? Actors { get; set; }

        [DynamoDBProperty("Description")]
        public string? Description { get; set; }

        [DynamoDBProperty("Comments")]
        public List<Comment>? Comments { get; set; }

        [DynamoDBProperty("Ratings")]
        public List<Rating>? Ratings { get; set; }

        [DynamoDBProperty("UserID")]
        public string? UserID { get; set; }

        [DynamoDBProperty("AverageRating")]
        public int? AverageRating { get; set; }
    }
}
