using System.ComponentModel.DataAnnotations;

namespace RfidLibraryApi.Models;

public class Patron
{
    [Key, MaxLength(20)] public string MemberId { get; set; } = Guid.NewGuid().ToString("N")[..20];
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(20)] public string Phone { get; set; } = "";
    [MaxLength(100)] public string Email { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiryDate { get; set; }
}
