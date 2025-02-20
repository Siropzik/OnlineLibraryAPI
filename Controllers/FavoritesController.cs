using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLibraryAPI.Data;
using OnlineLibraryAPI.Models;
using System.Security.Claims;

namespace OnlineLibraryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // вимагає будь-який авторизований користувач
    public class FavoritesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FavoritesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Список вибраних книг (для поточного користувача)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Book>>> GetFavorites()
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return Unauthorized();

            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Book)
                .Select(f => f.Book)
                .ToListAsync();

            return Ok(favorites);
        }

        // Додати книгу до вибраного
        [HttpPost]
        public async Task<ActionResult> AddToFavorites([FromBody] int bookId)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return Unauthorized();

            var bookExists = await _context.Books.AnyAsync(b => b.Id == bookId);
            if (!bookExists)
                return NotFound("Книгу не знайдено");

            var alreadyFavorite = await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.BookId == bookId);

            if (alreadyFavorite)
                return BadRequest("Книга вже в обраному");

            var favorite = new Favorite
            {
                UserId = userId,
                BookId = bookId
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            return Ok("Книга додана до обраного");
        }

        // Видалити книгу з вибраного
        [HttpDelete("{bookId}")]
        public async Task<ActionResult> RemoveFromFavorites(int bookId)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return Unauthorized();

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.BookId == bookId);

            if (favorite == null)
                return NotFound("Книгу не знайдено в обраному");

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return Ok("Книжку видалено з обраного");
        }
    }
}
