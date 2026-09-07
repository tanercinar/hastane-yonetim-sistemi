# Faz 12 Çoklu İstemci Kapısı (Phase 12 Multi-Client Gate)

## 1. Amaç

Bu kapı, Hastane Yönetim Sistemi projesinde **Faz 12 — Windows ve Android istemcileri** kapsamında geliştirilen yerel istemcilerin, paylaşılan Razor Class Library bileşenlerinin, API güvenlik katmanının, çevrimdışı davranışın ve platform paketleme süreçlerinin belirlenen kabul ölçütlerini eksiksiz karşıladığını belgeler ve kanıtlar.

## 2. Kapı Sözleşmesi ve Temel Prensipler

1. **Sunucu Tek Doğruluk Kaynağıdır (Single Source of Truth)**:
   - Web, Windows ve Android istemcileri aynı API uç noktalarına ve PostgreSQL veri tabanına bağlanır.
   - İstemcilerin hiçbiri iş kuralı veya yetkilendirme kararı vermez; tüm doğrulamalar (`Permission + Resource Scope + Care Relationship`) sunucu tarafında uygulanır.
2. **Sıfır Çevrimdışı Kuyruk (Zero Offline Write Queuing)**:
   - Klinik güvenlik ve hasta sağlığı gereğince, ağ kesintisi sırasında yerel yazma kuyruğu tutulmaz.
   - Ağ bağlantısı koptuğunda hem Windows hem Android istemcilerinde açık ve erişilebilir bir "Bağlantı Gerekli" bariyeri (`ConnectionRequiredState`) gösterilir.
3. **Standart OIDC ve PKCE Güvenliği (No Homemade JWT)**:
   - RFC 7636 (PKCE S256) ve RFC 8252 (OAuth 2.0 for Native Apps) sistem tarayıcısı kullanılır.
   - 15 dakikalık kısa ömürlü access token'lar ve Refresh Token Rotation (RTR) uygulanır; reuse detection ile token family iptali sağlanır.
   - Web çerez tabanlı (`__Host-HospitalManagement.Auth`) akış %100 korunmuştur.
4. **Korumalı Sağlık Verisi (PHI) Filtresi**:
   - İşletim sistemi bildirimlerinde veya kilit ekranı önizlemelerinde hastanın teşhisi, ilaç isimleri, dozajları veya tetkik değerleri asla gösterilmez.
   - Bildirimler generic güvenli şablonlara indirgenir; klinik detaylar yalnızca yetkili oturum açıldıktan sonra uygulama içinde görüntülenir.
5. **Güvenli Deep-Link Yönlendirmesi**:
   - `hospitalapp://` özel şeması katı allow-list, dizin geçişi (`..`, `\`, `//`) ve alfasayısal parametre doğrulamasından geçirilir.
   - Hasta rolü personel çalışma alanına erişemez (`/forbidden`).
6. **Repoda Sıfır Sertifika ve Keystore İlkesi**:
   - Kaynak kod deposunda hiçbir özel anahtar (`.key`, `.snk`), sertifika (`.pfx`, `.p12`) veya keystore (`.keystore`, `.jks`) bulunmaz.
   - `tools/verify-platform-packaging.ps1` ile otomatik doğrulanır.

## 3. Otomatik Test Kanıtı

- **Bileşen ve Çoklu İstemci Kapı Testleri**: `tests/HospitalManagement.ComponentTests`
  - 147 testin tamamı başarılı (0 fail, 0 skip).
  - `Phase12MultiClientGateTests`:
    - `GateCrossClientOfflineInterruptionBlocksLocalQueuingAndShowsBarrier` (PASS)
    - `GateTokenExpirationAndRtrTransparentlyRefreshesAndRetriesSafeRequests` (PASS)
    - `GateSafeRetryHandlerRefusesToRetryNonIdempotentPostRequests` (PASS)
    - `GateCrossPlatformAuthorizationRejectsPrivilegeEscalationOnBothClients` (PASS)
    - `GatePhiLockScreenSanitizationGuaranteesZeroClinicalDataInPreviews` (PASS)
- **Birim Testleri**: `tests/HospitalManagement.UnitTests`
  - 423 testin tamamı başarılı (0 fail, 0 skip).
  - RFC 7636 Appendix B test vektörleri dahil PKCE doğrulaması.
- **Mimari Testleri**: `tests/HospitalManagement.ArchitectureTests`
  - 13 testin tamamı başarılı (0 fail, 0 skip).
- **Platform Derleme Doğrulaması**:
  - Windows (`net10.0-windows10.0.19041.0`): 0 hata, 0 uyarı.
  - Android (`net10.0-android`): 0 hata, 0 uyarı.
  - `verify-platform-packaging.ps1`: PASS (0 sızıntı).
- **Biçimlendirme**: `dotnet format --verify-no-changes` PASS.
- **Kök Belge Bağlantıları**: `tools/validate-phase0.ps1` PASS.

## 4. Manuel Kabul

Tekrarlanabilir adımlar ve test matrisi repository kökündeki [`F12_Test.md`](../../F12_Test.md) belgesinde yer almaktadır.
