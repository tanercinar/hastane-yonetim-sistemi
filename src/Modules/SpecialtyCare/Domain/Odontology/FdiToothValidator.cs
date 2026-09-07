namespace HospitalManagement.Modules.SpecialtyCare.Domain.Odontology;

public static class FdiToothValidator
{
    private static readonly HashSet<int> ValidAdultTeeth =
    [
        11, 12, 13, 14, 15, 16, 17, 18,
        21, 22, 23, 24, 25, 26, 27, 28,
        31, 32, 33, 34, 35, 36, 37, 38,
        41, 42, 43, 44, 45, 46, 47, 48
    ];

    private static readonly HashSet<int> ValidPrimaryTeeth =
    [
        51, 52, 53, 54, 55,
        61, 62, 63, 64, 65,
        71, 72, 73, 74, 75,
        81, 82, 83, 84, 85
    ];

    public static bool IsValidToothNumber(int toothNumber) =>
        ValidAdultTeeth.Contains(toothNumber) || ValidPrimaryTeeth.Contains(toothNumber);

    public static bool IsAdultTooth(int toothNumber) =>
        ValidAdultTeeth.Contains(toothNumber);

    public static bool IsPrimaryTooth(int toothNumber) =>
        ValidPrimaryTeeth.Contains(toothNumber);

    public static IReadOnlyList<int> GetAllAdultTeeth() => ValidAdultTeeth.OrderBy(x => x).ToList();
}
