using System.ComponentModel.DataAnnotations;

namespace RfidLibraryApi.Models;

public class RfidTag
{
    [Key, MaxLength(50)] public string RfidNo { get; set; } = "";
    [MaxLength(20)] public string TagStatus { get; set; } = "Available";
    public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(20)] public string BookId { get; set; } = "";
    [MaxLength(20)] public string? RegisteredBy { get; set; }
    public Book? Book { get; set; }
}
