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
    // Create new instance of ProjectController to handle requests.
    public class ProjectController : ControllerBase
    {
        private readonly NoteDbContext _context;

        // Getting the DbContext for the requests to use.
        public ProjectController(NoteDbContext context)
        {
            _context = context;
        }

        /*
            GET: api/Project
            Get all of the projects and their notes. 
        */
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Project>>> GetProjects()
        {
            var projects = await _context.Projects
                .Include(p => p.Notes)
                .ToListAsync();

            return projects;
        }

        /*
            Get all of the projects and count how many notes each project has
                associated with it. 
        */
        [HttpGet("ProjectNoteCounts")]
        public async Task<ActionResult<IEnumerable<ProjectNoteCountDto>>> GetProjectNoteCount()
        {
            // Group notes by ProjectId and count them
            var projectNoteCount = await _context.Notes
                /*
                    Group notes by their ProjectId. 
                    Count how many are in that group. 
                    Put the groups with their count into a list. 
                */
                .GroupBy(note => note.ProjectId)
                .Select(group => new 
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

        /*
            GET: api/Project/5
            Get a project from passed in id.
            Include all of the notes associated with that project. 
        */
        [HttpGet("{id}")]
        public async Task<ActionResult<Project>> GetProject(int id)
        {
             var project = await _context.Projects
                // Include the list of notes associated witha project. 
                .Include(project => project.Notes)  
                // Check each project in the Db to find the first one that 
                // matches the condition where project.ProjectId == id or return null. 
                .FirstOrDefaultAsync(project => project.ProjectId == id); 

            if (project == null)
            {
                return NotFound();
            }

            return project;
        }

        /*
            PUT: api/Project/5
            Allow user to update the project based on id passed in. 
        */
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProject(int id, ProjectUpdateDto projectDto)
        {
            // Find the project with id. 
            var project = await _context.Projects.FindAsync(id);

            if (project == null)
            {
                return BadRequest();
            }

            // Update the name of project with DTO
            project.Name = projectDto.Name;

            // Tell EF Core that there was something updated and save changes. 
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

        /*
            POST: api/Project
            Create a new project using the projectDto. 
        */
        [HttpPost]
        public async Task<ActionResult<ProjectCreateDto>> PostProject(ProjectCreateDto projectDto)
        {
            // Check that projectDto and projectName is not null
            if (projectDto == null || string.IsNullOrEmpty(projectDto.Name))
            {
                return BadRequest("Note text is required.");
            }

            // Create the new project using the DTO
            var project = new Project
            {
                Name = projectDto.Name,
            };

            // Add new project to the DbContext, update Db.
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProject", new { id = project.ProjectId }, project);
        }

        /*
            DELETE: api/Project/5
            Delete a project based on the id passed in. 
        */
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
