using Backend.Db;
using Backend.Model;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Backend.Services;

public class UserService
{
    private readonly NoteDbContext _context;

    // Get the Db and put it into the class _context. 
    public UserService(NoteDbContext context)
    {
        _context = context;
    }

    // Check to make sure the username doesn't already exist. 
    public async Task<bool> IsUsernameUnique(string username)
    {
        return !await _context.Users.AnyAsync(u => u.Username == username);
    }

    // Hash the entered password.
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    // Check that entered password and hashed password match. 
    public bool VerifyPassword(string enteredPassword, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(enteredPassword, hashedPassword);
    }
}
