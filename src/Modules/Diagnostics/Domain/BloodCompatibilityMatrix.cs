namespace HospitalManagement.Modules.Diagnostics.Domain;

public static class BloodCompatibilityMatrix
{
    public static bool IsCompatible(BloodGroup donorGroup, BloodGroup recipientGroup, BloodProductType productType)
    {
        return productType switch
        {
            BloodProductType.FreshFrozenPlasma => IsPlasmaCompatible(donorGroup, recipientGroup),
            _ => IsRedCellCompatible(donorGroup, recipientGroup),
        };
    }

    public static bool IsRedCellCompatible(BloodGroup donor, BloodGroup recipient)
    {
        // O- is universal red cell donor
        if (donor == BloodGroup.ONegative)
            return true;

        // AB+ is universal red cell recipient
        if (recipient == BloodGroup.ABPositive)
            return true;

        // Exact match
        if (donor == recipient)
            return true;

        return (donor, recipient) switch
        {
            (BloodGroup.OPositive, BloodGroup.APositive or BloodGroup.BPositive or BloodGroup.ABPositive) => true,
            (BloodGroup.ANegative, BloodGroup.APositive or BloodGroup.ABNegative or BloodGroup.ABPositive) => true,
            (BloodGroup.APositive, BloodGroup.ABPositive) => true,
            (BloodGroup.BNegative, BloodGroup.BPositive or BloodGroup.ABNegative or BloodGroup.ABPositive) => true,
            (BloodGroup.BPositive, BloodGroup.ABPositive) => true,
            (BloodGroup.ABNegative, BloodGroup.ABPositive) => true,
            _ => false,
        };
    }

    public static bool IsPlasmaCompatible(BloodGroup donor, BloodGroup recipient)
    {
        // AB is universal plasma donor
        if (donor == BloodGroup.ABPositive || donor == BloodGroup.ABNegative)
            return true;

        if (donor == recipient)
            return true;

        return (donor, recipient) switch
        {
            (BloodGroup.APositive or BloodGroup.ANegative, BloodGroup.OPositive or BloodGroup.ONegative) => true,
            (BloodGroup.BPositive or BloodGroup.BNegative, BloodGroup.OPositive or BloodGroup.ONegative) => true,
            _ => false,
        };
    }
}
