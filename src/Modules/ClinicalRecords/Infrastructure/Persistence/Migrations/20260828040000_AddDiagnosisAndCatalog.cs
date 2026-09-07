using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagement.Modules.ClinicalRecords.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddDiagnosisAndCatalog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "diagnosis_catalog_items",
            schema: "clinical_records",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                name_turkish = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                name_english = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                chapter = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                block = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                catalog_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_diagnosis_catalog_items", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "encounter_diagnoses",
            schema: "clinical_records",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                diagnosed_by_practitioner_id = table.Column<Guid>(type: "uuid", nullable: false),
                diagnosis_type = table.Column<int>(type: "integer", nullable: false),
                is_coded = table.Column<bool>(type: "boolean", nullable: false),
                icd10_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                diagnosis_title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                catalog_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                diagnosed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                is_entered_in_error = table.Column<bool>(type: "boolean", nullable: false),
                entered_in_error_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                version = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_encounter_diagnoses", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_diagnosis_catalog_items_name_turkish",
            schema: "clinical_records",
            table: "diagnosis_catalog_items",
            column: "name_turkish");

        migrationBuilder.CreateIndex(
            name: "ux_diagnosis_catalog_items_code",
            schema: "clinical_records",
            table: "diagnosis_catalog_items",
            column: "code",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_encounter_diagnoses_encounter_id",
            schema: "clinical_records",
            table: "encounter_diagnoses",
            column: "encounter_id");

        migrationBuilder.CreateIndex(
            name: "ix_encounter_diagnoses_icd10_code",
            schema: "clinical_records",
            table: "encounter_diagnoses",
            column: "icd10_code");

        migrationBuilder.CreateIndex(
            name: "ix_encounter_diagnoses_patient_id",
            schema: "clinical_records",
            table: "encounter_diagnoses",
            column: "patient_id");

        // Seed initial standard demo ICD-10 subset
        migrationBuilder.InsertData(
            schema: "clinical_records",
            table: "diagnosis_catalog_items",
            columns: new[] { "id", "code", "name_turkish", "name_english", "chapter", "block", "catalog_version", "is_active" },
            values: new object[,]
            {
                { Guid.Parse("00000000-0000-0000-0000-000000000701"), "I10", "Esansiyel (primer) hipertansiyon", "Essential (primary) hypertension", "IX - Dolaşım Sistemi Hastalıkları", "I10-I15 Hipertansif hastalıklar", "ICD-10-TR-2026.1", true },
                { Guid.Parse("00000000-0000-0000-0000-000000000702"), "E11.9", "Tip 2 diabetes mellitus, komplikasyonsuz", "Type 2 diabetes mellitus without complications", "IV - Endokrin, Beslenme ve Metabolizma Hastalıkları", "E10-E14 Diabetes mellitus", "ICD-10-TR-2026.1", true },
                { Guid.Parse("00000000-0000-0000-0000-000000000703"), "J06.9", "Akut üst solunum yolu enfeksiyonu, tanımlanmamış", "Acute upper respiratory infection, unspecified", "X - Solunum Sistemi Hastalıkları", "J00-J06 Akut üst solunum yolu enfeksiyonları", "ICD-10-TR-2026.1", true },
                { Guid.Parse("00000000-0000-0000-0000-000000000704"), "J03.9", "Akut tonsillit, tanımlanmamış", "Acute tonsillitis, unspecified", "X - Solunum Sistemi Hastalıkları", "J00-J06 Akut üst solunum yolu enfeksiyonları", "ICD-10-TR-2026.1", true },
                { Guid.Parse("00000000-0000-0000-0000-000000000705"), "J18.9", "Pnömoni, tanımlanmamış", "Pneumonia, unspecified", "X - Solunum Sistemi Hastalıkları", "J09-J18 Grip ve pnömoni", "ICD-10-TR-2026.1", true },
                { Guid.Parse("00000000-0000-0000-0000-000000000706"), "K21.9", "Gastro-özofajeal reflü hastalığı, özofajitsiz", "Gastro-esophageal reflux disease without esophagitis", "XI - Sindirim Sistemi Hastalıkları", "K20-K31 Yemek borusu, mide ve duodenum hastalıkları", "ICD-10-TR-2026.1", true },
                { Guid.Parse("00000000-0000-0000-0000-000000000707"), "M54.5", "Bel ağrısı (lumbago)", "Low back pain", "XIII - Kas-İskelet Sistemi ve Bağ Dokusu Hastalıkları", "M50-M54 Diğer dorsopatiler", "ICD-10-TR-2026.1", true },
                { Guid.Parse("00000000-0000-0000-0000-000000000708"), "R51", "Baş ağrısı", "Headache", "XVIII - Semptomlar, Bulgular ve Anormal Klinik Bulgular", "R50-R69 Genel semptomlar ve bulgular", "ICD-10-TR-2026.1", true },
                { Guid.Parse("00000000-0000-0000-0000-000000000709"), "N39.0", "İdrar yolu enfeksiyonu, yerleşimi belirtilmemiş", "Urinary tract infection, site not specified", "XIV - Genitoüriner Sistem Hastalıkları", "N30-N39 İdrar sistemi diğer hastalıkları", "ICD-10-TR-2026.1", true },
                { Guid.Parse("00000000-0000-0000-0000-000000000710"), "K52.9", "Enfeksiyöz olmayan gastroenterit ve kolit, tanımlanmamış", "Non-infective gastroenteritis and colitis, unspecified", "XI - Sindirim Sistemi Hastalıkları", "K50-K52 Enfeksiyöz olmayan enterit ve kolit", "ICD-10-TR-2026.1", true },
                { Guid.Parse("00000000-0000-0000-0000-000000000711"), "J45.9", "Astım, tanımlanmamış", "Asthma, unspecified", "X - Solunum Sistemi Hastalıkları", "J40-J47 Kronik alt solunum yolu hastalıkları", "ICD-10-TR-2026.1", true },
                { Guid.Parse("00000000-0000-0000-0000-000000000712"), "F41.1", "Yaygın anksiyete bozukluğu", "Generalized anxiety disorder", "V - Ruhsal ve Davranışsal Bozukluklar", "F40-F48 Nevrotik, stresle ilgili ve somatoform bozukluklar", "ICD-10-TR-2026.1", true },
                { Guid.Parse("00000000-0000-0000-0000-000000000713"), "F32.9", "Depresif nöbet, tanımlanmamış", "Depressive episode, unspecified", "V - Ruhsal ve Davranışsal Bozukluklar", "F30-F39 Duygudurum bozuklukları", "ICD-10-TR-2026.1", true },
                { Guid.Parse("00000000-0000-0000-0000-000000000714"), "E03.9", "Hipotiroidizm, tanımlanmamış", "Hypothyroidism, unspecified", "IV - Endokrin, Beslenme ve Metabolizma Hastalıkları", "E00-E07 Tiroid bezi bozuklukları", "ICD-10-TR-2026.1", true },
                { Guid.Parse("00000000-0000-0000-0000-000000000715"), "R05", "Öksürük", "Cough", "XVIII - Semptomlar, Bulgular ve Anormal Klinik Bulgular", "R00-R09 Dolaşım ve solunum sistemlerine ait semptom ve bulgular", "ICD-10-TR-2026.1", true },
                { Guid.Parse("00000000-0000-0000-0000-000000000716"), "R10.4", "Diğer ve tanımlanmamış karın ağrısı", "Other and unspecified abdominal pain", "XVIII - Semptomlar, Bulgular ve Anormal Klinik Bulgular", "R10-R19 Sindirim sistemi ve karına ait semptom ve bulgular", "ICD-10-TR-2026.1", true }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "diagnosis_catalog_items",
            schema: "clinical_records");

        migrationBuilder.DropTable(
            name: "encounter_diagnoses",
            schema: "clinical_records");
    }
}
