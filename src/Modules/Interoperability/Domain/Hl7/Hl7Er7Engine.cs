using System.Globalization;

namespace HospitalManagement.Modules.Interoperability.Domain.Hl7;

public static class Hl7Er7Engine
{
    private static readonly string[] LineDelimiters = ["\r\n", "\r", "\n"];

    public static (bool IsValid, Hl7V2Message? Message, string? ErrorReason) ParseMessage(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return (false, null, "Mesaj içeriği boş olamaz.");
        }

        var lines = rawContent.Split(LineDelimiters, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            return (false, null, "Mesaj segmenti bulunamadı.");
        }

        var mshLine = lines[0];
        if (!mshLine.StartsWith("MSH|", StringComparison.Ordinal))
        {
            return (false, null, "Mesaj geçerli bir MSH başlık segmenti ile başlamalıdır.");
        }

        var mshFields = mshLine.Split('|');
        // MSH fields:
        // index 0: "MSH"
        // index 1: encoding characters "^~\\&"
        // index 2: sending app
        // index 3: sending facility
        // index 4: receiving app
        // index 5: receiving facility
        // index 6: datetime
        // index 7: security
        // index 8: message type (e.g. "ADT^A01")
        // index 9: message control ID (e.g. "MSG-1001")
        // index 10: processing ID ("P" / "D" / "T")
        // index 11: version ("2.3.1" / "2.5")

        if (mshFields.Length < 10)
        {
            return (false, null, "MSH segmenti eksik alanlar içeriyor (En az 9 alan zorunludur).");
        }

        var messageTypeFull = mshFields[8];
        var typeParts = messageTypeFull.Split('^');
        var messageType = typeParts.Length > 0 ? typeParts[0] : "UNKNOWN";
        var triggerEvent = typeParts.Length > 1 ? typeParts[1] : "";

        var messageControlId = mshFields[9];
        if (string.IsNullOrWhiteSpace(messageControlId))
        {
            return (false, null, "MSH-10 (Message Control ID) boş olamaz.");
        }

        var msg = new Hl7V2Message(
            MessageControlId: messageControlId,
            MessageType: messageType,
            TriggerEvent: triggerEvent,
            TimestampUtc: DateTime.UtcNow,
            RawEr7Content: rawContent);

        return (true, msg, null);
    }

    public static Hl7V2Ack GenerateAck(string messageControlId, string ackCode, string textMessage)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var ackControlId = $"ACK-{Guid.NewGuid():N}"[..12];

        var raw = string.Join("\r",
            $"MSH|^~\\&|HMS_CORE|HMS_HOSPITAL|SENDER_APP|SENDER_FACILITY|{timestamp}||ACK|{ackControlId}|P|2.3.1",
            $"MSA|{ackCode}|{messageControlId}|{textMessage}");

        return new Hl7V2Ack(ackControlId, ackCode, textMessage, raw);
    }

    public static string GenerateAdtA01(Guid patientId, string protocolNumber, string wardName, string bedNumber)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var msgId = $"ADT-{Guid.NewGuid():N}"[..12];

        return string.Join("\r",
            $"MSH|^~\\&|HMS_CORE|HMS_HOSPITAL|EXT_SYSTEM|EXT_FACILITY|{timestamp}||ADT^A01|{msgId}|P|2.3.1",
            $"EVN|A01|{timestamp}",
            $"PID|1||DEMO-PAT-{patientId.ToString()[..8]}||DEMO^PATIENT||19900101|F|||DEMO STREET^^ISTANBUL^34000",
            $"PV1|1|I|{wardName}^{bedNumber}^BED|||||||||||||||{protocolNumber}");
    }

    public static string GenerateAdtA03(Guid patientId, string protocolNumber, DateTime dischargeDateUtc)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var dischargeTs = dischargeDateUtc.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var msgId = $"ADT-{Guid.NewGuid():N}"[..12];

        return string.Join("\r",
            $"MSH|^~\\&|HMS_CORE|HMS_HOSPITAL|EXT_SYSTEM|EXT_FACILITY|{timestamp}||ADT^A03|{msgId}|P|2.3.1",
            $"EVN|A03|{timestamp}",
            $"PID|1||DEMO-PAT-{patientId.ToString()[..8]}||DEMO^PATIENT||19900101|F|||DEMO STREET^^ISTANBUL^34000",
            $"PV1|1|I||||||||||||||||{protocolNumber}|||||||||||||||||||||||||{dischargeTs}");
    }

    public static string GenerateOrmO01(Guid orderId, Guid patientId, string testCode, string testName)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var msgId = $"ORM-{Guid.NewGuid():N}"[..12];

        return string.Join("\r",
            $"MSH|^~\\&|HMS_CORE|HMS_HOSPITAL|LAB_LIS|LAB_FACILITY|{timestamp}||ORM^O01|{msgId}|P|2.3.1",
            $"PID|1||DEMO-PAT-{patientId.ToString()[..8]}||DEMO^PATIENT",
            $"ORC|NW|ORD-{orderId.ToString()[..8]}|||||1^once^^^^R",
            $"OBR|1|ORD-{orderId.ToString()[..8]}||{testCode}^{testName}^LN|||{timestamp}");
    }

    public static string GenerateOruR01(Guid orderId, Guid patientId, string testCode, string resultValue, string units)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var msgId = $"ORU-{Guid.NewGuid():N}"[..12];

        return string.Join("\r",
            $"MSH|^~\\&|LAB_LIS|LAB_FACILITY|HMS_CORE|HMS_HOSPITAL|{timestamp}||ORU^R01|{msgId}|P|2.3.1",
            $"PID|1||DEMO-PAT-{patientId.ToString()[..8]}||DEMO^PATIENT",
            $"OBR|1|ORD-{orderId.ToString()[..8]}||{testCode}^TEST^LN|||{timestamp}",
            $"OBX|1|NM|{testCode}^TEST^LN||{resultValue}|{units}|N|||F");
    }
}
