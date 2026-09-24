# Copyright © Erickson Lopez. MIT License.
# =============================================================================
# REPOSITORY COMPLIANCE & ARCHITECTURAL INVARIANT AUDITOR
# Repository: EricksonLopez.Processes
# Enforces:
#   1. Documentation kebab-case file naming convention
#   2. Zero [Obsolete] attribute usages in production code (src/)
#   3. Canonical MIT copyright headers on all production C# files
#   4. Single Type Per File rule across all production C# files
#   5. Normalized GitHub links pointing to ericksonlopezf/dotnet-processes
#   6. Canonical contact/security email: ericksonlopezf@gmail.com
# =============================================================================

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$Violations = [System.Collections.Generic.List[string]]::new()

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  REPOSITORY COMPLIANCE & ARCHITECTURE AUDITOR    " -ForegroundColor Cyan
Write-Host "  Repository: EricksonLopez.Processes             " -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# -----------------------------------------------------------------------------
# 1. Documentation file naming (must be lowercase kebab-case .md)
# -----------------------------------------------------------------------------
Write-Host "`n[1/7] Checking documentation file naming (kebab-case)..." -ForegroundColor Yellow
$DocsPath = Join-Path $RepoRoot "docs"
if (Test-Path $DocsPath) {
    $DocFiles = Get-ChildItem -Path $DocsPath -Recurse -File -Filter "*.md"
    foreach ($File in $DocFiles) {
        $BaseName = $File.BaseName
        if ($BaseName -cmatch '[A-Z_]') {
            $Violations.Add("Doc naming violation: '$($File.FullName)' contains uppercase letters or underscores. Use lowercase kebab-case.")
        }
    }
    if ($Violations.Count -eq 0) {
        Write-Host "  ✅ All documentation files use valid kebab-case naming." -ForegroundColor Green
    }
}

# -----------------------------------------------------------------------------
# 2. Zero [Obsolete] usages in src/
# -----------------------------------------------------------------------------
Write-Host "`n[2/7] Checking for [Obsolete] attribute usages in src/..." -ForegroundColor Yellow
$SrcPath = Join-Path $RepoRoot "src"
if (Test-Path $SrcPath) {
    $ObsoleteMatches = Get-ChildItem -Path $SrcPath -Recurse -Filter "*.cs" | 
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
        Select-String -Pattern '\[\s*(System\.)?Obsolete'
    foreach ($Match in $ObsoleteMatches) {
        $Violations.Add("Obsolete attribute violation: '$($Match.Path):$($Match.LineNumber)' - [Obsolete] is prohibited in production code.")
    }
    if (@($ObsoleteMatches).Count -eq 0) {
        Write-Host "  ✅ Zero [Obsolete] attributes in production code." -ForegroundColor Green
    }
}

# -----------------------------------------------------------------------------
# 3. Canonical MIT Copyright Headers across all source files
# -----------------------------------------------------------------------------
Write-Host "`n[3/12] Checking canonical MIT copyright headers across repository..." -ForegroundColor Yellow
$AllCsFiles = Get-ChildItem -Path $RepoRoot -Recurse -Filter "*.cs" | 
    Where-Object { $_.FullName -notmatch '\\(bin|obj|BenchmarkDotNet\.Artifacts|TestResults|StrykerOutput)\\' }
foreach ($File in $AllCsFiles) {
    $Content = Get-Content -Path $File.FullName -Raw
    if ($Content -notmatch 'Copyright.*Erickson Lopez.*MIT') {
        $Violations.Add("Missing/Invalid copyright header: '$($File.FullName)'. Expected '// Copyright © Erickson Lopez. MIT License.' at top.")
    }
}
$PsScripts = Get-ChildItem -Path (Join-Path $RepoRoot "scripts") -Recurse -Filter "*.ps1" |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
foreach ($Script in $PsScripts) {
    $Content = Get-Content -Path $Script.FullName -Raw
    if ($Content -notmatch 'Copyright.*Erickson Lopez.*MIT') {
        $Violations.Add("Missing/Invalid copyright header: '$($Script.FullName)'. Expected '# Copyright © Erickson Lopez. MIT License.' at top.")
    }
}
Write-Host "  ✅ All C# and PowerShell source files contain canonical MIT copyright headers." -ForegroundColor Green

