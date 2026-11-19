using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RfidLibraryApi.Data;
using RfidLibraryApi.Models;

namespace RfidLibraryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CirculationController(AppDbContext db) : ControllerBase
{
    public record BorrowRequest(string MemberId, string RfidNo, string? StaffId, string? ReaderId, int LoanDays = 14);
    public record ReturnRequest(string RfidNo, string? StaffId, string? ReaderId);

    [HttpPost("borrow")]
    public async Task<IActionResult> Borrow(BorrowRequest request)
    {
        var patron = await db.Patrons.FindAsync(request.MemberId);
        if (patron is null || !patron.IsActive ||
            (patron.ExpiryDate.HasValue && patron.ExpiryDate.Value.Date < DateTime.UtcNow.Date))
            return BadRequest(new { message = "MEMBERSHIP_INVALID" });

        var tag = await db.RfidTags.Include(t => t.Book).FirstOrDefaultAsync(t => t.RfidNo == request.RfidNo);
        if (tag?.Book is null) return NotFound(new { message = "RFID_OR_BOOK_NOT_FOUND" });
        if (!tag.Book.IsAvailable || tag.TagStatus == "Borrowed")
            return Conflict(new { message = "BOOK_NOT_AVAILABLE" });

        tag.Book.IsAvailable = false;
        tag.TagStatus = "Borrowed";
        var trx = new LibraryTransaction
        {
            TransType = "Borrow",
            BorrowDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(Math.Clamp(request.LoanDays, 1, 60)),
            Status = "Borrowed",
            MemberId = request.MemberId,
            BookId = tag.BookId,
            StaffId = request.StaffId,
            ReaderId = request.ReaderId
        };
        db.Transactions.Add(trx);
        await db.SaveChangesAsync();
        return Ok(new { message = "Book borrowed successfully.", transaction = trx, book = tag.Book });
    }

    [HttpPost("return")]
    public async Task<IActionResult> Return(ReturnRequest request)
    {
        var tag = await db.RfidTags.Include(t => t.Book).FirstOrDefaultAsync(t => t.RfidNo == request.RfidNo);
        if (tag?.Book is null) return NotFound(new { message = "RFID_OR_BOOK_NOT_FOUND" });
        var trx = await db.Transactions
            .Where(t => t.BookId == tag.BookId && t.Status == "Borrowed")
            .OrderByDescending(t => t.BorrowDate)
            .FirstOrDefaultAsync();
        if (trx is null) return BadRequest(new { message = "NO_ACTIVE_BORROW_TRANSACTION" });

        trx.ReturnDate = DateTime.UtcNow;
        trx.Status = "Returned";
        trx.TransType = "Return";
        trx.StaffId = request.StaffId ?? trx.StaffId;
        trx.ReaderId = request.ReaderId ?? trx.ReaderId;
        tag.Book.IsAvailable = true;
        tag.TagStatus = "Available";
        await db.SaveChangesAsync();
        return Ok(new { message = "Book returned successfully.", transaction = trx });
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions() =>
        Ok(await (from t in db.Transactions
                  join b in db.Books on t.BookId equals b.BookId
                  join p in db.Patrons on t.MemberId equals p.MemberId
                  orderby t.BorrowDate descending
                  select new {
                      t.TransId, t.TransType, t.BorrowDate, t.DueDate, t.ReturnDate, t.Status,
                      t.MemberId, memberName = p.Name, t.BookId, bookTitle = b.Title, t.StaffId, t.ReaderId
                  }).ToListAsync());
}
