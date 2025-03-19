using Backend.Db;
using Backend.Model;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Backend.Services;

public class UserService
{
    private readonly NoteDbContext _context;

    public UserService(NoteDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsUsernameUnique(string username)
    {
        return !await _context.Users.AnyAsync(u => u.Username == username);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyPassword(string enteredPassword, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(enteredPassword, hashedPassword);
    }
}