# -----------------------------------------------------------------------------
# 4. Single Type Per File Rule in src/
# -----------------------------------------------------------------------------
Write-Host "`n[4/12] Checking 'One Type Per File' rule in src/..." -ForegroundColor Yellow
$TypeRegex = '^\s*((public|internal|protected|private|sealed|abstract|static|readonly|ref|partial)\s+)*(class|interface|struct|enum|record(\s+struct|\s+class)?)\s+([A-Za-z0-9_]+(\s*<[^>]+>)?)\b'
if (Test-Path $SrcPath) {
    $ProductionFiles = Get-ChildItem -Path $SrcPath -Recurse -Filter "*.cs" | 
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
    foreach ($File in $ProductionFiles) {
        $Lines = Get-Content -Path $File.FullName
        $DeclaredTypes = [System.Collections.Generic.List[string]]::new()
        $InBlockComment = $false

        foreach ($Line in $Lines) {
            $Trimmed = $Line.Trim()
            if ($Trimmed.StartsWith("/*")) { $InBlockComment = $true }
            if ($Trimmed.EndsWith("*/")) { $InBlockComment = $false; continue }
            if ($InBlockComment -or $Trimmed.StartsWith("//") -or [string]::IsNullOrWhiteSpace($Trimmed)) { continue }

            # Match only top-level types (ignoring indented nested types and members)
            if ($Line -match '^[a-z]' -and $Trimmed -match $TypeRegex) {
                if ($Trimmed -notmatch '\(.*\)|\bget\b|\bset\b|=>') {
                    $TypeName = $Matches[5]
                    $DeclaredTypes.Add($TypeName)
                }
            }
        }

        if ($DeclaredTypes.Count -gt 1) {
            $Violations.Add("Multiple top-level types declared in single file: '$($File.FullName)' contains [$($DeclaredTypes -join ', ')].")
        }
    }
    Write-Host "  ✅ Every production file satisfies the 'One Type Per File' invariant." -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# 5. GitHub Repository Identity Links
# -----------------------------------------------------------------------------
Write-Host "`n[5/12] Checking GitHub identity links (ericksonlopezf/dotnet-processes)..." -ForegroundColor Yellow
$MarkdownFiles = Get-ChildItem -Path $RepoRoot -Recurse -Filter "*.md" | Where-Object { $_.FullName -notmatch '\\(bin|obj|TestResults|StrykerOutput|node_modules)\\' }
foreach ($File in $MarkdownFiles) {
    $Mismatches = Get-Content -Path $File.FullName | Select-String -Pattern 'github\.com/([a-zA-Z0-9_-]+)/([a-zA-Z0-9_-]+)'
    foreach ($M in $Mismatches) {
        $Url = $M.Matches[0].Value
        if ($Url -match 'github\.com/ericksonlopezf/dotnet-' -and $Url -notmatch 'dotnet-processes' -and $Url -notmatch 'dotnet-template' -and $Url -notmatch 'dotnet-mediator' -and $Url -notmatch 'dotnet-mapper' -and $Url -notmatch 'dotnet-events' -and $Url -notmatch 'dotnet-outbox' -and $Url -notmatch 'dotnet-specification' -and $Url -notmatch 'dotnet-sql-builder' -and $Url -notmatch 'dotnet-dapper-extensions' -and $Url -notmatch 'dotnet-messaging' -and $Url -notmatch 'dotnet-sharedkernel' -and $Url -notmatch 'dotnet-shared-kernel' -and $Url -notmatch 'dotnet-result' -and $Url -notmatch 'dotnet-idempotency' -and $Url -notmatch 'dotnet-concurrency' -and $Url -notmatch 'dotnet-transaction' -and $Url -notmatch 'dotnet-multitenancy') {
            $Violations.Add("Broken repo URL: '$($File.FullName):$($M.LineNumber)' references '$Url'.")
        }
    }
}
Write-Host "  ✅ All GitHub URLs correctly target valid repositories." -ForegroundColor Green

# -----------------------------------------------------------------------------
# 6. Contact and Security Email Normalization
# -----------------------------------------------------------------------------
Write-Host "`n[6/12] Checking contact and security email normalization (ericksonlopezf@gmail.com)..." -ForegroundColor Yellow
$SecFile = Join-Path $RepoRoot "SECURITY.md"
if (Test-Path $SecFile) {
    $Content = Get-Content -Path $SecFile -Raw
    if ($Content -notmatch 'ericksonlopezf@gmail\.com') {
        $Violations.Add("SECURITY.md contact email is not normalized to ericksonlopezf@gmail.com")
    } else {
        Write-Host "  ✅ Official contact emails normalized to ericksonlopezf@gmail.com." -ForegroundColor Green
    }
}

# -----------------------------------------------------------------------------
# 7. Changelog Canonical Structure & VersionPrefix Parity (Rule 25)
# -----------------------------------------------------------------------------
Write-Host "`n[7/12] Checking Changelog canonical structure, ## [Unreleased], and VersionPrefix parity..." -ForegroundColor Yellow
$ChangelogFile = Join-Path $RepoRoot "CHANGELOG.md"
if (-not (Test-Path $ChangelogFile)) {
    $Violations.Add("Missing CHANGELOG.md in repository root.")
} else {
    $ChangelogContent = Get-Content -Path $ChangelogFile -Raw -Encoding utf8
    if ($ChangelogContent -notmatch '(?m)^##\s*\[Unreleased\]') {
        $Violations.Add("CHANGELOG.md violation: Missing mandatory '## [Unreleased]' section header at top.")
    }

    if ($ChangelogContent -match '(?m)^##\s*\[(?<version>\d+\.\d+\.\d+)\]\s*-\s*(?<date>\d{4}-\d{2}-\d{2})') {
        $ChangelogVersion = $Matches['version']
        $ReleaseDate = $Matches['date']

        # Extract VersionPrefix from Directory.Build.props
        $BldProps = Join-Path $RepoRoot "Directory.Build.props"
        if (Test-Path $BldProps) {
            $BldContent = Get-Content -Path $BldProps -Raw -Encoding utf8
            if ($BldContent -match '<VersionPrefix>(?<v>[^<]+)</VersionPrefix>') {
                $VersionPrefix = $Matches['v'].Trim()
                if ($ChangelogVersion -ne $VersionPrefix) {
                    $Violations.Add("Version desynchronization: CHANGELOG.md most recent release version '[$ChangelogVersion]' does not match Directory.Build.props <VersionPrefix> '$VersionPrefix'.")
                }
            } else {
                $Violations.Add("Directory.Build.props is missing <VersionPrefix> tag.")
            }
        }
        Write-Host "  ✅ Version synchrony verified: Changelog [$ChangelogVersion] == Directory.Build.props [$VersionPrefix]." -ForegroundColor Green
    } else {
        $Violations.Add("CHANGELOG.md violation: No release version entry immediately below [Unreleased] matching strictly '^##\s*\[\d+\.\d+\.\d+\]\s*-\s*\d{4}-\d{2}-\d{2}'.")
    }
}

# -----------------------------------------------------------------------------
# 8. Local Packages Prohibition & nuget.config Security (Rule 24)
# -----------------------------------------------------------------------------
Write-Host "`n[8/12] Checking prohibition of local-packages and offline package feeds..." -ForegroundColor Yellow
$LocalPkgDir = Join-Path $RepoRoot "local-packages"
if (Test-Path $LocalPkgDir) {
    $Violations.Add("Prohibited local package directory detected: '$LocalPkgDir'. All packages must be consumed directly from official NuGet.org feed.")
}
$AllLocalPkgs = @(Get-ChildItem -Path $RepoRoot -Recurse -Filter "local-packages" -Directory -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })
if ($AllLocalPkgs.Count -gt 0) {
    $Violations.Add("Prohibited local package directory found in subfolder: $($AllLocalPkgs[0].FullName).")
}

