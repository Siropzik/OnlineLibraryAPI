using System.ComponentModel.DataAnnotations;

namespace OnlineLibraryAPI.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "client"; // admin или client

        public List<Favorite> Favorites { get; set; } = new();
    }
}
