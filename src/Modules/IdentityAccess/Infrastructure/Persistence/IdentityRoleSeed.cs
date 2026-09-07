using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.IdentityAccess.Infrastructure.Persistence;

internal static class IdentityRoleSeed
{
    internal static readonly (Guid Id, string Code, string Name)[] Roles =
    [
        (new Guid("40000000-0000-0000-0000-000000000001"), "PAT", "Hasta"),
        (new Guid("40000000-0000-0000-0000-000000000002"), "DOC", "Doktor"),
        (new Guid("40000000-0000-0000-0000-000000000003"), "NUR", "Hemşire"),
        (new Guid("40000000-0000-0000-0000-000000000004"), "CHM", "Başhekim"),
        (new Guid("40000000-0000-0000-0000-000000000005"), "REG", "Kayıt/Danışma Personeli"),
        (new Guid("40000000-0000-0000-0000-000000000006"), "LAB", "Laboratuvar Personeli"),
        (new Guid("40000000-0000-0000-0000-000000000007"), "RAD", "Radyoloji Personeli"),
        (new Guid("40000000-0000-0000-0000-000000000008"), "PHA", "Eczacı"),
        (new Guid("40000000-0000-0000-0000-000000000009"), "ADM", "Sistem Yöneticisi"),
        (new Guid("40000000-0000-0000-0000-00000000000a"), "MGR", "Hastane Yöneticisi"),
        (new Guid("40000000-0000-0000-0000-00000000000b"), "FIN", "Muhasebe Personeli"),
        (new Guid("40000000-0000-0000-0000-00000000000c"), "HR", "İnsan Kaynakları Personeli"),
    ];

    internal static void Configure(ModelBuilder builder)
    {
        var identityRoles = Roles.Select(r => new IdentityRole<Guid>
        {
            Id = r.Id,
            Name = r.Code,
            NormalizedName = r.Code.ToUpperInvariant(),
            ConcurrencyStamp = r.Id.ToString("D", System.Globalization.CultureInfo.InvariantCulture),
        }).ToArray();

        builder.Entity<IdentityRole<Guid>>().HasData(identityRoles);
    }
}