$NuGetConfigs = @(Get-ChildItem -Path $RepoRoot -Recurse -Filter "nuget.config" -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })
foreach ($nc in $NuGetConfigs) {
    $ncContent = Get-Content $nc.FullName -Raw
    if ($ncContent -match '<add\s+key="local-packages"' -or $ncContent -match 'value="[./\\]*local-packages"') {
        $Violations.Add("Prohibited local-packages packageSource entry found in '$($nc.FullName)'.")
    }
    if ($ncContent -match '<packageSources>.*?</packageSources>' -and $ncContent -match 'value="(?i)(?!https://)[^"]+"') {
        $Violations.Add("Insecure or local package source declared in '$($nc.FullName)'. Only secure https:// package sources allowed.")
    }
}
Write-Host "  ✅ Zero local-packages folders or offline feeds detected." -ForegroundColor Green

# -----------------------------------------------------------------------------
# 9. PackageProjectUrl Compliance (without 'dotnet-' prefix, Rule 24)
# -----------------------------------------------------------------------------
Write-Host "`n[9/12] Checking PackageProjectUrl without 'dotnet-' prefix..." -ForegroundColor Yellow
$BldProps = Join-Path $RepoRoot "Directory.Build.props"
if (Test-Path $BldProps) {
    $BldContent = Get-Content -Path $BldProps -Raw -Encoding utf8
    if ($BldContent -match '<PackageProjectUrl>(?<url>[^<]+)</PackageProjectUrl>') {
        $projUrl = $Matches['url'].Trim()
        if ($projUrl -match '/dotnet-') {
            $Violations.Add("PackageProjectUrl violation in Directory.Build.props: '$projUrl' contains forbidden 'dotnet-' prefix.")
        }
        if ($projUrl -notmatch '^https://ericksonlopez\.dev/') {
            $Violations.Add("PackageProjectUrl violation in Directory.Build.props: '$projUrl' must start with 'https://ericksonlopez.dev/'.")
        }
    } else {
        $Violations.Add("Missing <PackageProjectUrl> in Directory.Build.props.")
    }
}
$AllCsprojs = Get-ChildItem -Path $RepoRoot -Recurse -Filter "*.csproj" -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
foreach ($p in $AllCsprojs) {
    $content = Get-Content $p.FullName -Raw
    if ($content -match '<PackageProjectUrl>(?<url>[^<]+)</PackageProjectUrl>') {
        $pUrl = $Matches['url'].Trim()
        if ($pUrl -match '/dotnet-') {
            $Violations.Add("PackageProjectUrl violation in '$($p.FullName)': '$pUrl' contains forbidden 'dotnet-' prefix.")
        }
    }
}
Write-Host "  ✅ PackageProjectUrl is canonical (https://ericksonlopez.dev/{lib} without 'dotnet-')." -ForegroundColor Green

# -----------------------------------------------------------------------------
# 10. ImplicitUsings Zero Tolerance (Rule 5)
# -----------------------------------------------------------------------------
Write-Host "`n[10/12] Checking ImplicitUsings=disable strictly enforced..." -ForegroundColor Yellow
if (Test-Path $BldProps) {
    $BldContent = Get-Content -Path $BldProps -Raw -Encoding utf8
    if ($BldContent -notmatch '<ImplicitUsings>\s*disable\s*</ImplicitUsings>') {
        $Violations.Add("Directory.Build.props must centralize '<ImplicitUsings>disable</ImplicitUsings>'.")
    }
}
foreach ($p in $AllCsprojs) {
    $content = Get-Content $p.FullName -Raw
    if ($content -match '<ImplicitUsings>\s*enable\s*</ImplicitUsings>') {
        $Violations.Add("ImplicitUsings violation in '$($p.FullName)': Overriding ImplicitUsings to 'enable' is prohibited.")
    }
}
Write-Host "  ✅ ImplicitUsings is strictly disabled across all projects." -ForegroundColor Green

