[CmdletBinding()]
param([string]$PublishDirectory)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$failures = [Collections.Generic.List[string]]::new()
$checkCount = 0

function Check([bool]$Condition, [string]$Message) {
    $script:checkCount++
    if (-not $Condition) { $script:failures.Add($Message) }
}

function Check-File([string]$Path, [string]$Hash = '') {
    $exists = Test-Path -LiteralPath $Path -PathType Leaf
    Check $exists "Missing file: $Path"
    if ($exists -and $Hash) {
        Check ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -eq $Hash) "Content differs from inventory: $Path"
    }
    return $exists
}

$inventoryPath = Join-Path $repoRoot 'licenses/component-inventory.json'
if (-not (Check-File $inventoryPath)) { throw 'Component inventory missing.' }
$inventory = Get-Content -LiteralPath $inventoryPath -Raw | ConvertFrom-Json -AsHashtable
Check ($inventory.schemaVersion -eq 2 -and $inventory.application -eq 'HappyColorSplit') 'Unknown inventory schema/application.'
Check ($inventory.selfContained -eq $true) 'Inventory must describe the self-contained distribution.'

foreach ($file in $inventory.upstreamLegalFiles) {
    $null = Check-File (Join-Path $repoRoot $file.path) $file.sha256
}
$documentation = @('LICENSE', 'licenses/THIRD_PARTY_NOTICES.md', 'licenses/component-inventory.json',
    'docs/THIRD_PARTY_COMPONENTS.md', 'docs/THIRD_PARTY_COMPONENTS_DE.md', 'docs/USAGE_NOTICE.md', 'docs/USAGE_NOTICE_DE.md')
foreach ($path in $documentation) { $null = Check-File (Join-Path $repoRoot $path) }

[xml]$project = Get-Content -LiteralPath (Join-Path $repoRoot 'HappyColorSplit.csproj') -Raw
Check ($project.Project.PropertyGroup.PackageLicenseExpression -eq 'GPL-3.0-or-later') 'Application license changed.'
$expectedDirect = @{'Avalonia'='12.1.1'; 'Avalonia.Desktop'='12.1.1'; 'Avalonia.Themes.Fluent'='12.1.1';
    'Avalonia.Fonts.Inter'='12.1.1'; 'SkiaSharp'='3.119.4'}
$direct = @{}
foreach ($reference in $project.Project.ItemGroup.PackageReference) {
    if ($reference.Include) { $direct[[string]$reference.Include] = [string]$reference.Version }
}
Check ($direct.Count -eq $expectedDirect.Count) 'Direct dependency set changed; re-inventory required.'
foreach ($entry in $expectedDirect.GetEnumerator()) { Check ($direct[$entry.Key] -eq $entry.Value) "Direct dependency changed: $($entry.Key)" }

[xml]$profile = Get-Content -LiteralPath (Join-Path $repoRoot 'Properties/PublishProfiles/Windows.pubxml') -Raw
$expectedProfile = @{TargetFramework='net10.0'; RuntimeIdentifier='win-x64'; SelfContained='true'; PublishSelfContained='true';
    RuntimeFrameworkVersion=$inventory.runtimePack.version; PublishSingleFile='false'; PublishTrimmed='false';
    PublishReadyToRun='false'; CopyOutputSymbolsToPublishDirectory='false'}
foreach ($entry in $expectedProfile.GetEnumerator()) {
    Check ([string]$profile.Project.PropertyGroup.($entry.Key) -eq $entry.Value) "Publish setting changed: $($entry.Key)"
}

$sdkVersion = (dotnet --version).Trim()
Check ($sdkVersion -eq $inventory.appHost.sdkVersion) 'SDK version changed; re-audit the native apphost template.'
$dotnetDirectory = Split-Path -Parent (Get-Command dotnet).Source
$null = Check-File (Join-Path $dotnetDirectory $inventory.appHost.templateRelativePath) $inventory.appHost.templateSha256
foreach ($path in $inventory.appHost.licenseFiles) { $null = Check-File (Join-Path $repoRoot $path) }

$assetsPath = Join-Path $repoRoot 'obj/project.assets.json'
if (-not (Check-File $assetsPath)) { throw 'Restore the project before validation.' }
$assets = Get-Content -LiteralPath $assetsPath -Raw | ConvertFrom-Json -AsHashtable

