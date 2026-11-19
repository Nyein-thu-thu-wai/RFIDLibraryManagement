using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RfidLibraryApi.Data;
using RfidLibraryApi.Models;

namespace RfidLibraryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatronsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var q = db.Patrons.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search) || p.MemberId.Contains(search) || p.Email.Contains(search));
        return Ok(await q.OrderBy(p => p.Name).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(Patron patron)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        patron.MemberId = string.IsNullOrWhiteSpace(patron.MemberId) ? "MEM-" + Guid.NewGuid().ToString("N")[..8].ToUpper() : patron.MemberId;
        db.Patrons.Add(patron);
        await db.SaveChangesAsync();
        return Ok(patron);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, Patron input)
    {
        var p = await db.Patrons.FindAsync(id);
        if (p is null) return NotFound();
        p.Name = input.Name; p.Phone = input.Phone; p.Email = input.Email;
        p.IsActive = input.IsActive; p.ExpiryDate = input.ExpiryDate;
        await db.SaveChangesAsync();
        return Ok(p);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var p = await db.Patrons.FindAsync(id);
        if (p is null) return NotFound();
        if (await db.Transactions.AnyAsync(t => t.MemberId == id && t.Status == "Borrowed"))
            return Conflict(new { message = "MEMBER_HAS_UNRETURNED_BOOKS" });
        db.Patrons.Remove(p);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/validate")]
    public async Task<IActionResult> Validate(string id)
    {
        var p = await db.Patrons.FindAsync(id);
        var valid = p is not null && p.IsActive && (p.ExpiryDate is null || p.ExpiryDate.Value.Date >= DateTime.UtcNow.Date);
        return Ok(new { memberId = id, valid, message = valid ? "Membership is valid." : "Membership is invalid or expired." });
    }
}
