namespace Backend.DTOs;

public class NoteUpdateDto
{
    public string NoteText {get; set;} = string.Empty;
    public List<int> AttributeIds { get; set; } = new();
}