$knownPackages = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($package in $inventory.packages) {
    $key = "$($package.name)/$($package.version)"
    $null = $knownPackages.Add($key)
    Check ($assets.libraries.ContainsKey($key)) "Resolved dependency missing/changed: $key"
    foreach ($legalFile in $package.licenseFiles) { $null = Check-File (Join-Path $repoRoot $legalFile) }
    $relative = $package.name.ToLowerInvariant() + '/' + $package.version
    $cacheDirectory = @($assets.packageFolders.Keys | ForEach-Object { Join-Path $_ $relative } |
        Where-Object { Test-Path -LiteralPath $_ -PathType Container }) | Select-Object -First 1
    Check ([bool]$cacheDirectory) "Package absent from NuGet cache: $key"
    if (-not $cacheDirectory) { continue }
    $archiveName = $package.name.ToLowerInvariant() + '.' + $package.version + '.nupkg'
    $null = Check-File (Join-Path $cacheDirectory $archiveName) $package.archiveSha256
    [xml]$spec = Get-Content -LiteralPath (Join-Path $cacheDirectory ($package.name.ToLowerInvariant() + '.nuspec')) -Raw
    Check ([string]$spec.package.metadata.repository.commit -eq $package.revision) "Package revision changed: $key"
    foreach ($binary in @($inventory.binaryAssets | Where-Object { $_.package -eq $key })) {
        $null = Check-File (Join-Path $cacheDirectory $binary.packagePath) $binary.sha256
    }
}

$runtimePack = $inventory.runtimePack
$runtimeCache = @($assets.packageFolders.Keys | ForEach-Object {
    Join-Path $_ ($runtimePack.name.ToLowerInvariant() + '/' + $runtimePack.version)
} | Where-Object { Test-Path -LiteralPath $_ -PathType Container }) | Select-Object -First 1
Check ([bool]$runtimeCache) 'Audited .NET runtime pack missing from NuGet cache.'
if ($runtimeCache) {
    $null = Check-File (Join-Path $runtimeCache ($runtimePack.name.ToLowerInvariant() + '.' + $runtimePack.version + '.nupkg')) $runtimePack.archiveSha256
    [xml]$spec = Get-Content -LiteralPath (Join-Path $runtimeCache ($runtimePack.name.ToLowerInvariant() + '.nuspec')) -Raw
    Check ([string]$spec.package.metadata.repository.commit -eq $runtimePack.revision) 'Runtime source revision changed.'
    foreach ($binary in $runtimePack.binaryAssets) {
        $null = Check-File (Join-Path $runtimeCache $binary.packagePath) $binary.sha256
    }
    foreach ($path in $runtimePack.licenseFiles) {
        $hash = (Get-FileHash -LiteralPath (Join-Path $runtimeCache (Split-Path -Leaf $path)) -Algorithm SHA256).Hash
        $null = Check-File (Join-Path $repoRoot $path) $hash
    }
}

