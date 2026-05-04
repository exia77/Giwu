<#
.SYNOPSIS
    Builds and signs an MSIX bundle for Giwu.HRMS.Hybrid for Windows installation.

.DESCRIPTION
    Generates a self-signed code-signing certificate in CurrentUser\My (if missing),
    exports a public CER for end-user trust, then runs `dotnet publish` and signs the
    package using the cert thumbprint (avoids password-protected PFX import issues).

.PARAMETER Subject
    Certificate subject. MUST match the Publisher in Package.appxmanifest.

.EXAMPLE
    .\build-msix.ps1
#>

[CmdletBinding()]
param(
    [string]$Subject  = "CN=Giwu HRMS, O=Giwu, C=PH",
    [string]$CerPath  = "$PSScriptRoot\GiwuHRMS.cer",
    [string]$PfxPath  = "$PSScriptRoot\GiwuHRMS.pfx",
    [string]$PfxPassword = "ChangeMe!",
    [string]$Configuration = "Release",
    [string]$TargetFramework = "net10.0-windows10.0.19041.0",
    [string]$RuntimeIdentifier = "win10-x64"
)

$ErrorActionPreference = "Stop"

Write-Host "==> Checking for existing signing certificate..." -ForegroundColor Cyan

$existing = Get-ChildItem -Path Cert:\CurrentUser\My |
    Where-Object { $_.Subject -eq $Subject -and $_.HasPrivateKey }

# Validate that the existing cert has the Code Signing EKU. If not, recreate it.
if ($existing) {
    $hasCodeSigningEku = $false
    foreach ($c in $existing) {
        foreach ($ext in $c.Extensions) {
            if ($ext.Oid.Value -eq "2.5.29.37") {
                $eku = [System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]$ext
                foreach ($oid in $eku.EnhancedKeyUsages) {
                    if ($oid.Value -eq "1.3.6.1.5.5.7.3.3") { $hasCodeSigningEku = $true }
                }
            }
        }
    }
    if (-not $hasCodeSigningEku) {
        Write-Host "==> Existing cert lacks Code Signing EKU. Removing and regenerating..." -ForegroundColor Yellow
        $existing | ForEach-Object { Remove-Item -Path "Cert:\CurrentUser\My\$($_.Thumbprint)" -Force }
        $existing = $null
    }
}

if (-not $existing) {
    Write-Host "==> Creating self-signed code-signing certificate: $Subject" -ForegroundColor Cyan
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $Subject `
        -KeyUsage DigitalSignature `
        -KeyAlgorithm RSA `
        -KeyLength 2048 `
        -HashAlgorithm SHA256 `
        -FriendlyName "Giwu HRMS Signing" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -NotAfter (Get-Date).AddYears(3) `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
} else {
    Write-Host "==> Reusing existing certificate (Thumbprint: $($existing.Thumbprint))" -ForegroundColor Yellow
    $cert = $existing | Select-Object -First 1
}

$thumbprint = $cert.Thumbprint
Write-Host "==> Using cert thumbprint: $thumbprint" -ForegroundColor Cyan

# Export public CER (no password) for end-user trust import
Write-Host "==> Exporting public CER -> $CerPath" -ForegroundColor Cyan
Export-Certificate -Cert "Cert:\CurrentUser\My\$thumbprint" -FilePath $CerPath -Force | Out-Null

# Also export PFX (kept for reference / CI use; not used by this build)
$securePwd = ConvertTo-SecureString -String $PfxPassword -Force -AsPlainText
Export-PfxCertificate -Cert "Cert:\CurrentUser\My\$thumbprint" -FilePath $PfxPath -Password $securePwd -Force | Out-Null

Write-Host "==> Publishing MSIX (Configuration=$Configuration, TFM=$TargetFramework, RID=$RuntimeIdentifier)" -ForegroundColor Cyan

dotnet publish `
    -f $TargetFramework `
    -c $Configuration `
    /p:RuntimeIdentifierOverride=$RuntimeIdentifier `
    /p:WindowsPackageType=MSIX `
    /p:GenerateAppxPackageOnBuild=true `
    /p:AppxPackageSigningEnabled=true `
    /p:PackageCertificateThumbprint=$thumbprint `
    /p:AppxPackageSigningTimestampDigestAlgorithm=SHA256 `
    /p:AppxBundle=Always `
    /p:AppxBundlePlatforms=x64

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

$pkgRoot = Join-Path $PSScriptRoot "bin\$Configuration\$TargetFramework\$RuntimeIdentifier\AppPackages"
Write-Host ""
Write-Host "==> Build complete." -ForegroundColor Green
Write-Host "    Packages: $pkgRoot" -ForegroundColor Green
Write-Host "    Public cert (give to end users): $CerPath" -ForegroundColor Green
Write-Host ""
Write-Host "INSTALL ON TARGET MACHINE:" -ForegroundColor Yellow
Write-Host "  1. Right-click GiwuHRMS.cer -> Install Certificate -> Local Machine -> Trusted People"
Write-Host "  2. Double-click the .msix / .msixbundle -> Install"
