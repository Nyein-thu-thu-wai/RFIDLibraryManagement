using System.ComponentModel.DataAnnotations;

namespace RfidLibraryApi.Models;

public class RfidReader
{
    [Key, MaxLength(20)] public string ReaderId { get; set; } = "";
    [MaxLength(100)] public string Location { get; set; } = "";
    [MaxLength(20)] public string DeviceStatus { get; set; } = "Online";
}
