# Numune ve Barkod Devir-Teslim Zinciri İş Akışı (F06-G03)

## 1. Amaç ve Kapsam

Bu belge, Hastane Yönetim Sistemi Tanısal Hizmetler modülündeki numune toplama, barkodlama, devir-teslim (custody chain), laboratuvara kabul ve numune red iş akışını belgeler.

> [!NOTE]
> Sistem bir eğitim ve demo platformudur. Tüm numune kayıtları sentetik `DEMO-SMP-YYYYMMDD-XXXXXX` biçiminde barkodlanır ve laboratuvar analizörleriyle doğrudan donanımsal iletişim kurmaz.

---

## 2. Numune Durum Yaşam Döngüsü

Numune yaşam döngüsü aşağıdaki durum geçiş makinesiyle yönetilir:

```
 [ İstem Verildi (Placed) ]
            │
            ▼
     [ Collected ] ──(Taşımaya Ver)──► [ InTransit ]
            │                               │
            │ (Doğrudan Teslim)             │ (Kurye / Pnömatik Hat)
            ▼                               ▼
     [ Received ] ◄─────────────────────────┘
            │
            ├────────(Reddedildi)────────► [ Rejected ]
            │
            ▼
    [ Processing ] ──(Tamamlandı)──► [ Completed ] ──(İmha)──► [ Disposed ]
```

### Durum Açıklamaları

1. **Collected (Toplandı):** Hemşire veya flebotomist hastadan numuneyi alır, tüp türünü ve toplama notunu girer. Benzersiz `DEMO-SMP-...` barkodu üretilir.
2. **InTransit (Taşımada):** Numune kuryeye veya pnömatik taşıma hattına verilir; devir-teslim günlüğüne konum ve zaman kaydedilir.
3. **Received (Laboratuvarda Kabul Edildi):** Laboratuvar teknisyeni numuneyi teslim alır, barkodu okutur veya doğrular.
4. **Rejected (Reddedildi):** Numunede hemoliz, pıhtılaşma, yetersiz hacim veya uygunsuz tüp gibi analize engel bir durum varsa zorunlu gerekçe ile reddedilir.
5. **Processing (Çalışmada):** Numune analizöre veya çalışma istasyonuna yüklenir.
6. **Completed (Tamamlandı):** Numunenin analizi tamamlanır.
7. **Disposed (İmha Edildi):** Yasal saklama süresi sonrası tıbbi atık protokolü ile güvenli biçimde imha edilir.

---

## 3. Güvenlik, Doğrulama ve Audit Kuralları

- **Yanlış Hasta Koruması:** İstemdeki hasta kimliği ile numune alma sırasındaki hasta kimliği uyuşmazsa işlem `400 Bad Request` ile reddedilir.
- **Zorunlu Red Gerekçesi:** Numune reddinde gerekçe boş bırakılamaz (`rejection_reason` zorunlu alandır).
- **İzlenebilirlik (Custody Chain):** Her durum geçişinde aktör kimliği (`actor_user_id`), rolü (`actor_role`), konumu (`location`), notu (`notes`) ve zaman damgası (`transitioned_at_utc`) `specimen_transition_events` tablosunda kalıcı olarak saklanır.
- **Audit Günlüğü:** `Diagnostics.SpecimenCollect`, `Diagnostics.SpecimenTransit`, `Diagnostics.SpecimenReceive` ve `Diagnostics.SpecimenReject` eylemleri `audit_logs` tablosuna otomatik olarak yazılır.

---

## 4. API Uç Noktaları

| Metot | Yol | İzin | Açıklama |
|---|---|---|---|
| `POST` | `/api/v1/diagnostics/specimens/collect` | `laboratory.specimen.transition` | Numune toplama ve barkod üretme |
| `POST` | `/api/v1/diagnostics/specimens/{id}/transit` | `laboratory.specimen.transition` | Numuneyi taşımaya verme |
| `POST` | `/api/v1/diagnostics/specimens/{id}/receive` | `laboratory.specimen.transition` | Laboratuvarda numune kabul |
| `POST` | `/api/v1/diagnostics/specimens/{id}/reject` | `laboratory.specimen.transition` | Numune reddi |
| `GET` | `/api/v1/diagnostics/specimens/{id}` | `laboratory.worklist.view` | Kimliğe göre numune detayı ve devir geçmişi |
| `GET` | `/api/v1/diagnostics/specimens/by-barcode/{barcode}` | `laboratory.worklist.view` | Barkoda göre numune sorgulama |
| `GET` | `/api/v1/diagnostics/specimens` | `laboratory.worklist.view` | Numune iş listesi ve durum filtreleme |

---

## 5. Doğrulama ve Test Kapsamı

- **Birim Testleri (`SpecimenDomainTests.cs`):** Durum geçişleri, ilk toplama olayı, red gerekçesi ve geçersiz geçiş kısıtlamaları.
- **Bileşen Testleri (`SpecimenWorklistAndScannerComponentTests.cs`):** Barkod sorgulama, durum filtreleme ve devir-teslim zaman çizelgesi görselleştirmesi.
- **Entegrasyon Testleri (`SpecimenIntegrationTests.cs`):** PostgreSQL üzerinde istem -> toplama -> taşıma -> kabul -> red -> barkod sorgusu -> audit günlüğü doğrulaması.
