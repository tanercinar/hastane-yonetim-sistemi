namespace HospitalManagement.Modules.Inpatient.Domain;

public enum IsolationType
{
    None = 1,
    Contact = 2,       // Temas İzolasyonu
    Droplet = 3,       // Damlacık İzolasyonu
    Airborne = 4,      // Solunum / Hava Yolu İzolasyonu
    Protective = 5,    // Koruyucu İzolasyon (Nötropenik / İmmünsüpresif)
}
