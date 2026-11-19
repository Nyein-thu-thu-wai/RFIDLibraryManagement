using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RfidLibraryApi.Data;

namespace RfidLibraryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController(AppDbContext db) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var today = DateTime.UtcNow.Date;
        return Ok(new {
            totalBooks = await db.Books.CountAsync(),
            availableBooks = await db.Books.CountAsync(b => b.IsAvailable),
            borrowedBooks = await db.Books.CountAsync(b => !b.IsAvailable),
            totalPatrons = await db.Patrons.CountAsync(),
            activePatrons = await db.Patrons.CountAsync(p => p.IsActive),
            registeredTags = await db.RfidTags.CountAsync(),
            borrowedToday = await db.Transactions.CountAsync(t => t.TransType == "Borrow" && t.BorrowDate >= today)
        });
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> Overdue()
    {
        var now = DateTime.UtcNow;
        var rows = await (from t in db.Transactions
                          join b in db.Books on t.BookId equals b.BookId
                          join p in db.Patrons on t.MemberId equals p.MemberId
                          where t.Status == "Borrowed" && t.DueDate < now
                          select new { t.TransId, t.MemberId, memberName = p.Name, t.BookId, bookTitle = b.Title, t.DueDate })
                         .ToListAsync();
        return Ok(rows);
    }
}
