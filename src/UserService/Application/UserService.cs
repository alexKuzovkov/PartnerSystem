using Microsoft.EntityFrameworkCore;
using PartnerSystem.Contracts;
using UserService.Domain;
using UserService.Infrastructure;

namespace UserService.Application;

public class UserService : IUserService
{
    private readonly UserDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(UserDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserOperationResult> CreateUserAsync(string externalId, string? parentExternalId)
    {
        try
        {
            var existingUser = await _context.Users
                .AnyAsync(u => u.ExternalId == externalId);

            if (existingUser)
                return new UserOperationResult(false, "User already exists");

            var user = new User(externalId);

            if (!string.IsNullOrEmpty(parentExternalId))
            {
                var parent = await _context.Users
                    .FirstOrDefaultAsync(u => u.ExternalId == parentExternalId);

                if (parent != null)
                    user.SetParent(parent.Id);
                else
                    _logger.LogWarning("Партнер {PartnerId} не найден", parentExternalId);
            }

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Создан пользователь {UserId}", externalId);
            return new UserOperationResult(true, "User created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании пользователя {UserId}", externalId);
            return new UserOperationResult(false, $"Internal error: {ex.Message}");
        }
    }

    public async Task<UserOperationResult> SetPartnerLinkAsync(string userExternalId, string partnerExternalId)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ExternalId == userExternalId);

            if (user == null)
                return new UserOperationResult(false, "User not found");

            var partner = await _context.Users
                .FirstOrDefaultAsync(u => u.ExternalId == partnerExternalId);

            if (partner == null)
                return new UserOperationResult(false, "Partner not found");

            if (userExternalId == partnerExternalId)
                return new UserOperationResult(false, "Cannot link to self");

            var isDescendant = await IsDescendantAsync(user.Id, partner.Id);
            if (isDescendant)
                return new UserOperationResult(false, "Cannot create circular reference");

            user.SetParent(partner.Id);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Установлена связь: {UserId} -> {PartnerId}", userExternalId, partnerExternalId);
            return new UserOperationResult(true, "Partner link set successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при установке партнерской связи");
            return new UserOperationResult(false, $"Internal error: {ex.Message}");
        }
    }

    public async Task<List<PartnerChainItem>> GetPartnerChainAsync(string userExternalId, int maxLevels = 10)
    {
        var chain = new List<PartnerChainItem>();
        var currentExternalId = userExternalId;
        var actualMaxLevels = Math.Min(maxLevels, 10);

        for (int level = 0; level < actualMaxLevels; level++)
        {
            var user = await _context.Users
                .Include(u => u.Parent)
                .FirstOrDefaultAsync(u => u.ExternalId == currentExternalId);

            if (user == null || user.ParentId == null)
                break;

            var parent = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == user.ParentId);

            if (parent == null)
                break;

            chain.Add(new PartnerChainItem(parent.ExternalId, level + 1));
            currentExternalId = parent.ExternalId;
        }

        return chain;
    }

    public async Task<List<DownlineNode>> GetDownlineAsync(string userExternalId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.ExternalId == userExternalId);

        if (user == null) return new List<DownlineNode>();

        var result = new List<DownlineNode>();
        await BuildDownlineAsync(user.Id, 0, result);
        return result;
    }

    private async Task BuildDownlineAsync(Guid parentId, int level, List<DownlineNode> result)
    {
        if (level > 10) return;

        var children = await _context.Users
            .Where(u => u.ParentId == parentId)
            .ToListAsync();

        foreach (var child in children)
        {
            result.Add(new DownlineNode(
                child.ExternalId,
                level + 1,
                children.Count));

            await BuildDownlineAsync(child.Id, level + 1, result);
        }
    }

    private async Task<bool> IsDescendantAsync(Guid userId, Guid potentialDescendantId)
    {
        var currentId = potentialDescendantId;
        var maxDepth = 100;
        var depth = 0;

        while (depth < maxDepth)
        {
            if (currentId == userId)
                return true;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == currentId);

            if (user == null || user.ParentId == null)
                return false;

            currentId = user.ParentId.Value;
            depth++;
        }

        return false;
    }
}