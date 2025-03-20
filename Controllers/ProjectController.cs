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

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly NoteDbContext _context;

        public ProjectController(NoteDbContext context)
        {
            _context = context;
        }

        // GET: api/Project
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            var projects = await _context.Projects
                .Include(p => p.Notes)
                .ToListAsync();

            return projects;
        }

        [HttpGet("ProjectNoteCounts")]
        public async Task<ActionResult<IEnumerable<ProjectNoteCountDto>>> GetProjectNoteCount()
        {
            // Group notes by ProjectId and count them
            var projectNoteCount = await _context.Notes.GroupBy(note => note.ProjectId).Select(group => new 
            {
                ProjectId = group.Key,
                Count = group.Count()
            })
            .ToListAsync();

            // Convert to DTO format
            var result = projectNoteCount.Select(projDto => new ProjectNoteCountDto
            {
                ProjectId = projDto.ProjectId,
                Count = projDto.Count
            }).ToList();

            return Ok(result);
        }

        // GET: api/Project/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetProject(int id)
        {
             var project = await _context.Projects
                .Include(p => p.Notes)  // Eager load the Notes collection
                .FirstOrDefaultAsync(p => p.ProjectId == id);  // Get the project by ID

            if (project == null)
            {
                return NotFound();
            }

            return project;
        }

        // PUT: api/Project/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProject(int id, ProjectUpdateDto projectDto)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project == null)
            {
                return BadRequest();
            }

            project.Name = projectDto.Name;

            _context.Entry(project).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProjectExists(id))
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

        // POST: api/Project
        [HttpPost]
        public async Task<ActionResult<ProjectCreateDto>> PostProject(ProjectCreateDto projectDto)
        {
            if (projectDto == null || string.IsNullOrEmpty(projectDto.Name))
            {
                return BadRequest("Note text is required.");
            }

            var project = new Project
            {
                Name = projectDto.Name,
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProject", new { id = project.ProjectId }, project);
        }

        // DELETE: api/Project/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.ProjectId == id);
        }
    }
}
