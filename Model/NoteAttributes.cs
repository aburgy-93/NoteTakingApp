using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Backend.Model;

public class NoteAttribute
{
    [Key]
    public int AttributeId {get; set;}

    [Required]
    public required string AttributeName {get; set;}


}