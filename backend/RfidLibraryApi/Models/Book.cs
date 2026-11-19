using System.ComponentModel.DataAnnotations;

namespace RfidLibraryApi.Models;

public class Book
{
    [Key, MaxLength(20)] public string BookId { get; set; } = Guid.NewGuid().ToString("N")[..20];
    [Required, MaxLength(50)] public string CallNo { get; set; } = "";
    [Required, MaxLength(255)] public string Title { get; set; } = "";
    [MaxLength(150)] public string Author { get; set; } = "";
    public string? MarcData { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime AcquiredDate { get; set; } = DateTime.UtcNow;
    [MaxLength(20)] public string? ManagedBy { get; set; }
    public ICollection<RfidTag> RfidTags { get; set; } = new List<RfidTag>();
}