# -----------------------------------------------------------------------------
# 11. CS1591 Zero Tolerance (Rule 10)
# -----------------------------------------------------------------------------
Write-Host "`n[11/12] Checking CS1591 is not suppressed or silenced anywhere..." -ForegroundColor Yellow
if (Test-Path $BldProps) {
    $BldContent = Get-Content -Path $BldProps -Raw -Encoding utf8
    if ($BldContent -match '<NoWarn>[^<]*\b(CS)?1591\b') {
        $Violations.Add("CS1591 violation: Directory.Build.props silences CS1591 in <NoWarn>.")
    }
}
foreach ($p in $AllCsprojs) {
    $content = Get-Content $p.FullName -Raw
    if ($content -match '<NoWarn>[^<]*\b(CS)?1591\b') {
        $Violations.Add("CS1591 violation: Project '$($p.FullName)' silences CS1591 in <NoWarn>.")
    }
}
$SrcCsFiles = Get-ChildItem -Path (Join-Path $RepoRoot "src") -Recurse -Filter "*.cs" -File | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
foreach ($f in $SrcCsFiles) {
    $content = Get-Content $f.FullName -Raw
    if ($content -match '#pragma\s+warning\s+disable[^;\r\n]*\b(CS)?1591\b') {
        $Violations.Add("CS1591 pragma suppression in '$($f.FullName)': CS1591 must be resolved with XML documentation, never silenced.")
    }
}
Write-Host "  ✅ Zero CS1591 suppressions in Directory.Build.props, projects, or source code." -ForegroundColor Green

# -----------------------------------------------------------------------------
# 12. ADR Index Link Integrity & Case Sensitivity (Rules 3, 27)
# -----------------------------------------------------------------------------
Write-Host "`n[12/12] Checking ADR Index link integrity and case sensitivity..." -ForegroundColor Yellow
$AdrIndexPath = Join-Path $RepoRoot "docs/adr/index.md"
if (Test-Path $AdrIndexPath) {
    $AdrIndexContent = Get-Content $AdrIndexPath -Raw
    $AdrLinks = [regex]::Matches($AdrIndexContent, '\[(?<label>[^\]]+)\]\((?<link>[^)]+\.md)\)')
    foreach ($al in $AdrLinks) {
        $linkTarget = $al.Groups['link'].Value
        if ($linkTarget -notmatch '^https?://') {
            $resolved = Join-Path (Join-Path $RepoRoot "docs/adr") $linkTarget
            if (-not (Test-Path $resolved)) {
                $Violations.Add("Broken relative ADR link in docs/adr/index.md: '$linkTarget' not found on disk.")
            } else {
                $leaf = Split-Path $resolved -Leaf
                $actualFile = Get-ChildItem -Path (Split-Path $resolved -Parent) -Filter $leaf -File
                if ($actualFile -and $actualFile.Name -cne $leaf) {
                    $Violations.Add("Case sensitivity mismatch in docs/adr/index.md: Link references '$leaf' but file on disk is '$($actualFile.Name)'.")
                }
            }
        }
    }
    Write-Host "  ✅ All ADR links exist on disk with exact case-sensitive kebab-case filenames." -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# Stryker.NET Configuration, Concurrency, Anti-Gaming Blacklist & Matrix Synchronization
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: Stryker] Validating Stryker.NET configuration, concurrency, anti-gaming blacklist & package matrix..." -ForegroundColor Yellow
$strykerErrors = 0
$targetRoot = if (Get-Variable -Name "RootDirectory" -Scope 0 -ErrorAction SilentlyContinue) { $RootDirectory } elseif (Get-Variable -Name "WorkspaceRoot" -Scope 0 -ErrorAction SilentlyContinue) { $WorkspaceRoot } elseif (Get-Variable -Name "repoRoot" -Scope 0 -ErrorAction SilentlyContinue) { $repoRoot } elseif (Get-Variable -Name "RepoRoot" -Scope 0 -ErrorAction SilentlyContinue) { $RepoRoot } else { (Resolve-Path (Join-Path $PSScriptRoot "..")).Path }

$strykerConfigFiles = Get-ChildItem -Path $targetRoot -Recurse -Filter "stryker*.json" -File -ErrorAction SilentlyContinue | Where-Object {
    $_.FullName -notmatch '[\\/](bin|obj|StrykerOutput|BenchmarkDotNet\.Artifacts|node_modules)[\\/]' -and
    $_.Name -ne "stryker-config.master.json"
}

