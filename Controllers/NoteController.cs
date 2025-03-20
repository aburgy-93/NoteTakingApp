using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NoteController : ControllerBase
    {
        private readonly NoteDbContext _context;

        public NoteController(NoteDbContext context)
        {
            _context = context;
        }

        // GET: api/Note
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Note>>> GetNotes(
        [FromQuery] int? projectId = null,  // Optional project filter
        [FromQuery] int[] attributeIds = null // Optional attribute filter
        )
        {
            // Get the current user's UserId from the claims in the JWT token
            var userIdClaim = User.Claims.FirstOrDefault(claim => claim.Type == "userId");

            if (userIdClaim == null)
            {
                return Unauthorized("User not found in token.");
            }

            // Start with the query to get notes
            var query = _context.Notes
                .Include(note => note.Attributes) // Include attributes for filtering
                .AsQueryable();

            // Apply projectId filter if specified
            if (projectId.HasValue)
            {
                query = query.Where(n => n.ProjectId == projectId);
            }

            // Apply attributeId filter if specified
            if (attributeIds != null && attributeIds.Any())
            {
                query = query.Where(n => n.Attributes.Any(a => attributeIds.Contains(a.AttributeId)));
            }

            // Execute the query
            var notes = await query.ToListAsync();
            return Ok(notes);
        }

        // GET: api/Note/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Note>> GetNote(int id) {
            var note = await _context.Notes
                .Include(n => n.Attributes)
                .FirstOrDefaultAsync(n => n.NoteId == id);


            if (note == null) {
                return NotFound();
            }

            return note;
        }

        // PUT: api/Note/5
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


        // POST: api/Note
        [HttpPost]
        public async Task<ActionResult<Note>> PostNote(
            [FromBody] NoteCreateDto noteDto, 
            [FromQuery] int? projectId = null)  // Optional parameter
        {
            if (noteDto == null || string.IsNullOrEmpty(noteDto.NoteText))
            {
                return BadRequest("Note text is required.");
            }

            var attributes = noteDto.AttributeIds.Any()
                ? await _context.Attributes
                    .Where(a => noteDto.AttributeIds.Contains(a.AttributeId))
                    .ToListAsync()
                : new List<NoteAttribute>(); // If empty, return an empty list

            var note = new Note
            {
                NoteText = noteDto.NoteText,
                // Use the projectId from query parameter if provided, otherwise it will be null
                ProjectId = projectId,
                Attributes = attributes
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetNote", new { id = note.NoteId }, note);
        }


        // DELETE: api/Note/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            var note = await _context.Notes.FindAsync(id);
            if (note == null)
            {
                return NotFound();
            }

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
