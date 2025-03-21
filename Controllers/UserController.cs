using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Db;
using Backend.Model;
using Backend.Services;
using Backend.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Create new instance of AttributeController to handle requests.
    public class UserController : ControllerBase
    {
        private readonly NoteDbContext _context;

        private readonly UserService _userService;

        private readonly TokenService _tokenService;

        // Getting the DbContext for the requests to use.
        public UserController(NoteDbContext context, UserService userService, 
            TokenService tokenService)
        {
            _context = context;
            _userService = userService;
            _tokenService = tokenService;
        }

        /*
            GET: api/User
            Get all registered users. 
            Not used in current app for anything but testing purposes, 
                but could be used for admin. 
        */
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        /*
            GET: api/User/5
            Get a user based on an id. 
            Not used in current app for anything but testing purposes, 
                but could be used for admin. 
        */
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        /*
            PUT: api/User/5
            Used to update an existing user. 
            Not used in current app for anything but testing purposes, 
                but could be used for admin. 
        */
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.UserId)
            {
                return BadRequest();
            }

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            // Hash the password if it's being updated
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                user.PasswordHash = _userService.HashPassword(user.PasswordHash);
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
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
            POST: api/User
            Used to register a new user in the Db.
        */
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterRequest request)
        {
            // Verify that the password entered is note empty. 
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Password is required.");
            }

            // Check to see if user already exists based on username. 
            var UserExists = await _context.Users.AnyAsync(user => user.Username == request.Username);
            
            if (UserExists)
            {
                return BadRequest("Username already taken.");
            }

            // Hash the password before saving
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create the user and set their attributes. 
            var user = new User
            {
                Username = request.Username,
                CreationTimestamp = DateTime.UtcNow,
                LastLoginTimestamp = DateTime.UtcNow,
                PasswordHash = hashedPassword
            };

            // Add user to the DbContext and save the changes. 
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.UserId }, user);
        }

        /*
            Log a user in after they have registered an account. 
        */
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            // First find the user who is logging in based on their username. 
            var user = await _context.Users.FirstOrDefaultAsync(user => user.Username == request.Username);

            // If the user is null or the password is not correct, do not log in. 
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid credentials.");
            }

            // Update when the user last logged in to current date and time. 
            user.LastLoginTimestamp = DateTime.UtcNow;

            // Save changes in Db
            await _context.SaveChangesAsync();

            // Generate a token to confirm that user is authorized to perform different requests. 
            var token = _tokenService.GenerateJwtToken(user);
            return Ok(new {Token = token});
        }

        /*
            DELETE: api/User/5
            Delete a user based on an id. 
            Not used in current app for anything but testing purposes, 
                but could be used for admin. 
        */
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
