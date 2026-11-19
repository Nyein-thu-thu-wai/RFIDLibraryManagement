using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RfidLibraryApi.Data;
using RfidLibraryApi.Models;

namespace RfidLibraryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var query = db.Books.Include(b => b.RfidTags).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(b => b.Title.Contains(search) || b.Author.Contains(search) ||
                                     b.CallNo.Contains(search) ||
                                     b.RfidTags.Any(t => t.RfidNo.Contains(search)));
        return Ok(await query.OrderBy(b => b.Title).ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var book = await db.Books.Include(b => b.RfidTags).FirstOrDefaultAsync(b => b.BookId == id);
        return book is null ? NotFound(new { message = "Book not found." }) : Ok(book);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Book book)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (await db.Books.AnyAsync(b => b.CallNo == book.CallNo))
            return Conflict(new { message = "CALL_NO_EXISTS" });
        book.BookId = string.IsNullOrWhiteSpace(book.BookId) ? Guid.NewGuid().ToString("N")[..20] : book.BookId;
        db.Books.Add(book);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = book.BookId }, book);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, Book input)
    {
        var book = await db.Books.FindAsync(id);
        if (book is null) return NotFound();
        book.CallNo = input.CallNo;
        book.Title = input.Title;
        book.Author = input.Author;
        book.MarcData = input.MarcData;
        book.AcquiredDate = input.AcquiredDate;
        await db.SaveChangesAsync();
        return Ok(book);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var book = await db.Books.FindAsync(id);
        if (book is null) return NotFound();
        if (await db.Transactions.AnyAsync(t => t.BookId == id && t.Status == "Borrowed"))
            return Conflict(new { message = "BOOK_CURRENTLY_BORROWED" });
        db.Books.Remove(book);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
