using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Model;

public class Project
{
    [Key]
    
    public int ProjectId { get; set; }

    [Required]
    [StringLength(255)]
    public required string Name { get; set; } = string.Empty;

    public List<Note> Notes { get; set; } = [];
}