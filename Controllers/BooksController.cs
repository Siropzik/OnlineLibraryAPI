using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineLibraryAPI.Data;
using OnlineLibraryAPI.Models;
using System.Text;

namespace OnlineLibraryAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Список усіх книг (без логіну)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Book>>> GetBooks()
        {
            return await _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .ToListAsync();
        }

        // Отримати книгу з id (без логіну)
        [HttpGet("{id}")]
        public async Task<ActionResult<Book>> GetBook(int id)
        {
            var book = await _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
                return NotFound();

            return book;
        }

        // Додати книгу (тільки admin)
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<Book>> PostBook([FromBody] Book book)
        {
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
        }

        // Видалити книгу (тільки admin)
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound();

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Експорт книг у CSV (тільки admin)
        [HttpGet("export")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ExportBooksToCsv()
        {
            var books = await _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .ToListAsync();

            var csvContent = new StringBuilder();
            csvContent.AppendLine("Id,Title,Authors,Genres");

            foreach (var book in books)
            {
                var authors = (book.Authors != null && book.Authors.Count > 0)
                    ? string.Join(";", book.Authors.Select(a => a.Name))
                    : "None";

                var genres = (book.Genres != null && book.Genres.Count > 0)
                    ? string.Join(";", book.Genres.Select(g => g.Name))
                    : "None";

                csvContent.AppendLine($"{book.Id},{book.Title},{authors},{genres}");
            }

            var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
            return File(bytes, "text/csv", "books.csv");
        }
    }
}
