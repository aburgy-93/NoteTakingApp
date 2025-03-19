using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

public class User
{
    [Key]
    public int UserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; }

    [JsonIgnore]
    public string Password { get; set; } = string.Empty;  // Initialize to an empty string

    [Required]
    public DateTime CreationTimestamp { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginTimestamp { get; set; }
}
