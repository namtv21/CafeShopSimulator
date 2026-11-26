param(
    [Parameter(Mandatory = $true)]
    [string]$LocalAssetFolder
)

$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path -LiteralPath $LocalAssetFolder).Path
$entries = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'local-assets.txt')
foreach ($entry in $entries) {
    if ($entry -notmatch '^Assets/[A-Za-z0-9_./ ()-]+$' -or $entry -match '(^|/)\.\.(/|$)') {
        throw "Invalid asset destination: $entry"
    }
    if (-not (Test-Path -LiteralPath (Join-Path $sourceRoot $entry) -PathType Leaf)) {
        throw "Missing local asset: $entry"
    }
}
foreach ($entry in $entries) {
    $target = Join-Path $PSScriptRoot $entry
    New-Item -ItemType Directory -Force (Split-Path -Parent $target) | Out-Null
    Copy-Item -LiteralPath (Join-Path $sourceRoot $entry) -Destination $target
}
