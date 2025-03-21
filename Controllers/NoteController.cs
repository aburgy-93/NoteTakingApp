using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Db;
using Backend.Model;
using Backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Build.Logging;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    // Create new instance of NoteController to handle requests.
    public class NoteController : ControllerBase
    {
        private readonly NoteDbContext _context;

        // Getting the DbContext for the requests to use.
        public NoteController(NoteDbContext context)
        {
            _context = context;
        }

        /*
            GET: api/Note
            Allows the user to filter notes based on projectId or attribute type.
        */
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Note>>> GetNotes(
        [FromQuery] int? projectId = null,  // Optional project filter
        [FromQuery] int[]? attributeIds = null // Optional attribute filter
        )
        {
            /* 
                Get the current user's UserId from the claims in the JWT token.
                if a userId is returned from the JTW claim, then user is logged 
                    in and can get the requested data.
                Else, user is not authorized (not logged in/not registered)
            */ 
            var userIdClaim = User.Claims.FirstOrDefault(claim => claim.Type == "userId");

            if (userIdClaim == null)
            {
                return Unauthorized("User not found in token.");
            }

            // Start with the query to get notes
            var query = _context.Notes
                // Include attributes for filtering
                .Include(note => note.Attributes) 
                .AsQueryable();

            // Apply projectId filter if specified
            if (projectId.HasValue)
            {
                // Further filter attributes if there is a projectId
                query = query.Where(n => n.ProjectId == projectId);
            }

            // Apply attributeId filter if specified
            if (attributeIds != null && attributeIds.Any())
            {
                query = query.Where(note => note.Attributes
                    .Any(attr => attributeIds.Contains(attr.AttributeId)));
            }

            // Execute the query, return notes as a list of notes. 
            var notes = await query.ToListAsync();
            return Ok(notes);
        }

        // Getting the count of notes with or without an attribute(s)
       [HttpGet("AttributeNoteCounts")]
        public async Task<ActionResult<IEnumerable<AttributeNoteCountDTO>>> 
            GetAttributeCounts()
            {
                // Flatten the relationship between Notes and Attributes
                var attributeNoteCounts = await _context.Notes
                /*
                    SelectMany, method used to flatten out many-to-many relationship.
                    For each note we're iterating over its attributes and 
                        creating a new object for each combo of Note and attribute.
                    This collection of Notes and Attributes are then flattened 
                        to a one-dimensional collection of AttributeId-Noteid pairs.
                */
                    .SelectMany(note => note.Attributes
                    .Select(attr => new { attr.AttributeId, note.NoteId }))
                    /*
                        Grouping the results by AttributeId.
                        Groups all the pairs where the AttributeId is the same, 
                            so we can count how many notes have that particular 
                            attribute. 
                    */
                    .GroupBy(results => results.AttributeId)
                    /*
                        For each group, select the AttributeId (group.Key).
                        For each group, count the notes that have this attr. 
                        Then, convert the result to a list. 
                    */
                    .Select(group => new 
                    {
                        AttributeId = group.Key,
                        Count = group.Count()
                    })
                    .ToListAsync();

                // Get the count of notes without any attributes
                var notesWithoutAttributesCount = await _context.Notes
                    .Where(note => !note.Attributes.Any())
                    .CountAsync();

                // Convert the results to DTO format
                var results = attributeNoteCounts
                    .Select(attr => new AttributeNoteCountDTO
                    {
                        AttributeIds = new int[] { attr.AttributeId },
                        Count = attr.Count
                    })
                    .ToList();

                // Generate the results using the DTO
                results.Add(new AttributeNoteCountDTO
                {
                    AttributeIds = new int[] { },
                    Count = notesWithoutAttributesCount
                });

                return Ok(results);
            }

        /*
            GET: api/Note/5
            Get a note by a passed in id. 
        */
        [HttpGet("{id}")]
        public async Task<ActionResult<Note>> GetNote(int id) {
            // Get the note specified by the id and include the attributes. 
            var note = await _context.Notes
                .Include(note => note.Attributes)
                .FirstOrDefaultAsync(note => note.NoteId == id);

            // If no note found, return not found.
            if (note == null) {
                return NotFound();
            }

            // Return note matching the id.
            return note;
        }

        /*
            PUT: api/Note/5
            Update a note matching the passed in id. 
        */
        [HttpPut("{id}")]
        public async Task<IActionResult> PutNote(int id, NoteUpdateDto noteUpdateDto)
        {
            // Find the note by the provided id
            var note = await _context.Notes
                .Include(note => note.Attributes)
                .FirstOrDefaultAsync(note => note.NoteId == id);

            if (note == null)
            {
                // Return 404 if note is not found
                return NotFound();  
            }

            var attributes = noteUpdateDto.AttributeIds.Any()
                ? await _context.Attributes
                    .Where(a => noteUpdateDto.AttributeIds.Contains(a.AttributeId))
                    .ToListAsync()
                : new List<NoteAttribute>(); // If empty, clear attributes

            // Update the note properties with the data from the DTO
            note.NoteText = noteUpdateDto.NoteText;

            // Assign new attributes
            note.Attributes = attributes;

            // Mark the entity as modified and save changes to the database
            _context.Entry(note).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NoteExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        /*
            POST: api/Note
            Create a new note.
        */
        [HttpPost]
        public async Task<ActionResult<Note>> PostNote(
            [FromBody] NoteCreateDto noteDto, 
            [FromQuery] int? projectId = null) 
        {
            // Check to make sure noteDto is not null and NoteText is not null. 
            if (noteDto == null || string.IsNullOrEmpty(noteDto.NoteText))
            {
                return BadRequest("Note text is required.");
            }

            // Check to see if any attributes were added to the note.
            // If any were added, then return the list of attributes or empty list. 
            var attributes = noteDto.AttributeIds.Any()
                ? await _context.Attributes
                    .Where(a => noteDto.AttributeIds.Contains(a.AttributeId))
                    .ToListAsync()
                : new List<NoteAttribute>(); 

            // Use NoteDto to map note data passed in by the user. 
            var note = new Note
            {
                NoteText = noteDto.NoteText,
                // Use the projectId from query parameter if provided, otherwise it will be null
                ProjectId = projectId,
                Attributes = attributes
            };

            // Add the note to the context and save changes to Db. 
            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            // Return the new created note. 
            return CreatedAtAction("GetNote", new { id = note.NoteId }, note);
        }


        /*
            DELETE: api/Note/5
            Allow a user to delete a note (if logged in) with note id. 
        */
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            // Find note based on id if it exists. 
            var note = await _context.Notes.FindAsync(id);
            if (note == null)
            {
                return NotFound();
            }

            // Remove note from DbContext and save changes. 
            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool NoteExists(int id)
        {
            return _context.Notes.Any(e => e.NoteId == id);
        }
    }
}