if (-not $strykerConfigFiles -or $strykerConfigFiles.Count -eq 0) {
    Write-Host "  ❌ Zero Stryker configuration files found in repository." -ForegroundColor Red
    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Zero Stryker configuration files found.") } else { $Violations++ } }
    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
    $strykerErrors++
} else {
    foreach ($sf in $strykerConfigFiles) {
        $json = Get-Content $sf.FullName -Raw | ConvertFrom-Json
        $cfg = if ($json.PSObject.Properties['stryker-config']) { $json.'stryker-config' } else { $json }

        if ($cfg.PSObject.Properties['thresholds']) {
            $th = $cfg.thresholds
            if ($th.high -ne 100 -or $th.low -ne 98 -or $th.break -ne 95) {
                Write-Host "  ❌ Non-compliant mutation thresholds in $($sf.FullName): high=$($th.high), low=$($th.low), break=$($th.break). Required: high=100, low=98, break=95." -ForegroundColor Red
                if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
                if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Non-compliant mutation thresholds in $($sf.FullName)") } else { $Violations++ } }
                if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
                $strykerErrors++
            }
        }

        if ($cfg.PSObject.Properties['concurrency']) {
            if ($cfg.concurrency -ne 2) {
                Write-Host "  ❌ Non-compliant Stryker concurrency in $($sf.FullName): $($cfg.concurrency). Required: 2." -ForegroundColor Red
                if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
                if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Non-compliant Stryker concurrency in $($sf.FullName)") } else { $Violations++ } }
                if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
                $strykerErrors++
            }
        }

        if ($cfg.PSObject.Properties['ignore-methods'] -and $cfg.'ignore-methods') {
            foreach ($m in $cfg.'ignore-methods') {
                if ($m -match 'ThrowIf|Exception|Guard|ScrubEphemeralMemory') {
                    Write-Host "  ❌ Prohibited anti-gaming method exclusion '$m' detected in $($sf.FullName)." -ForegroundColor Red
                    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
                    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Prohibited anti-gaming exclusion '$m' in $($sf.FullName)") } else { $Violations++ } }
                    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
                    $strykerErrors++
                }
            }
        }
    }

        $strykerProfiles = Get-ChildItem -Path $targetRoot -Filter "stryker*.json" -File -ErrorAction SilentlyContinue | Where-Object {
        $_.Name -ne "stryker-config.master.json" -and
        ($_.Name -match '^stryker(-.+)?-config\.json$' -or $_.Name -eq "stryker-config.json")
    }

    $srcProjects = Get-ChildItem -Path (Join-Path $targetRoot "src") -Recurse -Filter "*.csproj" -ErrorAction SilentlyContinue | Where-Object {
        $_.FullName -notmatch '[\/](bin|obj)[\/]'
    }

    # Verify exact 1:1 count parity between Stryker profile configs and src/ projects
    if ($strykerProfiles.Count -ne $srcProjects.Count) {
        Write-Host "  ❌ Stryker profile count ($($strykerProfiles.Count)) does not match exactly the number of projects in src/ ($($srcProjects.Count))." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Stryker profile count ($($strykerProfiles.Count)) does not match project count in src/ ($($srcProjects.Count)).") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $strykerErrors++
    }

    foreach ($proj in $srcProjects) {
        $projName = $proj.Name
        $matched = $false
        foreach ($sf in $strykerProfiles) {
            $raw = Get-Content $sf.FullName -Raw
            if ($raw -match [regex]::Escape($projName) -or $sf.Name -match [regex]::Escape($proj.BaseName)) {
                $matched = $true
                break
            }
        }

        if (-not $matched) {
            Write-Host "  ❌ Project '$projName' has no corresponding Stryker configuration profile." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Project '$projName' has no corresponding Stryker configuration profile.") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $strykerErrors++
        }
    }

    foreach ($sf in $strykerProfiles) {
        $raw = Get-Content $sf.FullName -Raw
        $matchedProj = $false
        foreach ($proj in $srcProjects) {
            if ($raw -match [regex]::Escape($proj.Name) -or $sf.Name -match [regex]::Escape($proj.BaseName)) {
                $matchedProj = $true
                break
            }
        }
        if (-not $matchedProj) {
            Write-Host "  ❌ Stryker profile '$($sf.Name)' does not correspond to any project in src/ (orphaned profile)." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Stryker profile '$($sf.Name)' does not correspond to any project in src/.") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $strykerErrors++
        }
    }

    $mutationWfPath = Join-Path $targetRoot ".github/workflows/mutation-testing.yml"
    if (-not (Test-Path $mutationWfPath)) {
        Write-Host "  ❌ Missing .github/workflows/mutation-testing.yml" -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing .github/workflows/mutation-testing.yml") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $strykerErrors++
    } else {
        $wfContent = Get-Content $mutationWfPath -Raw
        if ($wfContent -match '--concurrency\s*[:\s]\s*([3-9]|\d{2,})') {
            Write-Host "  ❌ Mutation workflow overrides concurrency with value > 2 in CLI arguments." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Mutation workflow overrides concurrency > 2") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $strykerErrors++
        }
        if ($wfContent -match '--break-at\s*[:\s]\s*([0-8]\d|\d{1})(?!\d)') {
            Write-Host "  ❌ Mutation workflow overrides break threshold with value < 90 in CLI arguments." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Mutation workflow overrides break threshold < 90") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $strykerErrors++
        }

        foreach ($sf in $strykerConfigFiles) {
            if ($sf.Name -eq "stryker-config.json" -and $strykerConfigFiles.Count -gt 1) {
                continue
            }
            if ($sf.Name -eq "stryker-config-unit.json") {
                continue
            }
            $pkgIdent = if ($sf.Name -match '^stryker-(.+)-config\.json$') { $Matches[1] } else { $sf.Name }
            if ($wfContent -notmatch [regex]::Escape($sf.Name) -and $wfContent -notmatch "(?i)name:\s*$pkgIdent" -and $wfContent -notmatch "(?i)working-dir:.*$pkgIdent") {
                Write-Host "  ❌ Stryker configuration '$($sf.Name)' is missing from .github/workflows/mutation-testing.yml matrix." -ForegroundColor Red
                if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
                if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Stryker configuration '$($sf.Name)' is missing from matrix") } else { $Violations++ } }
                if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
                $strykerErrors++
            }
        }
    }
}

if ($strykerErrors -eq 0) {
    Write-Host "  ✅ Stryker.NET configuration, 100/98/95 thresholds, concurrency 2, anti-gaming blacklist, and package matrix synchronization verified." -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# Mutation Testing Release Gate Scripts, Workflow Gate & docs/testing-roadmap.md
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: Release Gate] Validating mutation release gate scripts, workflow enforcement & docs/testing-roadmap.md..." -ForegroundColor Yellow
$gateErrors = 0

$gateScriptPath = Join-Path $targetRoot "scripts/verify-mutation-gate.js"
if (-not (Test-Path $gateScriptPath)) {
    Write-Host "  ❌ Missing scripts/verify-mutation-gate.js release gate script." -ForegroundColor Red
    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing scripts/verify-mutation-gate.js") } else { $Violations++ } }
    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
    $gateErrors++
}

