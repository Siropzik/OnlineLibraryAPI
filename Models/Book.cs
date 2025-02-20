using System.ComponentModel.DataAnnotations;

namespace OnlineLibraryAPI.Models
{
    public class Book
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        public List<Author> Authors { get; set; } = new();
        public List<Genre> Genres { get; set; } = new();
    }
}
