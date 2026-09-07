using HospitalManagement.Modules.Inpatient.Application;
using HospitalManagement.Modules.Inpatient.Domain;
using HospitalManagement.Modules.Inpatient.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Modules.Inpatient.Infrastructure;

public sealed class InpatientDataSeeder : IInpatientDataSeeder
{
    private static readonly Guid CardiologyDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000003");

    private static readonly Guid InternalMedicineDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000002");

    private static readonly Guid IntensiveCareDepartmentId =
        Guid.Parse("30000000-0000-0000-0000-000000000008");

    public static readonly Guid CardiologyWardId =
        Guid.Parse("50000000-0000-0000-0000-000000000001");

    public static readonly Guid InternalMedicineWardId =
        Guid.Parse("50000000-0000-0000-0000-000000000002");

    public static readonly Guid IntensiveCareWardId =
        Guid.Parse("50000000-0000-0000-0000-000000000003");

    public static readonly Guid Bed301AId =
        Guid.Parse("51000000-0000-0000-0000-000000000001");
    public static readonly Guid Bed301BId =
        Guid.Parse("51000000-0000-0000-0000-000000000002");
    public static readonly Guid Bed302AId =
        Guid.Parse("51000000-0000-0000-0000-000000000003");
    public static readonly Guid Bed302BId =
        Guid.Parse("51000000-0000-0000-0000-000000000004");
    public static readonly Guid Bed303AId =
        Guid.Parse("51000000-0000-0000-0000-000000000005");
    public static readonly Guid Bed201AId =
        Guid.Parse("51000000-0000-0000-0000-000000000006");
    public static readonly Guid Bed201BId =
        Guid.Parse("51000000-0000-0000-0000-000000000007");

