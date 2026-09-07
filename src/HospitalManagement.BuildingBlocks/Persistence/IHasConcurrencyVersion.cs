namespace HospitalManagement.BuildingBlocks.Persistence;

/// <summary>
/// Marks an aggregate that uses an application-managed optimistic concurrency token.
/// </summary>
public interface IHasConcurrencyVersion
{
    long Version
    {
        get;
        set;
    }
}
