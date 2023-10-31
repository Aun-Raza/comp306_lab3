using Amazon.DynamoDBv2.DataModel;

namespace COMP306_MVC_Lab3.Models
{
    [DynamoDBTable("movie")]
    public class Movie
    {
        [DynamoDBHashKey("id")]
        public string? Id { get; set; }

        [DynamoDBProperty("title")]
        public string? Title { get; set; }

        [DynamoDBProperty("genre")]
        public string? Genre { get; set; }

        [DynamoDBProperty("userid")]
        public string? UserID { get; set; }

        [DynamoDBProperty("filepath")]
        public string? FilePath { get; set; }
    }
}
