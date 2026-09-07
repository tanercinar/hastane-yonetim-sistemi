namespace HospitalManagement.Modules.Diagnostics.Domain;

public sealed class LabResultItem
{
    public Guid Id
    {
        get; private set;
    }
    public Guid LabResultId
    {
        get; private set;
    }
    public string ParameterCode { get; private set; } = string.Empty;
    public string ParameterName { get; private set; } = string.Empty;
    public decimal? NumericValue
    {
        get; private set;
    }
    public string? StringValue
    {
        get; private set;
    }
    public string? Unit
    {
        get; private set;
    }
    public decimal? ReferenceRangeLow
    {
        get; private set;
    }
    public decimal? ReferenceRangeHigh
    {
        get; private set;
    }
    public string? ReferenceRangeText
    {
        get; private set;
    }
    public LabResultInterpretation Flag
    {
        get; private set;
    }
    public string? Notes
    {
        get; private set;
    }

    private LabResultItem()
    {
    }

    public static LabResultItem Create(
        Guid id,
        Guid labResultId,
        string parameterCode,
        string parameterName,
        decimal? numericValue,
        string? stringValue,
        string? unit,
        decimal? referenceRangeLow,
        decimal? referenceRangeHigh,
        string? referenceRangeText,
        string? notes = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Parametre sonuç kimliği zorunludur.", nameof(id));
        }

        if (labResultId == Guid.Empty)
        {
            throw new ArgumentException("Laboratuvar sonuç kimliği zorunludur.", nameof(labResultId));
        }

        if (string.IsNullOrWhiteSpace(parameterCode))
        {
            throw new ArgumentException("Parametre kodu zorunludur.", nameof(parameterCode));
        }

        if (string.IsNullOrWhiteSpace(parameterName))
        {
            throw new ArgumentException("Parametre adı zorunludur.", nameof(parameterName));
        }

        var flag = EvaluateFlag(numericValue, stringValue, referenceRangeLow, referenceRangeHigh);

        return new LabResultItem
        {
            Id = id,
            LabResultId = labResultId,
            ParameterCode = parameterCode.Trim().ToUpperInvariant(),
            ParameterName = parameterName.Trim(),
            NumericValue = numericValue,
            StringValue = stringValue?.Trim(),
            Unit = unit?.Trim(),
            ReferenceRangeLow = referenceRangeLow,
            ReferenceRangeHigh = referenceRangeHigh,
            ReferenceRangeText = referenceRangeText?.Trim(),
            Flag = flag,
            Notes = notes?.Trim(),
        };
    }

    public void UpdateValue(
        decimal? numericValue,
        string? stringValue,
        string? notes)
    {
        NumericValue = numericValue;
        StringValue = stringValue?.Trim();
        Notes = notes?.Trim();
        Flag = EvaluateFlag(NumericValue, StringValue, ReferenceRangeLow, ReferenceRangeHigh);
    }

    public static LabResultInterpretation EvaluateFlag(
        decimal? numericValue,
        string? stringValue,
        decimal? low,
        decimal? high)
    {
        if (numericValue.HasValue)
        {
            var val = numericValue.Value;

            if (low.HasValue && high.HasValue)
            {
                // Critical thresholds: <= 50% of low, or >= 200% of high
                if (low.Value > 0 && val <= (low.Value * 0.5m))
                {
                    return LabResultInterpretation.CriticalLow;
                }

                if (val >= (high.Value * 2.0m))
                {
                    return LabResultInterpretation.CriticalHigh;
                }

                if (val < low.Value)
                {
                    return LabResultInterpretation.Low;
                }

                if (val > high.Value)
                {
                    return LabResultInterpretation.High;
                }

                return LabResultInterpretation.Normal;
            }

            if (low.HasValue && val < low.Value)
            {
                return LabResultInterpretation.Low;
            }

            if (high.HasValue && val > high.Value)
            {
                return LabResultInterpretation.High;
            }

            return LabResultInterpretation.Normal;
        }

        if (!string.IsNullOrWhiteSpace(stringValue))
        {
            var text = stringValue.Trim().ToLowerInvariant();
            if (text is "pozitif" or "positive" or "reaktif" or "reactive" or "üreme oldu" or "growth detected")
            {
                return LabResultInterpretation.Abnormal;
            }
        }

        return LabResultInterpretation.Normal;
    }
}
