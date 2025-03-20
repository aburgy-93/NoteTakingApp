using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Db;
using Backend.Model;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttributeController : ControllerBase
    {
        private readonly NoteDbContext _context;

        public AttributeController(NoteDbContext context)
        {
            _context = context;
        }

        // GET: api/Attribute
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NoteAttribute>>> GetAttributes()
        {
            return await _context.Attributes.ToListAsync();
        }

        // GET: api/Attribute/5
        [HttpGet("{id}")]
        public async Task<ActionResult<NoteAttribute>> GetNoteAttribute(int id)
        {
            var noteAttribute = await _context.Attributes.FindAsync(id);

            if (noteAttribute == null)
            {
                return NotFound();
            }

            return noteAttribute;
        }

        // PUT: api/Attribute/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutNoteAttribute(int id, NoteAttribute noteAttribute)
        {
            if (id != noteAttribute.AttributeId)
            {
                return BadRequest();
            }

            _context.Entry(noteAttribute).State = EntityState.Modified;

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

        // POST: api/Attribute
        [HttpPost]
        public async Task<ActionResult<NoteAttribute>> PostNoteAttribute(NoteAttribute noteAttribute)
        {
            _context.Attributes.Add(noteAttribute);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetNoteAttribute", new { id = noteAttribute.AttributeId }, noteAttribute);
        }

        // DELETE: api/Attribute/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNoteAttribute(int id)
        {
            var noteAttribute = await _context.Attributes.FindAsync(id);
            if (noteAttribute == null)
            {
                return NotFound();
            }

            _context.Attributes.Remove(noteAttribute);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool NoteAttributeExists(int id)
        {
            return _context.Attributes.Any(e => e.AttributeId == id);
        }
    }
}
