using Microsoft.EntityFrameworkCore;
using PartnerSystem.Contracts;
using UserService.Domain;
using UserService.Infrastructure;

namespace UserService.Application;

public sealed class UserService : IUserService
{
    private const int MaximumHierarchyDepth = 10;

    private readonly UserDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(UserDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserOperationResult> CreateUserAsync(
        string externalId,
        string? parentExternalId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            return new UserOperationResult(false, "ExternalId is required");

        try
        {
            if (await _context.Users.AnyAsync(u => u.ExternalId == externalId, cancellationToken))
                return new UserOperationResult(false, "User already exists");

            var user = new User(externalId);

            if (!string.IsNullOrWhiteSpace(parentExternalId))
            {
                var parent = await _context.Users
                    .SingleOrDefaultAsync(
                        u => u.ExternalId == parentExternalId,
                        cancellationToken);

                if (parent is null)
                {
                    _logger.LogWarning(
                        "Parent user {ParentId} was not found while creating {UserId}",
                        parentExternalId,
                        externalId);
                    return new UserOperationResult(false, "Parent user not found");
                }

                user.SetParent(parent.Id);
            }

            await _context.Users.AddAsync(user, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created user {UserId}", externalId);
            return new UserOperationResult(true, "User created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user {UserId}", externalId);
            return new UserOperationResult(false, "Internal error");
        }
    }

    public async Task<UserOperationResult> SetPartnerLinkAsync(
        string userExternalId,
        string partnerExternalId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (userExternalId == partnerExternalId)
                return new UserOperationResult(false, "Cannot link a user to itself");

            var user = await _context.Users
                .SingleOrDefaultAsync(
                    u => u.ExternalId == userExternalId,
                    cancellationToken);

            if (user is null)
                return new UserOperationResult(false, "User not found");

            var partner = await _context.Users
                .SingleOrDefaultAsync(
                    u => u.ExternalId == partnerExternalId,
                    cancellationToken);

            if (partner is null)
                return new UserOperationResult(false, "Partner not found");

            if (await WouldCreateCycleAsync(user.Id, partner.Id, cancellationToken))
                return new UserOperationResult(false, "Cannot create a circular partner relationship");

            user.SetParent(partner.Id);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Updated partner relationship: {UserId} -> {PartnerId}",
                userExternalId,
                partnerExternalId);

            return new UserOperationResult(true, "Partner link set successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update partner relationship for user {UserId}",
                userExternalId);
            return new UserOperationResult(false, "Internal error");
        }
    }

    public async Task<List<PartnerChainItem>> GetPartnerChainAsync(
        string userExternalId,
        int maxLevels = MaximumHierarchyDepth,
        CancellationToken cancellationToken = default)
    {
        var chain = new List<PartnerChainItem>();
        var currentExternalId = userExternalId;
        var actualMaxLevels = Math.Clamp(maxLevels, 0, MaximumHierarchyDepth);

        for (var level = 1; level <= actualMaxLevels; level++)
        {
            var currentUser = await _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    u => u.ExternalId == currentExternalId,
                    cancellationToken);

            if (currentUser?.ParentId is null)
                break;

            var parent = await _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    u => u.Id == currentUser.ParentId.Value,
                    cancellationToken);

            if (parent is null)
                break;

            chain.Add(new PartnerChainItem(parent.ExternalId, level));
            currentExternalId = parent.ExternalId;
        }

        return chain;
    }

    public async Task<List<DownlineNode>> GetDownlineAsync(
        string userExternalId,
        CancellationToken cancellationToken = default)
    {
        // Load the hierarchy once to avoid an N+1 query for every node in the downline tree.
        var users = await _context.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var root = users.SingleOrDefault(u => u.ExternalId == userExternalId);
        if (root is null)
            return [];

        var childrenByParent = users
            .Where(u => u.ParentId.HasValue)
            .GroupBy(u => u.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<DownlineNode>();
        var queue = new Queue<(User User, int Level)>();

        if (childrenByParent.TryGetValue(root.Id, out var rootChildren))
        {
            foreach (var child in rootChildren)
                queue.Enqueue((child, 1));
        }

        while (queue.Count > 0)
        {
            var (user, level) = queue.Dequeue();
            if (level > MaximumHierarchyDepth)
                continue;

            childrenByParent.TryGetValue(user.Id, out var children);
            var directReferralsCount = children?.Count ?? 0;

            result.Add(new DownlineNode(
                user.ExternalId,
                level,
                directReferralsCount));

            if (children is null || level == MaximumHierarchyDepth)
                continue;

            foreach (var child in children)
                queue.Enqueue((child, level + 1));
        }

        return result;
    }

    private async Task<bool> WouldCreateCycleAsync(
        Guid userId,
        Guid proposedParentId,
        CancellationToken cancellationToken)
    {
        var currentId = proposedParentId;

        for (var depth = 0; depth < 100; depth++)
        {
            if (currentId == userId)
                return true;

            var current = await _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == currentId, cancellationToken);

            if (current?.ParentId is null)
                return false;

            currentId = current.ParentId.Value;
        }

        // Treat an unexpectedly deep hierarchy as unsafe instead of risking a cycle.
        return true;
    }
}
