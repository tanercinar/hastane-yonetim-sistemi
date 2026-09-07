# Yerel altyapı

Bu geliştirme ortamı PostgreSQL 18, MinIO ve yalnızca e-posta yakalamak için kullanılan Mailpit servislerini sağlar. Tamamı yereldir; gerçek hasta verisi, kurum bağlantısı veya gerçek e-posta teslimatı için kullanılamaz.

## Ön koşullar

- Çalışır durumda Docker Desktop ve `docker compose`
- Windows PowerShell 5.1 veya PowerShell 7+
- İlk kurulumda imajları ve MinIO kaynak bağımlılıklarını indirmek için internet erişimi

## Tek komutla başlatma

Repo kökünde çalıştır:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\start-local-infrastructure.ps1
```

Komut ilk çalışmada `.gitignore` kapsamındaki `.env` dosyasını kriptografik olarak rastgele parolalarla üretir. Mevcut `.env` dosyasını ve parolaları sonraki çalışmalarda değiştirmez. Ardından Compose yapılandırmasını doğrular, imajları kurar/indirir, üç servisin de sağlıklı olmasını bekler ve otomatik smoke testlerini çalıştırır. İlk MinIO kaynak derlemesi birkaç dakika sürebilir.

Host uygulamasını ilk kez çalıştırmadan önce `.env` içindeki yerel kimlikleri Development user-secrets deposuna aktar. Ayrıntılar için [`configuration-and-secrets.md`](configuration-and-secrets.md) belgesini izle.

| Servis | Yerel adres | Amaç |
|---|---|---|
| PostgreSQL 18.6 | `127.0.0.1:5432` | İlişkisel geliştirme veritabanı |
| MinIO API | `http://127.0.0.1:9000` | S3 uyumlu nesne depolama simülasyonu |
| MinIO Console | `http://127.0.0.1:9001` | Yerel nesne depolama arayüzü |
| Mailpit SMTP | `127.0.0.1:1025` | `MOCK` e-posta yakalama |
| Mailpit UI | `http://127.0.0.1:8025` | Yakalanan sentetik e-postaları inceleme |

Portlar `.env` içinden değiştirilebilir. MinIO Console oturumu için kullanıcı adı ve parola da yalnız bu dosyadadır.

## Doğrulama ve günlük kullanım

Servisleri yeniden test et:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\test-local-infrastructure.ps1
```

Test; boş örnek parolaların reddedildiğini, Compose modelinin geçerli olduğunu, PostgreSQL sorgusunu, MinIO ve Mailpit health endpoint'lerini ve Mailpit'in `.invalid` alan adına gönderilen sentetik bir `DEMO` e-postayı yakaladığını doğrular.

Durumu veya logları incele:

```powershell
docker compose --env-file .env ps
docker compose --env-file .env logs --follow
```

Servisleri durdur; kalıcı geliştirme verileri korunur:

```powershell
docker compose --env-file .env down
```

Tüm yerel geliştirme verilerini sıfırlamak için aşağıdaki komut adlandırılmış volume'ları kalıcı olarak siler. Yalnız sentetik veride kullan:

```powershell
docker compose --env-file .env down --volumes
```

## Güvenlik sınırları

- Tüm host portları yalnız `127.0.0.1` üzerinde dinler; yerel ağa açılmaz.
- Mailpit'te otomatik sürüm kontrolü, relay ve forwarding etkin değildir. Bu servis yalnız `MOCK` yakalayıcıdır; gerçek alıcılara e-posta göndermez.
- Standart bridge ağı, Docker Desktop'ın loopback portlarını yayımlayabilmesi için kullanılır. Servislere gerçek entegrasyon endpoint'i veya credential'ı verilmez.
- `.env.example` bilerek boş parola alanları içerir ve Compose bunları reddeder. Gerçek yerel değerler ignore edilen `.env` içinde kalır; terminale veya dokümana yazdırılmaz.
- Mailpit ve MinIO arayüzleri yalnız loopback sınırı nedeniyle yerelde parolasız/parolalı geliştirme kolaylığı sağlar. Bir proxy ile yayımlanmamalıdır.
- Bu ortamda yalnız `DEMO`/sentetik veri kullanılmalıdır.

## Sürüm notu

PostgreSQL için güncel 18.x güvenlik sürümü olan `18.6`, Mailpit için `v1.31.0` sabitlenmiştir. PostgreSQL 18+ imajında veri volume hedefi resmî değişikliğe uygun olarak `/var/lib/postgresql` kullanır.

MinIO Community Edition dağıtımı 2025 sonunda source-only modele geçti. Son hazır topluluk imajı, daha sonra yayımlanan yetki yükseltme düzeltmesini içermediğinden kullanılmaz. `infrastructure/minio/Dockerfile`, son resmî güvenlik etiketi `RELEASE.2025-10-15T17-29-55Z` kaynak kodunu yerelde derler. Bu kurulum yalnız eğitim ve geliştirme içindir; üretim desteği iddiası taşımaz.

Kaynaklar: [PostgreSQL 18.6 sürüm notu](https://www.postgresql.org/docs/release/18.6/), [PostgreSQL resmî Docker imajı](https://hub.docker.com/_/postgres), [MinIO source-only duyurusu](https://github.com/minio/minio#source-only-distribution), [MinIO son güvenlik sürümü](https://github.com/minio/minio/releases/tag/RELEASE.2025-10-15T17-29-55Z), [Mailpit Docker kurulumu](https://mailpit.axllent.org/docs/install/docker/), [Mailpit health check'leri](https://mailpit.axllent.org/docs/integration/healthcheck/).
