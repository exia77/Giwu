<#
.SYNOPSIS
    GUI installer for Giwu HRMS. Imports the signing certificate into the
    LocalMachine certificate stores and then installs the MSIX bundle.

.DESCRIPTION
    Replaces the manual "right-click .cer → Install Certificate → Local Machine
    → Trusted People … repeat for Trusted Root" flow.

    Run by double-clicking install-msix.cmd (the wrapper) — it relaunches this
    script with -ExecutionPolicy Bypass and self-elevates to Administrator.

.PARAMETER CerPath
    Path to the public .cer exported by build-msix.ps1. Defaults to GiwuHRMS.cer
    next to this script.

.PARAMETER MsixPath
    Path to the .msix / .msixbundle. If omitted, auto-detects the most recent
    one under bin\Release\…\AppPackages.

.PARAMETER Silent
    Skip the GUI and run with stdout messages only. Useful for CI / scripted
    deployments.
#>

[CmdletBinding()]
param(
    [string]$CerPath  = "$PSScriptRoot\GiwuHRMS.cer",
    [string]$MsixPath = "",
    [switch]$Silent
)

$ErrorActionPreference = "Stop"

# ── Self-elevate ────────────────────────────────────────────────────────────
$currentUser = New-Object Security.Principal.WindowsPrincipal(
    [Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentUser.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    $argList = @(
        '-NoProfile','-ExecutionPolicy','Bypass',
        '-File', "`"$PSCommandPath`"",
        '-CerPath', "`"$CerPath`""
    )
    if ($MsixPath) { $argList += @('-MsixPath', "`"$MsixPath`"") }
    if ($Silent)   { $argList += '-Silent' }
    Start-Process -FilePath 'powershell.exe' -ArgumentList $argList -Verb RunAs
    exit 0
}

# ── Auto-detect MSIX if not supplied ───────────────────────────────────────
if (-not $MsixPath) {
    $searchRoots = @(
        "$PSScriptRoot",
        "$PSScriptRoot\bin\Release"
    )
    foreach ($r in $searchRoots) {
        if (-not (Test-Path $r)) { continue }
        $cand = Get-ChildItem -Path $r -Recurse -Include *.msix, *.msixbundle -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($cand) { $MsixPath = $cand.FullName; break }
    }
}

# ── Worker: do the actual install steps ────────────────────────────────────
function Invoke-Install {
    param(
        [string]$Cer,
        [string]$Msix,
        [scriptblock]$Log
    )

    & $Log "Verifying files..." 'info'
    if (-not (Test-Path $Cer))  { throw "Certificate not found: $Cer" }
    if (-not (Test-Path $Msix)) { throw "MSIX package not found: $Msix" }

    & $Log "Importing certificate -> LocalMachine\TrustedPeople" 'info'
    Import-Certificate -FilePath $Cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople -ErrorAction Stop | Out-Null

    & $Log "Importing certificate -> LocalMachine\Root (Trusted Root CAs)" 'info'
    Import-Certificate -FilePath $Cer -CertStoreLocation Cert:\LocalMachine\Root -ErrorAction Stop | Out-Null

    & $Log "Installing MSIX package..." 'info'
    Add-AppxPackage -Path $Msix -ErrorAction Stop

    & $Log "Done. Launch 'Giwu HRMS' from the Start menu." 'success'
}

# ── Silent / console mode ──────────────────────────────────────────────────
if ($Silent -or -not [Environment]::UserInteractive) {
    $log = {
        param($msg, $level)
        $color = switch ($level) {
            'success' { 'Green' }
            'error'   { 'Red' }
            default   { 'Cyan' }
        }
        Write-Host "==> $msg" -ForegroundColor $color
    }
    try {
        Invoke-Install -Cer $CerPath -Msix $MsixPath -Log $log
        exit 0
    } catch {
        & $log $_.Exception.Message 'error'
        exit 1
    }
}

# ── GUI mode (WinForms) ────────────────────────────────────────────────────
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$form              = New-Object System.Windows.Forms.Form
$form.Text         = "Giwu HRMS Installer"
$form.Size         = New-Object System.Drawing.Size(560, 420)
$form.StartPosition = 'CenterScreen'
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox  = $false
$form.MinimizeBox  = $false
$form.BackColor    = [System.Drawing.Color]::White

$header           = New-Object System.Windows.Forms.Label
$header.Text      = "Install Giwu HRMS"
$header.Font      = New-Object System.Drawing.Font("Segoe UI", 18, [System.Drawing.FontStyle]::Bold)
$header.Location  = New-Object System.Drawing.Point(24, 18)
$header.Size      = New-Object System.Drawing.Size(500, 32)
$form.Controls.Add($header)

$subtitle         = New-Object System.Windows.Forms.Label
$subtitle.Text    = "This installer trusts the signing certificate, then installs the app."
$subtitle.Font    = New-Object System.Drawing.Font("Segoe UI", 9)
$subtitle.ForeColor = [System.Drawing.Color]::FromArgb(110,110,110)
$subtitle.Location = New-Object System.Drawing.Point(24, 52)
$subtitle.Size    = New-Object System.Drawing.Size(500, 20)
$form.Controls.Add($subtitle)

# Cert row
$cerLabel         = New-Object System.Windows.Forms.Label
$cerLabel.Text    = "Certificate (.cer)"
$cerLabel.Font    = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$cerLabel.Location = New-Object System.Drawing.Point(24, 90)
$cerLabel.Size    = New-Object System.Drawing.Size(120, 18)
$form.Controls.Add($cerLabel)

$cerBox           = New-Object System.Windows.Forms.TextBox
$cerBox.Text      = $CerPath
$cerBox.Location  = New-Object System.Drawing.Point(24, 110)
$cerBox.Size      = New-Object System.Drawing.Size(420, 24)
$cerBox.Font      = New-Object System.Drawing.Font("Segoe UI", 9)
$form.Controls.Add($cerBox)

$cerBrowse        = New-Object System.Windows.Forms.Button
$cerBrowse.Text   = "Browse"
$cerBrowse.Location = New-Object System.Drawing.Point(450, 109)
$cerBrowse.Size   = New-Object System.Drawing.Size(80, 26)
$cerBrowse.FlatStyle = 'Flat'
$cerBrowse.Add_Click({
    $dlg = New-Object System.Windows.Forms.OpenFileDialog
    $dlg.Filter = "Certificate files (*.cer)|*.cer|All files (*.*)|*.*"
    if ($dlg.ShowDialog() -eq 'OK') { $cerBox.Text = $dlg.FileName }
})
$form.Controls.Add($cerBrowse)

# MSIX row
$msixLabel        = New-Object System.Windows.Forms.Label
$msixLabel.Text   = "Package (.msix / .msixbundle)"
$msixLabel.Font   = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$msixLabel.Location = New-Object System.Drawing.Point(24, 150)
$msixLabel.Size   = New-Object System.Drawing.Size(220, 18)
$form.Controls.Add($msixLabel)

$msixBox          = New-Object System.Windows.Forms.TextBox
$msixBox.Text     = $MsixPath
$msixBox.Location = New-Object System.Drawing.Point(24, 170)
$msixBox.Size     = New-Object System.Drawing.Size(420, 24)
$msixBox.Font     = New-Object System.Drawing.Font("Segoe UI", 9)
$form.Controls.Add($msixBox)

$msixBrowse       = New-Object System.Windows.Forms.Button
$msixBrowse.Text  = "Browse"
$msixBrowse.Location = New-Object System.Drawing.Point(450, 169)
$msixBrowse.Size  = New-Object System.Drawing.Size(80, 26)
$msixBrowse.FlatStyle = 'Flat'
$msixBrowse.Add_Click({
    $dlg = New-Object System.Windows.Forms.OpenFileDialog
    $dlg.Filter = "MSIX packages (*.msix;*.msixbundle)|*.msix;*.msixbundle|All files (*.*)|*.*"
    if ($dlg.ShowDialog() -eq 'OK') { $msixBox.Text = $dlg.FileName }
})
$form.Controls.Add($msixBrowse)

# Log box
$logBox           = New-Object System.Windows.Forms.RichTextBox
$logBox.Location  = New-Object System.Drawing.Point(24, 210)
$logBox.Size      = New-Object System.Drawing.Size(506, 120)
$logBox.ReadOnly  = $true
$logBox.BackColor = [System.Drawing.Color]::FromArgb(248,249,252)
$logBox.Font      = New-Object System.Drawing.Font("Consolas", 9)
$logBox.BorderStyle = 'FixedSingle'
$form.Controls.Add($logBox)

# Buttons
$installBtn       = New-Object System.Windows.Forms.Button
$installBtn.Text  = "Install"
$installBtn.Location = New-Object System.Drawing.Point(370, 342)
$installBtn.Size  = New-Object System.Drawing.Size(80, 30)
$installBtn.FlatStyle = 'Flat'
$installBtn.BackColor = [System.Drawing.Color]::FromArgb(225,150,60)
$installBtn.ForeColor = [System.Drawing.Color]::White
$installBtn.Font  = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold)
$form.Controls.Add($installBtn)

$closeBtn         = New-Object System.Windows.Forms.Button
$closeBtn.Text    = "Close"
$closeBtn.Location = New-Object System.Drawing.Point(456, 342)
$closeBtn.Size    = New-Object System.Drawing.Size(75, 30)
$closeBtn.FlatStyle = 'Flat'
$closeBtn.Add_Click({ $form.Close() })
$form.Controls.Add($closeBtn)

# Logger writing into the RichTextBox with color levels
$logger = {
    param($msg, $level)
    $color = switch ($level) {
        'success' { [System.Drawing.Color]::FromArgb(20,140,60) }
        'error'   { [System.Drawing.Color]::FromArgb(190,40,40) }
        default   { [System.Drawing.Color]::FromArgb(40,80,140) }
    }
    $logBox.SelectionStart  = $logBox.TextLength
    $logBox.SelectionLength = 0
    $logBox.SelectionColor  = $color
    $logBox.AppendText("[$([DateTime]::Now.ToString('HH:mm:ss'))] $msg`r`n")
    $logBox.SelectionColor  = $logBox.ForeColor
    $logBox.ScrollToCaret()
    [System.Windows.Forms.Application]::DoEvents()
}

$installBtn.Add_Click({
    $installBtn.Enabled = $false
    $logBox.Clear()
    try {
        Invoke-Install -Cer $cerBox.Text -Msix $msixBox.Text -Log $logger
        [void][System.Windows.Forms.MessageBox]::Show(
            "Installation complete. Launch 'Giwu HRMS' from the Start menu.",
            "Giwu HRMS Installer",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Information)
    } catch {
        & $logger $_.Exception.Message 'error'
        [void][System.Windows.Forms.MessageBox]::Show(
            $_.Exception.Message,
            "Installation failed",
            [System.Windows.Forms.MessageBoxButtons]::OK,
            [System.Windows.Forms.MessageBoxIcon]::Error)
    } finally {
        $installBtn.Enabled = $true
    }
})

[void]$form.ShowDialog()
