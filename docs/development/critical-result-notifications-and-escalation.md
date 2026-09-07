# Kritik Sonuç Bildirimleri ve Eskalasyon İş Akışı (F06-G05)

## 1. Amaç ve Kapsam

Bu belge, Hastane Yönetim Sistemi Tanısal Hizmetler modülündeki kritik (panik) laboratuvar değerlerinin otomatik tespiti, ilgili istemci hekime ve bakım ekibine gerçek zamanlı/uygulama içi bildirim iletilmesi, hekim teslim ve alındı teyidi (acknowledgment) ve eskalasyon iş akışını belgeler.

> [!NOTE]
> Sistem bir eğitim ve demo platformudur. Kritik değerler sentetik parametrelere ve referans sınırlarına göre otomatik bayraklanır. Gerçek SMS/çağrı cihazı donanımı bağlı değildir; eskalasyon ve alındı teyidi simüle edilir.

---

## 2. Kritik Sonuç Durum Yaşam Döngüsü

Kritik sonuç bildirimleri aşağıdaki yaşam döngüsüyle yönetilir:

```
  [ Kritik Değer Tespiti ] ──► [ Active (Seviye 1 - Birincil Hekim) ]
                                      │                     │
               (Eskalasyon Simülasyonu)│                     │ (Alındı Teyidi)
                                      ▼                     ▼
          [ Escalated (Seviye 2+ - Klinik Sorumlusu) ]      [ Acknowledged ]
                                      │                     (Teslim Kanıtı)
                                      │ (Alındı Teyidi)
                                      ▼
                              [ Acknowledged ]
```

### Durum ve Seviye Açıklamaları

1. **Active (Aktif):** Laboratuvar sonucu taslak olarak girildiğinde veya güncellendiğinde parametre değeri `CriticalLow` veya `CriticalHigh` sınırını aşarsa otomatik olarak aktif bildirim üretilir. Bildirim istemi veren sorumlu hekime (`ResponsibleDoctorUserId`) yönlendirilir.
2. **Escalated (Eskale Edildi):** Belirli süre yanıt alınamayan veya ulaşılamayan durumlarda bildirim eskalasyon seviyesi artırılarak (`EscalationLevel = 2+`) servis/nöbetçi sorumlusuna iletilir. Zorunlu eskalasyon gerekçesi (`EscalationReason`) kaydedilir.
3. **Acknowledged (Alındı Teyitli):** Hekim veya yetkili sağlık personeli kritik değeri teslim aldığını, hastaya müdahale veya klinik eylemi içeren zorunlu alındı notu (`AcknowledgmentNotes`) ile teyit eder. Teyit eden kullanıcı (`AcknowledgedByUserId`) ve zamanı (`AcknowledgedAtUtc`) kaydedilir.
4. **Closed (Kapatıldı):** Klinik sürecin tamamlanmasıyla kapatılan bildirimler.

---

## 3. Güvenlik, Doğrulama ve Audit Kuralları

- **Hasta İzolasyonu:** Hasta rolündeki kullanıcılar kritik değer alındı teyidi veremez (`403 Forbidden`).
- **Zorunlu Teslim Kanıtı:** Alındı teyidinde `acknowledgment_notes` boş bırakılamaz; yapılan klinik eylem ve teyit notu kaydedilmelidir.
- **Zorunlu Eskalasyon Gerekçesi:** Eskalasyon işleminde `escalation_reason` boş bırakılamaz.
- **Audit Günlüğü:** `Diagnostics.CriticalResultNotify`, `Diagnostics.CriticalResultEscalate` ve `Diagnostics.CriticalResultAcknowledge` eylemleri `audit_logs` tablosuna otomatik olarak kaydedilir.

---

## 4. API Uç Noktaları

| Metot | Yol | İzin / Rol | Açıklama |
|---|---|---|---|
| `GET` | `/api/v1/diagnostics/critical-notifications/active` | Yetkili Personel | Bekleyen aktif ve eskale kritik bildirimler listesi |
| `GET` | `/api/v1/diagnostics/critical-notifications/{id}` | İlgili Rol / Hasta | Kritik bildirim detayı |
| `POST` | `/api/v1/diagnostics/critical-notifications/{id}/acknowledge` | Sağlık Personeli | Kritik sonuç alındı ve eylem teyidi |
| `POST` | `/api/v1/diagnostics/critical-notifications/{id}/escalate` | Sağlık Personeli | Kritik bildirim eskalasyon simülasyonu |