    private readonly InpatientDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public InpatientDataSeeder(InpatientDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        if (await _dbContext.Wards.AnyAsync(cancellationToken))
        {
            return;
        }

        // 1. Cardiology Ward
        var cardWard = Ward.Create(
            CardiologyWardId,
            "DEMO-WRD-CARD",
            "Kardiyoloji & Koroner Bakım Servisi",
            CardiologyDepartmentId,
            "Ana Bina",
            "3. Kat",
            WardType.CoronaryCare,
            nowUtc);

        var room301Id = Guid.Parse("50500000-0000-0000-0000-000000000001");
        var room301 = Room.Create(room301Id, CardiologyWardId, "301", BedPlacementGender.MaleOnly, IsolationType.None, false, nowUtc);
        var bed301A = Bed.Create(Bed301AId, CardiologyWardId, room301Id, "301-A", BedPlacementGender.MaleOnly, IsolationType.None, true, true, false, nowUtc);
        var bed301B = Bed.Create(Bed301BId, CardiologyWardId, room301Id, "301-B", BedPlacementGender.MaleOnly, IsolationType.None, true, true, false, nowUtc);

        var room302Id = Guid.Parse("50500000-0000-0000-0000-000000000002");
        var room302 = Room.Create(room302Id, CardiologyWardId, "302", BedPlacementGender.FemaleOnly, IsolationType.None, false, nowUtc);
        var bed302A = Bed.Create(Bed302AId, CardiologyWardId, room302Id, "302-A", BedPlacementGender.FemaleOnly, IsolationType.None, true, true, false, nowUtc);
        var bed302B = Bed.Create(Bed302BId, CardiologyWardId, room302Id, "302-B", BedPlacementGender.FemaleOnly, IsolationType.None, true, true, false, nowUtc);

        var room303Id = Guid.Parse("50500000-0000-0000-0000-000000000003");
        var room303 = Room.Create(room303Id, CardiologyWardId, "303-İzole", BedPlacementGender.Any, IsolationType.Contact, false, nowUtc);
        var bed303A = Bed.Create(Bed303AId, CardiologyWardId, room303Id, "303-A", BedPlacementGender.Any, IsolationType.Contact, true, true, false, nowUtc);

        // 2. Internal Medicine Ward
        var medWard = Ward.Create(
            InternalMedicineWardId,
            "DEMO-WRD-INTMED",
            "Genel Dahiliye Servisi",
            InternalMedicineDepartmentId,
            "Ana Bina",
            "2. Kat",
            WardType.GeneralAdult,
            nowUtc);

        var room201Id = Guid.Parse("50500000-0000-0000-0000-000000000004");
        var room201 = Room.Create(room201Id, InternalMedicineWardId, "201", BedPlacementGender.MaleOnly, IsolationType.None, false, nowUtc);
        var bed201A = Bed.Create(Bed201AId, InternalMedicineWardId, room201Id, "201-A", BedPlacementGender.MaleOnly, IsolationType.None, false, true, false, nowUtc);
        var bed201B = Bed.Create(Bed201BId, InternalMedicineWardId, room201Id, "201-B", BedPlacementGender.MaleOnly, IsolationType.None, false, true, false, nowUtc);

        var room202Id = Guid.Parse("50500000-0000-0000-0000-000000000005");
        var room202 = Room.Create(room202Id, InternalMedicineWardId, "202", BedPlacementGender.FemaleOnly, IsolationType.None, false, nowUtc);
        var bed202A = Bed.Create(Guid.Parse("51000000-0000-0000-0000-000000000008"), InternalMedicineWardId, room202Id, "202-A", BedPlacementGender.FemaleOnly, IsolationType.None, false, true, false, nowUtc);
        var bed202B = Bed.Create(Guid.Parse("51000000-0000-0000-0000-000000000009"), InternalMedicineWardId, room202Id, "202-B", BedPlacementGender.FemaleOnly, IsolationType.None, false, true, false, nowUtc);

        var room203Id = Guid.Parse("50500000-0000-0000-0000-000000000006");
        var room203 = Room.Create(room203Id, InternalMedicineWardId, "203-NegatifBasınç", BedPlacementGender.Any, IsolationType.Airborne, true, nowUtc);
        var bed203A = Bed.Create(Guid.Parse("51000000-0000-0000-0000-000000000010"), InternalMedicineWardId, room203Id, "203-A", BedPlacementGender.Any, IsolationType.Airborne, true, true, false, nowUtc);

        // 3. Intensive Care Unit (ICU)
        var icuWard = Ward.Create(
            IntensiveCareWardId,
            "DEMO-WRD-ICU",
            "Genel Yoğun Bakım Ünitesi",
            IntensiveCareDepartmentId,
            "Kritik Bakım Bloğu",
            "1. Kat",
            WardType.IntensiveCare,
            nowUtc);

        var roomIcu1Id = Guid.Parse("50500000-0000-0000-0000-000000000007");
        var roomIcu1 = Room.Create(roomIcu1Id, IntensiveCareWardId, "ICU-01", BedPlacementGender.Any, IsolationType.None, false, nowUtc);
        var bedIcu1 = Bed.Create(Guid.Parse("51000000-0000-0000-0000-000000000011"), IntensiveCareWardId, roomIcu1Id, "ICU-BED-01", BedPlacementGender.Any, IsolationType.None, true, true, true, nowUtc);
        var bedIcu2 = Bed.Create(Guid.Parse("51000000-0000-0000-0000-000000000012"), IntensiveCareWardId, roomIcu1Id, "ICU-BED-02", BedPlacementGender.Any, IsolationType.None, true, true, true, nowUtc);

        _dbContext.Wards.AddRange(cardWard, medWard, icuWard);
        _dbContext.Rooms.AddRange(room301, room302, room303, room201, room202, room203, roomIcu1);
        _dbContext.Beds.AddRange(bed301A, bed301B, bed302A, bed302B, bed303A, bed201A, bed201B, bed202A, bed202B, bed203A, bedIcu1, bedIcu2);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
