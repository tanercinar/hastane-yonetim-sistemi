namespace HospitalManagement.Modules.Pharmacy.Domain;

public sealed class PrescriptionItem
{
    private PrescriptionItem()
    {
    }

    public Guid Id
    {
        get; private set;
    }
    public Guid PrescriptionId
    {
        get; private set;
    }
    public Guid MedicationCatalogItemId
    {
        get; private set;
    }
    public string MedicationCode { get; private set; } = string.Empty;
    public string BrandName { get; private set; } = string.Empty;
    public string GenericName { get; private set; } = string.Empty;
    public MedicationForm Form
    {
        get; private set;
    }
    public MedicationRoute Route
    {
        get; private set;
    }
    public decimal Dose
    {
        get; private set;
    }
    public string DoseUnit { get; private set; } = string.Empty;
    public string Frequency { get; private set; } = string.Empty;
    public int DurationDays
    {
        get; private set;
    }
    public int Quantity
    {
        get; private set;
    }
    public string QuantityUnit { get; private set; } = string.Empty;
    public int DispensedQuantity
    {
        get; private set;
    }
    public string? Instructions
    {
        get; private set;
    }
    public DateTime CreatedAtUtc
    {
        get; private set;
    }

    public bool IsFullyDispensed => DispensedQuantity >= Quantity;

    public static PrescriptionItem Create(
        Guid id,
        Guid prescriptionId,
        Guid medicationCatalogItemId,
        string medicationCode,
        string brandName,
        string genericName,
        MedicationForm form,
        MedicationRoute route,
        decimal dose,
        string doseUnit,
        string frequency,
        int durationDays,
        int quantity,
        string quantityUnit,
        string? instructions,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Reçete kalemi kimliği boş olamaz.", nameof(id));
        }

        if (prescriptionId == Guid.Empty)
        {
            throw new ArgumentException("Reçete kimliği boş olamaz.", nameof(prescriptionId));
        }

        if (medicationCatalogItemId == Guid.Empty)
        {
            throw new ArgumentException("İlaç kataloğu kimliği boş olamaz.", nameof(medicationCatalogItemId));
        }

        if (string.IsNullOrWhiteSpace(medicationCode))
        {
            throw new ArgumentException("İlaç kodu boş olamaz.", nameof(medicationCode));
        }

        if (string.IsNullOrWhiteSpace(brandName))
        {
            throw new ArgumentException("İlaç ticari adı boş olamaz.", nameof(brandName));
        }

        if (string.IsNullOrWhiteSpace(genericName))
        {
            throw new ArgumentException("İlaç etken madde adı boş olamaz.", nameof(genericName));
        }

        if (dose <= 0)
        {
            throw new ArgumentException("İlaç dozu 0'dan büyük olmalıdır.", nameof(dose));
        }

        if (string.IsNullOrWhiteSpace(doseUnit))
        {
            throw new ArgumentException("Doz birimi boş olamaz.", nameof(doseUnit));
        }

        if (string.IsNullOrWhiteSpace(frequency))
        {
            throw new ArgumentException("Kullanım sıklığı boş olamaz.", nameof(frequency));
        }

        if (durationDays <= 0)
        {
            throw new ArgumentException("Kullanım süresi (gün) 0'dan büyük olmalıdır.", nameof(durationDays));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Reçete edilen miktar 0'dan büyük olmalıdır.", nameof(quantity));
        }

        if (string.IsNullOrWhiteSpace(quantityUnit))
        {
            throw new ArgumentException("Miktar birimi boş olamaz.", nameof(quantityUnit));
        }

        return new PrescriptionItem
        {
            Id = id,
            PrescriptionId = prescriptionId,
            MedicationCatalogItemId = medicationCatalogItemId,
            MedicationCode = medicationCode.Trim(),
            BrandName = brandName.Trim(),
            GenericName = genericName.Trim(),
            Form = form,
            Route = route,
            Dose = dose,
            DoseUnit = doseUnit.Trim(),
            Frequency = frequency.Trim(),
            DurationDays = durationDays,
            Quantity = quantity,
            QuantityUnit = quantityUnit.Trim(),
            DispensedQuantity = 0,
            Instructions = string.IsNullOrWhiteSpace(instructions) ? null : instructions.Trim(),
            CreatedAtUtc = createdAtUtc,
        };
    }

    public void Update(
        Guid medicationCatalogItemId,
        string medicationCode,
        string brandName,
        string genericName,
        MedicationForm form,
        MedicationRoute route,
        decimal dose,
        string doseUnit,
        string frequency,
        int durationDays,
        int quantity,
        string quantityUnit,
        string? instructions)
    {
        if (medicationCatalogItemId == Guid.Empty)
        {
            throw new ArgumentException("İlaç kataloğu kimliği boş olamaz.", nameof(medicationCatalogItemId));
        }

        if (string.IsNullOrWhiteSpace(medicationCode))
        {
            throw new ArgumentException("İlaç kodu boş olamaz.", nameof(medicationCode));
        }

        if (string.IsNullOrWhiteSpace(brandName))
        {
            throw new ArgumentException("İlaç ticari adı boş olamaz.", nameof(brandName));
        }

        if (string.IsNullOrWhiteSpace(genericName))
        {
            throw new ArgumentException("İlaç etken madde adı boş olamaz.", nameof(genericName));
        }

        if (dose <= 0)
        {
            throw new ArgumentException("İlaç dozu 0'dan büyük olmalıdır.", nameof(dose));
        }

        if (string.IsNullOrWhiteSpace(doseUnit))
        {
            throw new ArgumentException("Doz birimi boş olamaz.", nameof(doseUnit));
        }

        if (string.IsNullOrWhiteSpace(frequency))
        {
            throw new ArgumentException("Kullanım sıklığı boş olamaz.", nameof(frequency));
        }

        if (durationDays <= 0)
        {
            throw new ArgumentException("Kullanım süresi (gün) 0'dan büyük olmalıdır.", nameof(durationDays));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Reçete edilen miktar 0'dan büyük olmalıdır.", nameof(quantity));
        }

        if (string.IsNullOrWhiteSpace(quantityUnit))
        {
            throw new ArgumentException("Miktar birimi boş olamaz.", nameof(quantityUnit));
        }

        MedicationCatalogItemId = medicationCatalogItemId;
        MedicationCode = medicationCode.Trim();
        BrandName = brandName.Trim();
        GenericName = genericName.Trim();
        Form = form;
        Route = route;
        Dose = dose;
        DoseUnit = doseUnit.Trim();
        Frequency = frequency.Trim();
        DurationDays = durationDays;
        Quantity = quantity;
        QuantityUnit = quantityUnit.Trim();
        Instructions = string.IsNullOrWhiteSpace(instructions) ? null : instructions.Trim();
    }

    public void RecordDispense(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Teslim miktarı 0'dan büyük olmalıdır.", nameof(amount));
        }

        if (DispensedQuantity + amount > Quantity)
        {
            throw new InvalidOperationException($"Reçete edilen toplam miktardan ({Quantity}) fazla teslim ({DispensedQuantity + amount}) yapılamaz.");
        }

        DispensedQuantity += amount;
    }
}
