// 1. Get a user by ID
// 2. Get a user by Entra object ID
// 3. Get all users
// 4. Add a user
// 5. Update a user

using Microsoft.EntityFrameworkCore;
using TeamIntelligenceHub.Application.Interfaces.Repositories;
using TeamIntelligenceHub.Domain.Entities;

namespace TeamIntelligenceHub.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IUserRepository, including lookup by Entra object ID.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly TeamIntelligenceHubDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    /// <param name="context">The database context used to read and save users.</param>
    public UserRepository(TeamIntelligenceHubDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEntraObjectIdAsync(
        string entraObjectId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(
                x => x.EntraObjectId == entraObjectId);
    }

    /// <inheritdoc />
    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users
            .OrderBy(x => x.DisplayName)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<User> AddAsync(User user)
    {
        await _context.Users.AddAsync(user);

        await _context.SaveChangesAsync();

        return user;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);

        await _context.SaveChangesAsync();
    }
}
