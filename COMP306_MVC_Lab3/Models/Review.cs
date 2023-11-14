using Amazon.DynamoDBv2.DataModel;
using System.ComponentModel.DataAnnotations;

namespace COMP306_MVC_Lab3.Models
{
    [DynamoDBTable("review")]
    public class Review
    {
        [Key]
        [DynamoDBHashKey("id")]
        public string? Id { get; set; }

        [Required]
        [MinLength(3)]
        [DynamoDBProperty("title")]
        public string? Title { get; set; }

        [Required]
        [MinLength(3)]
        [DynamoDBProperty("comment")]
        public string? Comment { get; set; }

        [Required]
        [Range(0, 10)]
        [DynamoDBProperty("rating")]
        [Display(Name = "Rating (0-10)")]
        public double? Rating { get; set; }

        [Required]
        [DynamoDBProperty("userid")]
        public string? UserID { get; set; }

        [Required]
        [DynamoDBProperty("movieid")]
        public string? MovieID { get; set; }
    }
}
