using System.Security.Cryptography;

namespace HospitalManagement.Modules.Patients.Application;

public static class MedicalRecordNumberGenerator
{
    public static string Generate(int year, int sequence)
    {
        return $"MRN-{year}-{sequence:D6}";
    }

    public static string GenerateRandom(int year)
    {
        var randomNum = RandomNumberGenerator.GetInt32(100000, 999999);
        return $"MRN-{year}-{randomNum:D6}";
    }
}
