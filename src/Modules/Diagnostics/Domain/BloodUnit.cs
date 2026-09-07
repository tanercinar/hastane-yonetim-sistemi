namespace HospitalManagement.Modules.Diagnostics.Domain;

public sealed class BloodUnit
{
    private BloodUnit()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public string UnitNumber { get; private set; } = null!;
    public BloodProductType ProductType
    {
        get; private set;
    }
    public BloodGroup BloodGroup
    {
        get; private set;
    }
    public int VolumeMl
    {
        get; private set;
    }
    public DateTime DonationDateUtc
    {
        get; private set;
    }
    public DateTime ExpiryDateUtc
    {
        get; private set;
    }
    public string StorageLocation { get; private set; } = null!;
    public BloodUnitStatus Status
    {
        get; private set;
    }

    public Guid? ReservedForPatientId
    {
        get; private set;
    }
    public DateTime? ReservedUntilUtc
    {
        get; private set;
    }

    public DateTime CreatedAtUtc
    {
        get; private set;
    }
    public DateTime? UpdatedAtUtc
    {
        get; private set;
    }
    public int Version { get; set; } = 1;

    public static BloodUnit Create(
        Guid id,
        string unitNumber,
        BloodProductType productType,
        BloodGroup bloodGroup,
        int volumeMl,
        DateTime donationDateUtc,
        DateTime expiryDateUtc,
        string storageLocation,
        DateTime nowUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id zorunludur.", nameof(id));
        if (string.IsNullOrWhiteSpace(unitNumber))
            throw new ArgumentException("Ünite numarası zorunludur.", nameof(unitNumber));
        if (string.IsNullOrWhiteSpace(storageLocation))
            throw new ArgumentException("Saklama dolabı / lokasyonu zorunludur.", nameof(storageLocation));
        if (volumeMl <= 0)
            throw new ArgumentException("Hacim 0'dan büyük olmalıdır.", nameof(volumeMl));

        return new BloodUnit
        {
            Id = id,
            UnitNumber = unitNumber.Trim(),
            ProductType = productType,
            BloodGroup = bloodGroup,
            VolumeMl = volumeMl,
            DonationDateUtc = donationDateUtc,
            ExpiryDateUtc = expiryDateUtc,
            StorageLocation = storageLocation.Trim(),
            Status = BloodUnitStatus.Available,
            CreatedAtUtc = nowUtc,
            Version = 1,
        };
    }

    public void Reserve(Guid patientId, DateTime untilUtc, DateTime nowUtc)
    {
        if (Status != BloodUnitStatus.Available)
        {
            throw new InvalidOperationException($"Yalnızca Müsait durumdaki üniteler rezerve edilebilir. Mevcut durum: {Status}");
        }

        if (nowUtc > ExpiryDateUtc)
        {
            throw new InvalidOperationException("Son kullanma tarihi geçmiş kan ürünü rezerve edilemez.");
        }

        ReservedForPatientId = patientId;
        ReservedUntilUtc = untilUtc;
        Status = BloodUnitStatus.Reserved;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void ReleaseReservation(DateTime nowUtc)
    {
        if (Status != BloodUnitStatus.Reserved)
        {
            throw new InvalidOperationException($"Yalnızca Rezerve durumdaki ünitelerin rezervasyonu kaldırılabilir. Mevcut durum: {Status}");
        }

        ReservedForPatientId = null;
        ReservedUntilUtc = null;
        Status = BloodUnitStatus.Available;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Issue(DateTime nowUtc)
    {
        if (Status != BloodUnitStatus.Reserved && Status != BloodUnitStatus.Available)
        {
            throw new InvalidOperationException($"Kan ürünü çıkışı yalnızca Müsait veya Rezerve durumdayken yapılabilir. Mevcut durum: {Status}");
        }

        if (nowUtc > ExpiryDateUtc)
        {
            throw new InvalidOperationException("Son kullanma tarihi geçmiş kan ürünü çıkışı yapılamaz.");
        }

        Status = BloodUnitStatus.Issued;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void RecordTransfusion(DateTime nowUtc)
    {
        if (Status != BloodUnitStatus.Issued)
        {
            throw new InvalidOperationException($"Transfüzyon kaydı yalnızca Çıkışı Yapılmış (Issued) durumundaki ünite için girilebilir. Mevcut durum: {Status}");
        }

        Status = BloodUnitStatus.Transfused;
        UpdatedAtUtc = nowUtc;
        Version++;
    }

    public void Discard(string reason, DateTime nowUtc)
    {
        if (Status == BloodUnitStatus.Transfused)
        {
            throw new InvalidOperationException("Transfüzyonu tamamlanmış bir ünite imha edilemez.");
        }

        Status = BloodUnitStatus.Discarded;
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}
