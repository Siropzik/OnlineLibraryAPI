﻿using System.ComponentModel.DataAnnotations;

namespace OnlineLibraryAPI.Models
{
    public class Author
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public List<Book> Books { get; set; } = new();
    }
}