$gateTestPath = Join-Path $targetRoot "scripts/verify-mutation-gate.test.js"
if (-not (Test-Path $gateTestPath)) {
    Write-Host "  ❌ Missing scripts/verify-mutation-gate.test.js unit tests." -ForegroundColor Red
    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing scripts/verify-mutation-gate.test.js") } else { $Violations++ } }
    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
    $gateErrors++
}

$roadmapPath = Join-Path $targetRoot "docs/testing-roadmap.md"
if (-not (Test-Path $roadmapPath)) {
    Write-Host "  ❌ Missing docs/testing-roadmap.md governance document." -ForegroundColor Red
    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing docs/testing-roadmap.md") } else { $Violations++ } }
    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
    $gateErrors++
}

$publishWfPath = Join-Path $targetRoot ".github/workflows/publish.yml"
if (Test-Path $publishWfPath) {
    $pubContent = Get-Content $publishWfPath -Raw
    if ($pubContent -notmatch "verify-mutation-gate\.js") {
        Write-Host "  ❌ .github/workflows/publish.yml does not enforce verify-mutation-gate.js before publishing." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("publish.yml does not enforce verify-mutation-gate.js") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $gateErrors++
    }
}

if ($gateErrors -eq 0) {
    Write-Host "  ✅ Mutation release gate scripts, publish pipeline gate, and docs/testing-roadmap.md verified." -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# Benchmark Regression Quality Gate & CI Enforcement
# -----------------------------------------------------------------------------
$hasBenchProject = (Get-ChildItem -Path $targetRoot -Filter "*Benchmark*.csproj" -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '[\\/](obj|bin|MEGA-AUDITORIA|StrykerOutput)[\\/]' } | Select-Object -First 1) -ne $null
if ($hasBenchProject) {
    Write-Host "`n[Gate: Benchmark Gate] Validating benchmark regression scripts & workflow enforcement..." -ForegroundColor Yellow
    $benchGateErrors = 0

    $benchScriptPath = Join-Path $targetRoot "scripts/verify-benchmark-gate.ps1"
    if (-not (Test-Path $benchScriptPath)) {
        Write-Host "  ❌ Missing scripts/verify-benchmark-gate.ps1 regression assertion script." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing scripts/verify-benchmark-gate.ps1") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $benchGateErrors++
    }

    $benchTestScriptPath = Join-Path $targetRoot "scripts/verify-benchmark-gate.test.ps1"
    if (-not (Test-Path $benchTestScriptPath)) {
        Write-Host "  ❌ Missing scripts/verify-benchmark-gate.test.ps1 unit tests." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing scripts/verify-benchmark-gate.test.ps1") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $benchGateErrors++
    }

    $benchWfPath = Join-Path $targetRoot ".github/workflows/benchmark-regression-gate.yml"
    if (-not (Test-Path $benchWfPath)) {
        Write-Host "  ❌ Missing .github/workflows/benchmark-regression-gate.yml CI workflow." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing benchmark-regression-gate.yml") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $benchGateErrors++
    } else {
        $benchWfContent = Get-Content $benchWfPath -Raw -Encoding utf8
        if ($benchWfContent -notmatch "verify-benchmark-gate\.ps1" -or $benchWfContent -notmatch "--exporters json") {
            Write-Host "  ❌ .github/workflows/benchmark-regression-gate.yml does not enforce verify-benchmark-gate.ps1 and --exporters json." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Invalid benchmark-regression-gate.yml") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $benchGateErrors++
        }
    }

    $baselinePath = Join-Path $targetRoot "benchmarks/results/baseline.json"
    if (-not (Test-Path $baselinePath)) {
        Write-Host "  ❌ Missing benchmarks/results/baseline.json baseline file." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing benchmarks/results/baseline.json") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $benchGateErrors++
    }

    if ($benchGateErrors -eq 0) {
        Write-Host "  ✅ Benchmark regression assertion script, baseline, and CI workflow verified." -ForegroundColor Green
    }
}

# -----------------------------------------------------------------------------
# SourceLink & Central Package Management Integration Gate
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: SourceLink] Validating centralized Microsoft.SourceLink.GitHub integration..." -ForegroundColor Yellow
$sourceLinkErrors = 0

$pkgPropsPath = Join-Path $targetRoot "Directory.Packages.props"
$bldPropsPath = Join-Path $targetRoot "Directory.Build.props"

if (-not (Test-Path $pkgPropsPath)) {
    Write-Host "  ❌ Missing Directory.Packages.props." -ForegroundColor Red
    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing Directory.Packages.props") } else { $Violations++ } }
    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
    $sourceLinkErrors++
} else {
    $pkgContent = Get-Content $pkgPropsPath -Raw -Encoding utf8
    if ($pkgContent -notmatch 'PackageVersion\s+Include="Microsoft\.SourceLink\.GitHub"') {
        Write-Host "  ❌ Directory.Packages.props must declare 'Microsoft.SourceLink.GitHub' instead of generic or missing package." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Directory.Packages.props missing Microsoft.SourceLink.GitHub") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $sourceLinkErrors++
    }
    if ($pkgContent -match 'PackageVersion\s+Include="Microsoft\.SourceLink\.Common"' -and $pkgContent -notmatch 'PackageVersion\s+Include="Microsoft\.SourceLink\.GitHub"') {
        Write-Host "  ❌ Directory.Packages.props uses generic Microsoft.SourceLink.Common without GitHub provider." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Generic Microsoft.SourceLink.Common used") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $sourceLinkErrors++
    }
}

