using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


namespace Backend.Model;

public class Note {
    [Key]
    [JsonIgnore]
    public int NoteId {get; set;}

    [Required]
    public DateTime CreatedAt { get; set;} = DateTime.UtcNow;

    [Required]
    public string NoteText {get; set;} = string.Empty;

    // Foreign Key for Project (Nullable, since notes may not belong to a project)
    public int? ProjectId {get; set;}

    public List<NoteAttribute> Attributes { get; set; } = new();
}