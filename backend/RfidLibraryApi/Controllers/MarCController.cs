using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RfidLibraryApi.Data;

namespace RfidLibraryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarcController(AppDbContext db) : ControllerBase
{
    // Simple MARC text export/import starter. This is not a complete MARC21 parser.
    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var books = await db.Books.OrderBy(b => b.CallNo).ToListAsync();
        var lines = books.Select(b => string.IsNullOrWhiteSpace(b.MarcData)
            ? $"001  {b.BookId}\n090  ##$a{b.CallNo}\n245  10$a{b.Title}\n100  1#$a{b.Author}"
            : b.MarcData);
        return File(System.Text.Encoding.UTF8.GetBytes(string.Join("\n\n", lines)),
            "text/plain", "library-marc-export.txt");
    }

    [HttpPost("import")]
    [RequestSizeLimit(2_000_000)]
    public async Task<IActionResult> Import()
    {
        if (!Request.HasFormContentType || Request.Form.Files.Count == 0)
            return BadRequest(new { message = "UPLOAD_A_TEXT_FILE" });

        using var reader = new StreamReader(Request.Form.Files[0].OpenReadStream());
        var content = await reader.ReadToEndAsync();
        var records = content.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var imported = 0;
        foreach (var record in records)
        {
            string Get(string tag) {
                var line = record.Split('\n').FirstOrDefault(x => x.TrimStart().StartsWith(tag));
                return line is null ? "" : line[(line.IndexOf('$') + 2)..].Trim();
            }
            var title = Get("245");
            if (string.IsNullOrWhiteSpace(title)) continue;
            var callNo = Get("090");
            if (string.IsNullOrWhiteSpace(callNo)) callNo = "IMPORTED-" + Guid.NewGuid().ToString("N")[..8];
            if (await db.Books.AnyAsync(b => b.CallNo == callNo)) continue;
            db.Books.Add(new Models.Book { CallNo = callNo, Title = title, Author = Get("100"), MarcData = record });
            imported++;
        }
        await db.SaveChangesAsync();
        return Ok(new { imported });
    }
}
