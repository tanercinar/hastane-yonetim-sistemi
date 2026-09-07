using HospitalManagement.Modules.Diagnostics.Domain;

namespace HospitalManagement.UnitTests.Diagnostics;

public sealed class DiagnosticOrderDomainTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G01")]
    public void CreateDraftOrderWithItemsSucceeds()
    {
        var nowUtc = new DateTime(2026, 8, 30, 10, 0, 0, DateTimeKind.Utc);
        var order = DiagnosticOrder.CreateDraft(
            Guid.NewGuid(),
            "DEMO-LAB-20260830-1001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DiagnosticOrderType.Laboratory,
            DiagnosticOrderPriority.Routine,
            "Rutin kontrol",
            "Aç karnına alınacak",
            nowUtc);

        Assert.Equal(DiagnosticOrderStatus.Draft, order.Status);
        Assert.Equal(DiagnosticOrderType.Laboratory, order.OrderType);
        Assert.Equal(DiagnosticOrderPriority.Routine, order.Priority);
        Assert.Empty(order.Items);

        order.AddItem(
            Guid.NewGuid(),
            "LAB-CBC",
            "Tam Kan Sayımı (Hemogram)",
            "Hematology",
            "EDTA mor tüp",
            nowUtc);

        order.AddItem(
            Guid.NewGuid(),
            "LAB-BIO-GLU",
            "Açlık Kan Şekeri (Glukoz)",
            "Biochemistry",
            "Jelli sarı tüp",
            nowUtc);

        Assert.Equal(2, order.Items.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G01")]
    public void AddingDuplicateItemThrowsInvalidOperationException()
    {
        var nowUtc = new DateTime(2026, 8, 30, 10, 0, 0, DateTimeKind.Utc);
        var order = DiagnosticOrder.CreateDraft(
            Guid.NewGuid(),
            "DEMO-LAB-20260830-1002",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DiagnosticOrderType.Laboratory,
            DiagnosticOrderPriority.Routine,
            null,
            null,
            nowUtc);

        order.AddItem(
            Guid.NewGuid(),
            "LAB-CBC",
            "Tam Kan Sayımı (Hemogram)",
            "Hematology",
            null,
            nowUtc);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            order.AddItem(
                Guid.NewGuid(),
                "LAB-CBC",
                "Tam Kan Sayımı (Hemogram)",
                "Hematology",
                null,
                nowUtc));

        Assert.Contains("zaten bu test", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G01")]
    public void PlacingOrderTransitionsStatusToPlaced()
    {
        var nowUtc = new DateTime(2026, 8, 30, 10, 0, 0, DateTimeKind.Utc);
        var order = DiagnosticOrder.CreateDraft(
            Guid.NewGuid(),
            "DEMO-LAB-20260830-1003",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DiagnosticOrderType.Laboratory,
            DiagnosticOrderPriority.Urgent,
            "Acil tetkik",
            null,
            nowUtc);

        order.AddItem(
            Guid.NewGuid(),
            "LAB-TROP",
            "Troponin I",
            "Biochemistry",
            "STAT acil çalışılacak",
            nowUtc);

        order.Place(order.PlacingDoctorId, nowUtc.AddMinutes(5));

        Assert.Equal(DiagnosticOrderStatus.Placed, order.Status);
        Assert.NotNull(order.PlacedAtUtc);

        // Cannot add items after placing
        var ex = Assert.Throws<InvalidOperationException>(() =>
            order.AddItem(
                Guid.NewGuid(),
                "LAB-CKMB",
                "CK-MB",
                "Biochemistry",
                null,
                nowUtc));

        Assert.Contains("Yalnızca taslak", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G01")]
    public void PlacingEmptyOrderThrowsInvalidOperationException()
    {
        var nowUtc = new DateTime(2026, 8, 30, 10, 0, 0, DateTimeKind.Utc);
        var order = DiagnosticOrder.CreateDraft(
            Guid.NewGuid(),
            "DEMO-LAB-20260830-1004",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DiagnosticOrderType.Laboratory,
            DiagnosticOrderPriority.Routine,
            null,
            null,
            nowUtc);

        var ex = Assert.Throws<InvalidOperationException>(() => order.Place(order.PlacingDoctorId, nowUtc));
        Assert.Contains("En az bir test", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F06-G01")]
    public void CancellingOrderSetsReasonAndCancelsItems()
    {
        var nowUtc = new DateTime(2026, 8, 30, 10, 0, 0, DateTimeKind.Utc);
        var order = DiagnosticOrder.CreateDraft(
            Guid.NewGuid(),
            "DEMO-LAB-20260830-1005",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DiagnosticOrderType.Laboratory,
            DiagnosticOrderPriority.Routine,
            null,
            null,
            nowUtc);

        order.AddItem(
            Guid.NewGuid(),
            "LAB-CBC",
            "Tam Kan Sayımı",
            "Hematology",
            null,
            nowUtc);

        order.Place(order.PlacingDoctorId, nowUtc);
        order.Cancel(order.PlacingDoctorId, "Hasta taburcu oldu", nowUtc.AddHours(1));

        Assert.Equal(DiagnosticOrderStatus.Cancelled, order.Status);
        Assert.Equal("Hasta taburcu oldu", order.CancellationReason);
        Assert.Equal(DiagnosticOrderItemStatus.Cancelled, order.Items.First().Status);
    }
}
