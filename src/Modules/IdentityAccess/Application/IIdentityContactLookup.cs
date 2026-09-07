namespace HospitalManagement.Modules.IdentityAccess.Application;

public interface IIdentityContactLookup
{
    Task<IdentityContactDto?> FindByPersonIdAsync(
        Guid personId,
        CancellationToken cancellationToken = default);
}

public sealed record IdentityContactDto(Guid PersonId, string Email);
