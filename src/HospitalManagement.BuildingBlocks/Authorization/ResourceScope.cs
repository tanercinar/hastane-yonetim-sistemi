namespace HospitalManagement.BuildingBlocks.Authorization;

public enum ResourceScope
{
    Own = 1,
    Assigned = 2,
    CareTeam = 3,
    Department = 4,
    Facility = 5,
    Organization = 6,
    Deidentified = 7,
    System = 8,
}

