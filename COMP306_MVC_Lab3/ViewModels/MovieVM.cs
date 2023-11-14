using COMP306_MVC_Lab3.Models;

namespace COMP306_MVC_Lab3.ViewModels
{
    public class MovieVM
    {
        public Movie? Movie { get; set; }
        public List<Review>? Reviews { get; set; }
    }
}
