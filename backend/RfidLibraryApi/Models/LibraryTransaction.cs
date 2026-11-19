using System.ComponentModel.DataAnnotations;

namespace RfidLibraryApi.Models;

public class LibraryTransaction
{
    [Key, MaxLength(30)] public string TransId { get; set; } = "TRX-" + Guid.NewGuid().ToString("N")[..12].ToUpper();
    [Required, MaxLength(20)] public string TransType { get; set; } = "Borrow";
    public DateTime BorrowDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(14);
    public DateTime? ReturnDate { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Borrowed";
    [Required, MaxLength(20)] public string MemberId { get; set; } = "";
    [Required, MaxLength(20)] public string BookId { get; set; } = "";
    [MaxLength(20)] public string? StaffId { get; set; }
    [MaxLength(20)] public string? ReaderId { get; set; }
}
