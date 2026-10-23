[CmdletBinding()]
param([string]$OutputDirectory = 'artifacts/releases', [switch]$NoRestore)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot
try {
    if (git status --porcelain) { throw 'Commit or remove pending changes before creating a release.' }
    $commit = (git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Cannot determine source commit.' }
    [xml]$project = Get-Content -LiteralPath 'HappyColorSplit.csproj' -Raw
    $version = [string]$project.Project.PropertyGroup.Version
    if ($version -notmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?$') { throw 'Invalid application version.' }
    $output = [IO.Path]::GetFullPath($OutputDirectory, $repoRoot)
    if (-not $output.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Release output must be inside the project directory.'
    }
    $null = New-Item -ItemType Directory -Path $output -Force
    $name = 'HappyColorSplit-' + $version
    $zip = Join-Path $output ($name + '-Windows-x64.zip')
    $sourceZip = Join-Path $output ($name + '-Source.zip')
    $checksumFile = Join-Path $output ($name + '-SHA256SUMS.txt')
    foreach ($path in @($zip, $sourceZip, $checksumFile)) {
        if (Test-Path -LiteralPath $path) { throw "Release file already exists: $path" }
    }
    $staging = Join-Path $output ('staging-' + [Guid]::NewGuid().ToString('N'))
    $publish = Join-Path $staging 'HappyColorSplit'
    $publishArgs = @('publish', 'HappyColorSplit.csproj', '-p:PublishProfile=Windows', '-p:UsedAvaloniaProducts=', '-o', $publish)
    if ($NoRestore) { $publishArgs += '--no-restore' }
    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) { throw 'Windows publish failed.' }
    $productVersion = (Get-Item -LiteralPath (Join-Path $publish 'HappyColorSplit.exe')).VersionInfo.ProductVersion
    if ($productVersion -ne ($version + '+' + $commit)) { throw 'Published binary does not identify the exact source commit.' }

    $usageNoticeDe = Get-Content -LiteralPath (Join-Path $publish 'docs/USAGE_NOTICE_DE.md') -Raw -Encoding utf8
    $usageNoticeEn = Get-Content -LiteralPath (Join-Path $publish 'docs/USAGE_NOTICE.md') -Raw -Encoding utf8
    @"
HappyColorSplit $version - Windows x64

DE: Die ZIP-Datei vollstaendig entpacken und HappyColorSplit.exe starten.
Die .NET-10-Laufzeit ist enthalten. Keine zusaetzliche .NET-Installation erforderlich.
Alle Dateien und Unterordner zusammen behalten. Nur die EXE zu kopieren reicht nicht.
Diese Ausgabe ist eine Vorabversion. Informationen zu Quellcode und Lizenzen: SOURCE.txt

Windows kann die EXE nach Download und Entpacken zunaechst blockieren.
Nur bei gepruefter Herkunft aus dem offiziellen GitHub-Release fortfahren:
Bei SmartScreen ggf. Weitere Informationen > Trotzdem ausfuehren waehlen.
Bei einer Dateiblockierung: Rechtsklick auf HappyColorSplit.exe > Eigenschaften >
Allgemein > Sicherheit > Zulassen (falls vorhanden), dann Uebernehmen.
Alternativ die ZIP vor dem Entpacken auf dieselbe Weise zulassen und neu entpacken.
Nicht bei konkreter Malware-Meldung oder administrativer Sperre umgehen.
Defender und SmartScreen nicht generell deaktivieren. Zulassen ist keine Sicherheitsgarantie.

EN: Extract the entire ZIP and run HappyColorSplit.exe.
The .NET 10 runtime is included. No separate .NET installation is required.
Keep all files and subdirectories together. This is a preview version.
See SOURCE.txt for source code and licensing information.

Windows may initially block the EXE after downloading and extracting the ZIP.
Proceed only after verifying its origin from the official GitHub release:
SmartScreen may offer More info > Run anyway for an unknown app.
For a downloaded-file block: right-click HappyColorSplit.exe > Properties > General >
Unblock (if available), then Apply. Alternatively, unblock the ZIP and extract again.
Do not bypass a malware detection or an administrator's policy.
Do not disable Defender or SmartScreen globally. Unblocking is not a safety guarantee.

$usageNoticeDe

$usageNoticeEn
"@ | Set-Content -LiteralPath (Join-Path $publish 'START_HERE.txt') -Encoding utf8
    @"
HappyColorSplit $version
Source commit: $commit
License: GPL-3.0-or-later (see LICENSE)
Third-party license texts: licenses/ and docs/THIRD_PARTY_COMPONENTS*.md

Matching source archive supplied alongside this binary:
$name-Source.zip
https://github.com/HappyProjectLab/HappyColorSplit/releases/download/v$version/$name-Source.zip

Repository: https://github.com/HappyProjectLab/HappyColorSplit
Source revision: https://github.com/HappyProjectLab/HappyColorSplit/tree/$commit

GitHub access follows the repository's current visibility and permissions.
When redistributing, provide the matching source and retain the license documents.
The source archive contains the project, build instructions, dependency versions and
release scripts. Third-party components retain their own licenses and notices.
"@ | Set-Content -LiteralPath (Join-Path $publish 'SOURCE.txt') -Encoding utf8

    & (Join-Path $PSScriptRoot 'ValidateThirdPartyCompliance.ps1') -PublishDirectory $publish
    & git archive --format=zip "--prefix=$name-Source/" "--output=$sourceZip" $commit
    if ($LASTEXITCODE -ne 0) { throw 'Source archive creation failed.' }
    [IO.Compression.ZipFile]::CreateFromDirectory($publish, $zip, [IO.Compression.CompressionLevel]::Optimal, $true)

    # Verify the bytes inside the final download, not just the staging directory.
    $archive = [IO.Compression.ZipFile]::OpenRead($zip)
    try {
        $files = @(Get-ChildItem -LiteralPath $publish -File -Recurse)
        $entries = @($archive.Entries | Where-Object { $_.Name })
        if ($entries.Count -ne $files.Count) { throw 'ZIP file count mismatch.' }
        foreach ($file in $files) {
            $relative = [IO.Path]::GetRelativePath($publish, $file.FullName).Replace('\', '/')
            $entry = $archive.GetEntry('HappyColorSplit/' + $relative)
            if (-not $entry) { throw "ZIP entry missing: $relative" }
            $stream = $entry.Open()
            try { $hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) }
            finally { $stream.Dispose() }
            if ($hash -ne (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash) { throw "ZIP content differs: $relative" }
        }
    } finally { $archive.Dispose() }
    $checksumLines = foreach ($path in @($zip, $sourceZip)) {
        $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $([IO.Path]::GetFileName($path))"
    }
    $checksumLines | Set-Content -LiteralPath $checksumFile -Encoding ascii
    Write-Host "PASS: ZIP contains $($files.Count) verified files. Source commit: $commit"
    Get-Item -LiteralPath $zip, $sourceZip, $checksumFile | Select-Object Name, Length, FullName
} finally { Pop-Location }
