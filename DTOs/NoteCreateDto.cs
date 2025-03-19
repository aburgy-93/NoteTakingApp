namespace Backend.DTOs;

public class NoteCreateDto
{
    public string NoteText {get; set;} = string.Empty;
    public int? ProjectId {get; set;} = null;
}