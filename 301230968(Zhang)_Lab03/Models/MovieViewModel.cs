using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace _301230968_Zhang__Lab03.Models
{
    public class MovieViewModel
    {
        [Required(ErrorMessage = "IMDb ID is required")]
        [RegularExpression(@"^tt\d{7,8}$", ErrorMessage = "Please enter a valid IMDb ID (e.g., tt1234567)")]
        public string MovieID { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Genre is required")]
        public string Genre { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Director(s) is required")]
        [MinLength(1, ErrorMessage = "At least one director is required")]
        public List<string> Directors { get; set; } = new List<string>();

        [Required(ErrorMessage = "Actor(s) is required")]
        [MinLength(1, ErrorMessage = "At least one actor is required")]
        public List<string> Actors { get; set; } = new List<string>();

        [Required(ErrorMessage = "Movie file is required")]
        public IFormFile MovieFile { get; set; }

        [Required(ErrorMessage = "Image file is required")]
        public IFormFile ImageFile { get; set; }

        public string? MovieFileUrl { get; set; }
        public string? ImageFileUrl { get; set; }

        [RegularExpression(@"^(\d{4})(-(0[1-9]|1[0-2]))?(-(0[1-9]|[12]\d|3[01]))?$",
        ErrorMessage = "Please enter a valid date (YYYY, YYYY-MM, or YYYY-MM-DD)")]
        public string? ReleaseDate { get; set; }

        public string UserID { get; set; }
    }
}
