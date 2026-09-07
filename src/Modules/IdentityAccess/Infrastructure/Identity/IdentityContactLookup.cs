using HospitalManagement.Modules.IdentityAccess.Application;
using HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.IdentityAccess.Infrastructure.Identity;

public sealed class IdentityContactLookup(IdentityAccessDbContext dbContext) : IIdentityContactLookup
{
    private readonly IdentityAccessDbContext _dbContext =
        dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public Task<IdentityContactDto?> FindByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default) =>
        _dbContext.Users
            .AsNoTracking()
            .Where(user => user.PersonId == personId && user.IsEnabled && user.Email != null)
            .Select(user => new IdentityContactDto(user.PersonId, user.Email!))
            .SingleOrDefaultAsync(cancellationToken);
}
