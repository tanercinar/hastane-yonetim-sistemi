$ErrorActionPreference = 'Stop'

$requiredFiles = @(
    'ROADMAP.md',
    'AGENTS.md',
    'CLAUDE.md',
    'UBIQUITOUS_LANGUAGE.md',
    '.cursor/rules/project-workflow.mdc',
    'docs/product/scope.md',
    'docs/product/user-stories.md',
    'docs/product/glossary.md',
    'docs/product/state-machines.md',
    'docs/security/authorization-matrix.md',
    'docs/security/threat-model.md',
    'docs/privacy/data-inventory.md',
    'docs/privacy/data-classification.md',
    'docs/privacy/retention-policy.md',
    'docs/testing/test-strategy.md',
    'docs/testing/phase-0-gate-review.md',
    'docs/adr/README.md',
    'docs/adr/ADR-0001-modular-monolith.md',
    'docs/adr/ADR-0002-api-first-blazor-clients.md',
    'docs/adr/ADR-0003-postgresql-ef-core.md',
    'docs/adr/ADR-0004-identity-and-native-oidc.md',
    'docs/adr/ADR-0005-clinical-record-integrity.md',
    'docs/adr/ADR-0006-synthetic-data-only.md'
)

$missingFiles = @($requiredFiles | Where-Object { -not (Test-Path -LiteralPath $_) })
if ($missingFiles.Count -gt 0) {
    throw "Eksik Faz 0 dosyalari: $($missingFiles -join ', ')"
}

$brokenLinks = [System.Collections.Generic.List[string]]::new()
$generatedDirectoryPattern = [regex]::new(
    '[\\/](?:bin|obj|artifacts|TestResults|node_modules|packages|coverage|playwright-report|test-results|\.git|\.nuget|\.dotnet|\.store)[\\/]',
    [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
$markdownFiles = @(
    Get-ChildItem -Recurse -File -Include '*.md', '*.mdc' |
        Where-Object { -not $generatedDirectoryPattern.IsMatch($_.FullName) }
)

foreach ($file in $markdownFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding utf8
    $matches = [regex]::Matches($content, '\[[^\]]+\]\(([^)]+)\)')

    foreach ($match in $matches) {
        $target = $match.Groups[1].Value
        if ($target -match '^(https?://|#|mailto:)') {
            continue
        }

        $targetPath = $target.Split('#')[0]
        if ([string]::IsNullOrWhiteSpace($targetPath)) {
            continue
        }

        $decodedTarget = [uri]::UnescapeDataString($targetPath)
        $resolvedTarget = Join-Path -Path $file.DirectoryName -ChildPath $decodedTarget
        if (-not (Test-Path -LiteralPath $resolvedTarget)) {
            $brokenLinks.Add("$($file.FullName) -> $target")
        }
    }
}

if ($brokenLinks.Count -gt 0) {
    throw "Bozuk yerel Markdown baglantilari:`n$($brokenLinks -join "`n")"
}

$requiredContent = @{
    'docs/product/scope.md' = @('Hasta', 'Doktor', 'Ecz', 'Kapsam')
    'docs/product/user-stories.md' = @('AC-E2E-01', 'AC-E2E-02', 'AC-E2E-03', 'AC-E2E-04', 'AC-SEC-01')
    'docs/product/glossary.md' = @('Randevu', 'Encounter', 'Referral/Transfer Out', 'Audit Event')
    'docs/product/state-machines.md' = @('Reserved', 'InProgress', 'PartiallyDispensed', 'Expected', 'Admitted')
    'docs/security/authorization-matrix.md' = @('Permission', 'Resource Scope', 'Care Relationship', 'IDOR')
    'docs/security/threat-model.md' = @('STRIDE', 'ASVS 5.0', 'TM-04', 'TM-10')
    'docs/privacy/data-inventory.md' = @('RET-IDENTITY', 'RET-CLINICAL', 'RET-AUDIT')
    'docs/privacy/data-classification.md' = @('C0', 'C1', 'C2', 'C3', 'C4')
    'docs/privacy/retention-policy.md' = @('TBD-LEGAL', 'RET-AUDIT', 'RET-EXPORT')
    'docs/testing/test-strategy.md' = @('Domain unit', 'Integration', 'EndToEnd', 'Playwright')
    'docs/testing/phase-0-gate-review.md' = @('senaryo 1', 'senaryo 2', 'senaryo 3', 'senaryo 4', '**Karar:**', 'Faz 1')
}

foreach ($entry in $requiredContent.GetEnumerator()) {
    $content = Get-Content -LiteralPath $entry.Key -Raw -Encoding utf8
    foreach ($needle in $entry.Value) {
        if (-not $content.Contains($needle)) {
            throw "Zorunlu icerik bulunamadi: '$needle' ($($entry.Key))"
        }
    }
}

$adrFiles = @(Get-ChildItem -LiteralPath 'docs/adr' -File -Filter 'ADR-*.md')
if ($adrFiles.Count -ne 6) {
    throw "Tam olarak 6 Faz 0 ADR bekleniyordu; bulunan: $($adrFiles.Count)"
}

$roadmap = Get-Content -LiteralPath 'ROADMAP.md' -Raw -Encoding utf8
$phaseZeroTaskIds = @(
    [regex]::Matches($roadmap, '(?m)^- \[[ x]\] \*\*(F00-(?:G\d{2}|KAPI))') |
        ForEach-Object { $_.Groups[1].Value }
)

if ($phaseZeroTaskIds.Count -ne 9 -or @($phaseZeroTaskIds | Sort-Object -Unique).Count -ne 9) {
    throw "Faz 0 icin 9 benzersiz gorev bekleniyordu."
}

$fenceIssues = [System.Collections.Generic.List[string]]::new()
foreach ($file in $markdownFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding utf8
    $fenceCount = [regex]::Matches($content, '(?m)^```').Count
    if (($fenceCount % 2) -ne 0) {
        $fenceIssues.Add($file.FullName)
    }
}

if ($fenceIssues.Count -gt 0) {
    throw "Kapanmamis kod blogu bulunan dosyalar: $($fenceIssues -join ', ')"
}

[pscustomobject]@{
    Status = 'PASS'
    RequiredFiles = $requiredFiles.Count
    MarkdownFiles = $markdownFiles.Count
    BrokenLocalLinks = $brokenLinks.Count
    ArchitectureDecisionRecords = $adrFiles.Count
    PhaseZeroTasks = $phaseZeroTaskIds.Count
}
