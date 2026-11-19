using System.ComponentModel.DataAnnotations;

namespace RfidLibraryApi.Models;

public class Librarian
{
    [Key, MaxLength(20)] public string StaffId { get; set; } = Guid.NewGuid().ToString("N")[..20];
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(100)] public string Email { get; set; } = "";
    [MaxLength(50)] public string Role { get; set; } = "Librarian";
}
