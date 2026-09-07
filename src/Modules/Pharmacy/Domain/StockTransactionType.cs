namespace HospitalManagement.Modules.Pharmacy.Domain;

public enum StockTransactionType
{
    InitialReceipt = 1,
    Dispense = 2,
    AdjustmentIn = 3,
    AdjustmentOut = 4,
    Return = 5,
    Expired = 6,
}
