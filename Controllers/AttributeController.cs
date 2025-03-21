using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Db;
using Backend.Model;
using Microsoft.AspNetCore.Authorization;
using Backend.DTOs;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    // Create new instance of AttributeController to handle requests.
    public class AttributeController : ControllerBase
    {
        private readonly NoteDbContext _context;

        // Getting the DbContext for the requests to use.
        public AttributeController(NoteDbContext context)
        {
            _context = context;
        }

        /*
            GET: api/Attribute
            Get all attributes and return them in a list.
        */
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NoteAttribute>>> 
            GetAttributes()
        {
            // Get all attributes in the DbContext, return as a list.
            return await _context.Attributes.ToListAsync();
        }

        /*
            GET: api/Attribute/5
            Get an attribute based on its Id
            TODO: Future implementation: a dropdown menu for users to choose 
                from and apply to their note(s)
        */
        [HttpGet("{id}")]
        public async Task<ActionResult<NoteAttribute>> GetNoteAttribute(int id)
        {
            // Get the attribute from the dbContext via attributeId
            var noteAttribute = await _context.Attributes.FindAsync(id);

            /*
                If no attribute is returned, returned not found error, 
                  else return attribute
            */
            if (noteAttribute == null)
            {
                return NotFound();
            }

            return noteAttribute;
        }

        /*
            PUT: api/Attribute/5
            Update an attribute name.
            TODO: Future implementation: maybe have a UI/table with all attributes
                    and allow users to update the attribute.
                Maybe give attributes a description section for users to explain
                    the purpose of that attribute.
        */
        [HttpPut("{id}")]
        public async Task<IActionResult> PutNoteAttribute(int id, 
            AttributeUpdateDto noteAttributeDto)
        {
            var attribute = await _context.Attributes.FindAsync(id);
            // Check to see id matches a note attribute.
            if (attribute == null)
            {
                // if none exists, return bad request
                return BadRequest();
            }

            attribute.AttributeName = noteAttributeDto.AttributeName;

            /*
                This is telling EF Core that an entity (noteAttribute) has been 
                    modified and should be updated in the database when 
                    SaveChanges() is called.
            */
            _context.Entry(attribute).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NoteAttributeExists(id))
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
            POST: api/Attribute
            Allow users to add an attribute. 
        */
        [HttpPost]
        public async Task<ActionResult<NoteAttribute>> PostNoteAttribute(NoteAttribute noteAttribute)
        {
            // Adding an atribute to the DbContext.
            _context.Attributes.Add(noteAttribute);

            // Save changes to database. 
            await _context.SaveChangesAsync();

            // Return the created object. 
            return CreatedAtAction("GetNoteAttribute", new { id = noteAttribute.AttributeId }, noteAttribute);
        }

        /*
            DELETE: api/Attribute/5
            Allow users to delete attributes. 
        */
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNoteAttribute(int id)
        {
            // Get the note from the DbContext that matches passed in id
            var noteAttribute = await _context.Attributes.FindAsync(id);

            // If note does note exist, return not found.
            if (noteAttribute == null)
            {
                return NotFound();
            }

            // Remove the note from the DbContext
            _context.Attributes.Remove(noteAttribute);

            // Save changes to Db.
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Method used to check if an attribute exists. 
        private bool NoteAttributeExists(int id)
        {
            return _context.Attributes.Any(e => e.AttributeId == id);
        }
    }
}
