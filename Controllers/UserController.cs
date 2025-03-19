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

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly NoteDbContext _context;

        private readonly UserService _userService;

        private readonly TokenService _tokenService;

        public UserController(NoteDbContext context, UserService userService, 
            TokenService tokenService)
        {
            _context = context;
            _userService = userService;
            _tokenService = tokenService;
        }

        // GET: api/User
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // GET: api/User/5
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

        // PUT: api/User/5
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

        // POST: api/User
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Password is required.");
            }

            var UserExists = await _context.Users.AnyAsync(user => user.Username == request.Username);
            
            if (UserExists)
            {
                return BadRequest("Username already taken.");
            }

            // Hash the password before saving
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                CreationTimestamp = DateTime.UtcNow,
                LastLoginTimestamp = DateTime.UtcNow,
                PasswordHash = hashedPassword
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.UserId }, user);
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(user => user.Username == request.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid credentials.");
            }

            user.LastLoginTimestamp = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = _tokenService.GenerateJwtToken(user);
            return Ok(new {Token = token});
        }

        // DELETE: api/User/5
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