if ($PublishDirectory) {
    $publish = [IO.Path]::GetFullPath($PublishDirectory)
    Check (Test-Path -LiteralPath $publish -PathType Container) "Publish directory missing: $publish"
    if (Test-Path -LiteralPath $publish -PathType Container) {
        $legalCopies = @('LICENSE', 'docs/THIRD_PARTY_COMPONENTS.md', 'docs/THIRD_PARTY_COMPONENTS_DE.md', 'docs/USAGE_NOTICE.md', 'docs/USAGE_NOTICE_DE.md') +
            @(Get-ChildItem -LiteralPath (Join-Path $repoRoot 'licenses') -Recurse -File | ForEach-Object { [IO.Path]::GetRelativePath($repoRoot, $_.FullName) })
        foreach ($path in $legalCopies) {
            $hash = (Get-FileHash -LiteralPath (Join-Path $repoRoot $path) -Algorithm SHA256).Hash
            $null = Check-File (Join-Path $publish $path) $hash
        }
        $startHere = Join-Path $publish 'START_HERE.txt'
        if (Test-Path -LiteralPath $startHere -PathType Leaf) {
            $instructions = Get-Content -LiteralPath $startHere -Raw -Encoding utf8
            foreach ($path in @('docs/USAGE_NOTICE_DE.md', 'docs/USAGE_NOTICE.md')) {
                $notice = (Get-Content -LiteralPath (Join-Path $repoRoot $path) -Raw -Encoding utf8).Trim()
                Check ($instructions.Contains($notice)) "Startup instructions must include the complete usage notice: $path"
            }
        }
        foreach ($binary in $inventory.binaryAssets) { $null = Check-File (Join-Path $publish $binary.path) $binary.sha256 }
        foreach ($binary in $runtimePack.binaryAssets) { $null = Check-File (Join-Path $publish $binary.path) $binary.sha256 }
        foreach ($path in @('HappyColorSplit.exe', 'HappyColorSplit.dll')) { $null = Check-File (Join-Path $publish $path) }
        Check (-not (Test-Path -LiteralPath (Join-Path $publish 'HappyColorSplit.pdb'))) 'Application debug symbols can expose local build paths; exclude them from distribution.'

        $depsPath = Join-Path $publish 'HappyColorSplit.deps.json'
        if (Check-File $depsPath) {
            $deps = Get-Content -LiteralPath $depsPath -Raw | ConvertFrom-Json -AsHashtable
            Check ($deps.runtimeTarget.name -eq '.NETCoreApp,Version=v10.0/win-x64') 'Publish is not the audited .NET 10 win-x64 target.'
            $actualPackages = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
            $actualAssets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
            $target = $deps.targets[$deps.runtimeTarget.name]
            foreach ($entry in $target.GetEnumerator()) {
                if ($deps.libraries[$entry.Key].type -ne 'package') { continue }
                foreach ($kind in @('runtime', 'native')) {
                    if (-not $entry.Value.ContainsKey($kind)) { continue }
                    $null = $actualPackages.Add($entry.Key)
                    foreach ($path in $entry.Value[$kind].Keys) { $null = $actualAssets.Add($entry.Key + '|' + $path) }
                }
                Check (-not $entry.Value.ContainsKey('runtimeTargets')) "Unexpected unfiltered runtime assets: $($entry.Key)"
            }
            Check ($knownPackages.SetEquals($actualPackages)) 'Shipped package set differs from the inventory.'
            $expectedAssets = @($inventory.binaryAssets | ForEach-Object { $_.package + '|' + $_.packagePath })
            Check ($actualAssets.SetEquals([string[]]$expectedAssets)) 'Shipped dependency asset set differs from the inventory.'
            $runtimeKey = 'runtimepack.' + $runtimePack.name + '/' + $runtimePack.version
            $runtimeLibraries = @($deps.libraries.Keys | Where-Object { $deps.libraries[$_].type -eq 'runtimepack' })
            Check ($runtimeLibraries.Count -eq 1 -and $runtimeLibraries[0] -eq $runtimeKey) 'Unexpected runtime pack set.'
            $actualRuntimeAssets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
            if ($target.ContainsKey($runtimeKey)) {
                foreach ($kind in @('runtime', 'native')) {
                    foreach ($path in $target[$runtimeKey][$kind].Keys) { $null = $actualRuntimeAssets.Add($kind + '|' + $path) }
                }
            }
            $expectedRuntimeAssets = @($runtimePack.binaryAssets | ForEach-Object { $_.kind + '|' + $_.path })
            Check ($actualRuntimeAssets.SetEquals([string[]]$expectedRuntimeAssets)) 'Runtime asset set differs from the inventory.'
        }

        $runtimePath = Join-Path $publish 'HappyColorSplit.runtimeconfig.json'
        if (Check-File $runtimePath) {
            $runtime = (Get-Content -LiteralPath $runtimePath -Raw | ConvertFrom-Json -AsHashtable).runtimeOptions
            Check ($runtime.tfm -eq 'net10.0' -and -not $runtime.ContainsKey('framework') -and -not $runtime.ContainsKey('frameworks')) 'Unexpected shared framework requirement.'
            Check ($runtime.includedFrameworks.Count -eq 1 -and $runtime.includedFrameworks[0].name -eq 'Microsoft.NETCore.App' -and
                $runtime.includedFrameworks[0].version -eq $runtimePack.version) 'Bundled runtime configuration differs from the inventory.'
        }
        $allowedFiles = @('HappyColorSplit.dll', 'HappyColorSplit.exe', 'HappyColorSplit.deps.json', 'HappyColorSplit.runtimeconfig.json') +
            @($inventory.binaryAssets.path) + @($runtimePack.binaryAssets.path) + @($legalCopies | ForEach-Object { $_.Replace('\', '/') }) +
            @('START_HERE.txt', 'SOURCE.txt')
        foreach ($file in Get-ChildItem -LiteralPath $publish -Recurse -File) {
            $relativePath = [IO.Path]::GetRelativePath($publish, $file.FullName).Replace('\', '/')
            Check ($allowedFiles -contains $relativePath) "Unexpected distribution file: $relativePath"
            Check ($file.Extension -notin @('.so', '.dylib', '.wasm', '.a')) "Unexpected non-Windows native asset: $relativePath"
        }
    }
}

if ($failures.Count -gt 0) { throw ("Third-party verification failed:`n - " + ($failures -join "`n - ")) }
Write-Host "PASS: $checkCount technical license, dependency and distribution checks."
Write-Host 'Checks detect selected technical drift; they do not certify legal completeness.'
