using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace COMP306_MVC_Lab3.Models
{
    [DynamoDBTable("movie")]
    public class Movie
    {
        [Key]
        [DynamoDBHashKey("id")]
        public string? Id { get; set; }

        [Required]
        [MinLength(5)]
        [DynamoDBProperty("title")]
        public string? Title { get; set; }
        
        [Required]
        [MinLength(3)]
        [MaxLength(10)]
        [DynamoDBProperty("genre")]
        public string? Genre { get; set; }
        
        [Required]
        [DynamoDBProperty("userid")]
        public string? UserID { get; set; }

        [ValidateNever]
        [DynamoDBProperty("filepath")]
        public string? FilePath { get; set; }

        [ValidateNever]
        [DynamoDBProperty("imagepath")]
        public string? ImagePath { get; set; }
    }
}
