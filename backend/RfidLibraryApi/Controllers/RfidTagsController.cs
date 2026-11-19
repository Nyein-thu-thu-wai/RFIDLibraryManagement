using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RfidLibraryApi.Data;
using RfidLibraryApi.Models;

namespace RfidLibraryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RfidTagsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await db.RfidTags.Include(t => t.Book).OrderBy(t => t.RfidNo).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Register(RfidTag tag)
    {
        if (!await db.Books.AnyAsync(b => b.BookId == tag.BookId))
            return BadRequest(new { message = "BOOK_NOT_FOUND" });
        if (await db.RfidTags.AnyAsync(t => t.RfidNo == tag.RfidNo))
            return Conflict(new { message = "RFID_ALREADY_REGISTERED" });
        tag.RegisteredDate = DateTime.UtcNow;
        tag.TagStatus = "Available";
        db.RfidTags.Add(tag);
        await db.SaveChangesAsync();
        return Ok(await db.RfidTags.Include(t => t.Book).FirstAsync(t => t.RfidNo == tag.RfidNo));
    }

    [HttpDelete("{rfidNo}")]
    public async Task<IActionResult> Delete(string rfidNo)
    {
        var tag = await db.RfidTags.FindAsync(rfidNo);
        if (tag is null) return NotFound();
        db.RfidTags.Remove(tag);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{rfidNo}/scan")]
    public async Task<IActionResult> Scan(string rfidNo)
    {
        var tag = await db.RfidTags.Include(t => t.Book).FirstOrDefaultAsync(t => t.RfidNo == rfidNo);
        return tag is null ? NotFound(new { message = "RFID_NOT_FOUND" }) : Ok(tag);
    }
}
