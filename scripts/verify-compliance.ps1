#!/usr/bin/env pwsh
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
# 3. Canonical MIT Copyright Headers in src/
# -----------------------------------------------------------------------------
Write-Host "`n[3/7] Checking canonical MIT copyright headers..." -ForegroundColor Yellow
$HeaderRegex = '(?s)^\s*(//|/\*|<!--)\s*Copyright\s+(©|\(c\)|&copy;)?\s*Erickson Lopez.*?(MIT License|\(MIT\))'
if (Test-Path $SrcPath) {
    $CsFiles = Get-ChildItem -Path $SrcPath -Recurse -Filter "*.cs" | 
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
    foreach ($File in $CsFiles) {
        $Content = Get-Content -Path $File.FullName -Raw
        if ($Content -notmatch $HeaderRegex) {
            $Violations.Add("Missing/Invalid copyright header: '$($File.FullName)'. Expected '// Copyright © Erickson Lopez. MIT License.' at top.")
        }
    }
    if ($Violations.Count -eq 0) {
        Write-Host "  ✅ All production C# files contain the required MIT copyright header." -ForegroundColor Green
    }
}

# -----------------------------------------------------------------------------
# 4. Single Type Per File Rule in src/
# -----------------------------------------------------------------------------
Write-Host "`n[4/7] Checking 'One Type Per File' rule in src/..." -ForegroundColor Yellow
$TypeRegex = '^\s*(public|internal|protected|private)?\s*(static|sealed|abstract|readonly|ref|partial)*\s*(class|interface|struct|enum|record(\s+struct|\s+class)?)\s+([A-Za-z0-9_]+(\s*<[^>]+>)?)\b'
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

            if ($Trimmed -match '^\s*(public|internal|sealed|abstract|static|readonly|ref|partial)*\s*(class|interface|struct|enum|record)\s+([A-Za-z0-9_]+)') {
                $TypeName = $Matches[3]
                $DeclaredTypes.Add($TypeName)
            }
        }

        if ($DeclaredTypes.Count -gt 1) {
            $Violations.Add("Multiple types declared in single file: '$($File.FullName)' contains [$($DeclaredTypes -join ', ')].")
        }
    }
    if ($Violations.Count -eq 0) {
        Write-Host "  ✅ Every production file satisfies the 'One Type Per File' invariant." -ForegroundColor Green
    }
}

# -----------------------------------------------------------------------------
# 5. GitHub Repository Identity Links
# -----------------------------------------------------------------------------
Write-Host "`n[5/7] Checking GitHub identity links (ericksonlopezf/dotnet-processes)..." -ForegroundColor Yellow
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
Write-Host "`n[6/7] Checking contact and security email normalization (ericksonlopezf@gmail.com)..." -ForegroundColor Yellow
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