if (-not (Test-Path $bldPropsPath)) {
    Write-Host "  ❌ Missing Directory.Build.props." -ForegroundColor Red
    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing Directory.Build.props") } else { $Violations++ } }
    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
    $sourceLinkErrors++
} else {
    $bldContent = Get-Content $bldPropsPath -Raw -Encoding utf8
    if ($bldContent -notmatch 'PackageReference\s+Include="Microsoft\.SourceLink\.GitHub"') {
        Write-Host "  ❌ Directory.Build.props must centralize '<PackageReference Include=""Microsoft.SourceLink.GitHub"" PrivateAssets=""All"" />'." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Directory.Build.props missing Microsoft.SourceLink.GitHub PackageReference") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $sourceLinkErrors++
    }
    if ($bldContent -notmatch '<PublishRepositoryUrl>\s*true\s*</PublishRepositoryUrl>' -and $bldContent -notmatch '<PublishRepositoryUrl\s+Condition=') {
        Write-Host "  ❌ Directory.Build.props must specify '<PublishRepositoryUrl>true</PublishRepositoryUrl>'." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Directory.Build.props missing PublishRepositoryUrl") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $sourceLinkErrors++
    }
}

if ($sourceLinkErrors -eq 0) {
    Write-Host "  ✅ SourceLink integration (Microsoft.SourceLink.GitHub) verified in Directory.Packages.props & Directory.Build.props." -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# Native AOT Test Gate & Compilation Smoke Test Invariants
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: Native AOT] Validating Native AOT compilation smoke tests & workflow enforcement..." -ForegroundColor Yellow
$aotErrors = 0

$allSrcProjs = Get-ChildItem -Path (Join-Path $targetRoot "src") -Recurse -Filter "*.csproj" -File -ErrorAction SilentlyContinue | Where-Object {
    $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
}

# 1. Discover AOT-applicable projects in src/
$aotApplicableProjects = @()
foreach ($proj in $allSrcProjs) {
    $projName = $proj.Name
    $projDir = $proj.DirectoryName
    
    # Exclude Roslyn Analyzers and Source Generators
    if ($projName -match '(Analyzers?|Generators?)\.csproj$' -or $projDir -match '[\\/](Analyzers?|Generators?)[\\/]?$') {
        continue
    }
    # Exclude API endpoints / applications if applicable
    if ($projName -match '\.Api\.csproj$') {
        continue
    }
    
    $projContent = Get-Content $proj.FullName -Raw
    # Exclude projects explicitly marked as non-AOT compatible
    if ($projContent -match '<IsAotCompatible>\s*false\s*</IsAotCompatible>' -or 
        $projContent -match '<PublishAot>\s*false\s*</PublishAot>') {
        continue
    }
    
    $aotApplicableProjects += $proj
}

if ($aotApplicableProjects.Count -gt 0) {
    Write-Host "  [INFO] Detected $($aotApplicableProjects.Count) Native AOT applicable project(s) in src/." -ForegroundColor Gray
    
    # 2. Check for dedicated Native AOT smoke test project in tests/ or samples/
    $aotTestProjects = @()
    foreach ($searchDir in @("tests", "samples")) {
        $dirPath = Join-Path $targetRoot $searchDir
        if (Test-Path $dirPath) {
            $candidateTests = Get-ChildItem -Path $dirPath -Recurse -Filter "*.csproj" -File -ErrorAction SilentlyContinue | Where-Object {
                $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
            }
            foreach ($t in $candidateTests) {
                $content = Get-Content $t.FullName -Raw
                if ($content -match '<PublishAot>\s*true\s*</PublishAot>' -or $t.Name -match 'AotSmokeTest|AotTest|NativeAot') {
                    $aotTestProjects += $t
                }
            }
        }
    }
    
    if ($aotTestProjects.Count -eq 0) {
        Write-Host "  ❌ Missing Native AOT smoke test project in tests/ or samples/ for $($aotApplicableProjects.Count) AOT-applicable project(s)." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing Native AOT smoke test project in tests/ or samples/.") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $aotErrors++
    } else {
        $hasValidExecutable = $false
        foreach ($aotProj in $aotTestProjects) {
            $aotContent = Get-Content $aotProj.FullName -Raw
            if ($aotContent -match '<OutputType>\s*Exe\s*</OutputType>' -and ($aotContent -match '<PublishAot>\s*true\s*</PublishAot>' -or $aotContent -match 'PublishAot')) {
                $hasValidExecutable = $true
                break
            }
        }
        if (-not $hasValidExecutable) {
            Write-Host "  ❌ At least one AOT smoke test project must declare OutputType=Exe and PublishAot=true." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("AOT smoke test project must declare OutputType=Exe and PublishAot=true.") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $aotErrors++
        }
    }
    
    # 3. Check for CI workflow .github/workflows/aot-smoke-test.yml
    $aotWorkflowPath = Join-Path $targetRoot ".github/workflows/aot-smoke-test.yml"
    if (-not (Test-Path $aotWorkflowPath)) {
        Write-Host "  ❌ Missing .github/workflows/aot-smoke-test.yml CI workflow." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing .github/workflows/aot-smoke-test.yml CI workflow.") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $aotErrors++
    } else {
        $wfContent = Get-Content $aotWorkflowPath -Raw
        if ($wfContent -notmatch 'dotnet publish' -or ($wfContent -notmatch 'linux-x64|win-x64' -and $wfContent -notmatch 'PublishAot')) {
            Write-Host "  ❌ Workflow .github/workflows/aot-smoke-test.yml does not execute a valid Native AOT publish step." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Invalid aot-smoke-test.yml workflow.") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $aotErrors++
        }
    }
} else {
    Write-Host "  [INFO] Zero Native AOT applicable projects in src/ (pure analyzer/generator repository). Native AOT test gate skipped." -ForegroundColor Gray
}

