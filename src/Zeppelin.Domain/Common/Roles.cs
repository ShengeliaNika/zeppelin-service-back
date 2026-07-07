namespace Zeppelin.Domain.Common;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Dentist = "Dentist";
    public const string Hygienist = "Hygienist";
    public const string FrontDesk = "FrontDesk";

    public static readonly string[] All = [Admin, Dentist, Hygienist, FrontDesk];
}

public static class Policies
{
    public const string ClinicalStaff = "ClinicalStaff";
    public const string SchedulingStaff = "SchedulingStaff";
    public const string AdminOnly = "AdminOnly";
}
