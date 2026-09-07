using HospitalManagement.Modules.Interoperability.Domain.Hl7;
using Xunit;

namespace HospitalManagement.UnitTests.Interoperability;

public sealed class Hl7V2ParserAndGeneratorTests
{
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G03")]
    public void Hl7ParserParsesValidMshAndExtractsFields()
    {
        var rawEr7 = "MSH|^~\\&|LAB_LIS|LAB_FACILITY|HMS_CORE|HMS_HOSPITAL|20260901120000||ORU^R01|MSG-98765|P|2.3.1\rPID|1||DEMO-PAT-123\rOBR|1|ORD-1\rOBX|1|NM|GLU||95|mg/dL|N|||F";

        var (isValid, msg, error) = Hl7Er7Engine.ParseMessage(rawEr7);

        Assert.True(isValid);
        Assert.Null(error);
        Assert.NotNull(msg);
        Assert.Equal("MSG-98765", msg.MessageControlId);
        Assert.Equal("ORU", msg.MessageType);
        Assert.Equal("R01", msg.TriggerEvent);
        Assert.Equal(rawEr7, msg.RawEr7Content);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G03")]
    public void Hl7ParserRejectsMalformedMessageAndReturnsError()
    {
        // 1. Empty string
        var (v1, _, err1) = Hl7Er7Engine.ParseMessage("");
        Assert.False(v1);
        Assert.NotNull(err1);

        // 2. Missing MSH
        var (v2, _, err2) = Hl7Er7Engine.ParseMessage("PID|1||DEMO-123");
        Assert.False(v2);
        Assert.Contains("MSH", err2, StringComparison.Ordinal);

        // 3. Short MSH
        var (v3, _, err3) = Hl7Er7Engine.ParseMessage("MSH|^~\\&|APP|FAC");
        Assert.False(v3);
        Assert.Contains("eksik alanlar", err3, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G03")]
    public void Hl7GeneratorProducesValidEr7Segments()
    {
        var patId = Guid.NewGuid();
        var orderId = Guid.NewGuid();

        // 1. ADT A01
        var a01 = Hl7Er7Engine.GenerateAdtA01(patId, "PROTO-101", "Kardiyoloji", "Bed-2");
        Assert.Contains("ADT^A01", a01, StringComparison.Ordinal);
        Assert.Contains("PV1|1|I|Kardiyoloji^Bed-2^BED", a01, StringComparison.Ordinal);

        // 2. ADT A03
        var a03 = Hl7Er7Engine.GenerateAdtA03(patId, "PROTO-101", DateTime.UtcNow);
        Assert.Contains("ADT^A03", a03, StringComparison.Ordinal);

        // 3. ORM O01
        var o01 = Hl7Er7Engine.GenerateOrmO01(orderId, patId, "GLU", "Glikoz");
        Assert.Contains("ORM^O01", o01, StringComparison.Ordinal);
        Assert.Contains("GLU^Glikoz^LN", o01, StringComparison.Ordinal);

        // 4. ORU R01
        var r01 = Hl7Er7Engine.GenerateOruR01(orderId, patId, "GLU", "98", "mg/dL");
        Assert.Contains("ORU^R01", r01, StringComparison.Ordinal);
        Assert.Contains("OBX|1|NM|GLU^TEST^LN||98|mg/dL|N|||F", r01, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Roadmap", "F10-G03")]
    public void Hl7AckGenerationProducesValidMsa()
    {
        var ack = Hl7Er7Engine.GenerateAck("MSG-1122", "AA", "Kabul edildi");
        Assert.Equal("AA", ack.AckCode);
        Assert.Equal("Kabul edildi", ack.TextMessage);
        Assert.Contains("MSA|AA|MSG-1122|Kabul edildi", ack.RawEr7Content, StringComparison.Ordinal);

        var errAck = Hl7Er7Engine.GenerateAck("MSG-1122", "AE", "Segment hatası");
        Assert.Equal("AE", errAck.AckCode);
        Assert.Contains("MSA|AE|MSG-1122|Segment hatası", errAck.RawEr7Content, StringComparison.Ordinal);
    }
}