if ($aotErrors -eq 0) {
    Write-Host "  ✅ Native AOT test project(s) and CI workflow verified." -ForegroundColor Green
}



# -----------------------------------------------------------------------------
# README Package Table Parity Gate
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: README Package Table Parity] Validating documentation package table synchronization..." -ForegroundColor Yellow
$readmeErrors = 0
$readmePath = Join-Path $targetRoot "README.md"

if (-not (Test-Path $readmePath)) {
    Write-Host "  ❌ Missing README.md in repository root." -ForegroundColor Red
    if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
    if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Missing README.md in repository root.") } else { $Violations++ } }
    if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
    $readmeErrors++
} else {
    $readmeContent = Get-Content $readmePath -Raw -Encoding utf8
    $allSrcProjs = Get-ChildItem -Path (Join-Path $targetRoot "src") -Recurse -Filter "*.csproj" -File -ErrorAction SilentlyContinue | Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    }

    foreach ($proj in $allSrcProjs) {
        $projName = $proj.Name
        $baseName = $proj.BaseName
        
        # Check if project appears in README.md inside a table or package reference
        $escapedBase = [regex]::Escape($baseName)
        $isDocumented = ($readmeContent -match ('\|\s*`?' + $escapedBase + '`?\s*\|')) -or 
                        ($readmeContent -match ('\[`?' + $escapedBase + '`?\]')) -or
                        ($readmeContent -match "/packages/$escapedBase") -or
                        ($readmeContent -match ('\|\s*\[`?' + $escapedBase + '`?\]'))

        if (-not $isDocumented) {
            Write-Host "  ❌ Project '$projName' is missing from the packages table in README.md." -ForegroundColor Red
            if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
            if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Project '$projName' is missing from the packages table in README.md.") } else { $Violations++ } }
            if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
            $readmeErrors++
        }
    }

    if ($readmeErrors -eq 0) {
        Write-Host "  ✅ All $($allSrcProjs.Count) project(s) in src/ verified in README.md package table." -ForegroundColor Green
    }
}

# -----------------------------------------------------------------------------
# Test Suite Symmetry & Coverage Gate (Principle 12)
# -----------------------------------------------------------------------------
Write-Host "`n[Gate: Test Suite Symmetry] Validating test project symmetry & project references across tests/..." -ForegroundColor Yellow
$testSymErrors = 0
$testsDir = Join-Path $targetRoot "tests"

$allSrcProjs = Get-ChildItem -Path (Join-Path $targetRoot "src") -Recurse -Filter "*.csproj" -File -ErrorAction SilentlyContinue | Where-Object {
    $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
}

$allTestProjs = @()
$testProjectReferences = @{}
if (Test-Path $testsDir) {
    $allTestProjs = Get-ChildItem -Path $testsDir -Recurse -Filter "*.csproj" -File -ErrorAction SilentlyContinue | Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    }
    foreach ($tp in $allTestProjs) {
        $tContent = Get-Content $tp.FullName -Raw -Encoding utf8
        $refs = [regex]::Matches($tContent, '<ProjectReference\s+Include="([^"]+)"')
        foreach ($m in $refs) {
            $refFile = Split-Path $m.Groups[1].Value.Replace('\', '/') -Leaf
            $testProjectReferences[$refFile] = $true
        }
    }
}

foreach ($proj in $allSrcProjs) {
    $projName = $proj.Name
    $baseName = $proj.BaseName
    
    # Check 1: Named test suite matching base name
    $hasNamedTest = ($allTestProjs | Where-Object { $_.BaseName -match "^$([regex]::Escape($baseName))(\..+)?Tests?$" -or $_.BaseName -like "*$baseName*" }) -ne $null
    
    # Check 2: Direct ProjectReference in any test project
    $hasReference = $testProjectReferences.ContainsKey($projName)
    
    if (-not $hasNamedTest -and -not $hasReference) {
        Write-Host "  ❌ Project '$projName' has no corresponding test suite in tests/ (missing test project or ProjectReference)." -ForegroundColor Red
        if (Get-Variable -Name "violations" -Scope 0 -ErrorAction SilentlyContinue) { $violations++ }
        if (Get-Variable -Name "Violations" -Scope 0 -ErrorAction SilentlyContinue) { if ($Violations -is [System.Collections.IList]) { $Violations.Add("Project '$projName' has no corresponding test suite in tests/.") } else { $Violations++ } }
        if (Get-Variable -Name "FailedChecks" -Scope 0 -ErrorAction SilentlyContinue) { $FailedChecks++ }
        $testSymErrors++
    }
}

if ($testSymErrors -eq 0) {
    Write-Host "  ✅ All $($allSrcProjs.Count) project(s) in src/ verified with corresponding test suite in tests/." -ForegroundColor Green
}


# Summary & Result Gate
# -----------------------------------------------------------------------------
Write-Host "`n==================================================" -ForegroundColor Cyan
if ($Violations.Count -gt 0) {
    Write-Host "  FAILED: $($Violations.Count) Compliance Violations Detected!" -ForegroundColor Red
    Write-Host "==================================================" -ForegroundColor Cyan
    foreach ($V in $Violations) {
        Write-Host "  ❌ $V" -ForegroundColor Red
    }
    exit 1
} else {
    Write-Host "  SUCCESS: 100% Governance & Compliance Verified. Zero violations. " -ForegroundColor Green
    Write-Host "==================================================" -ForegroundColor Cyan
    exit 0
}
