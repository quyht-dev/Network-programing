using BlogSystem.Data;
using BlogSystem.Models.Entities;

namespace BlogSystem.Services;

public class AuthService
{
    private readonly ApplicationDbContext _db;

    public AuthService(ApplicationDbContext db)
    {
        _db = db;
    }

    public User? Login(string email, string password)
    {
        var user = _db.Users.FirstOrDefault(u => u.Email == email);

        if (user == null)
            return null;

        if (user.PasswordHash != password)
            return null;

        return user;
    }
}
