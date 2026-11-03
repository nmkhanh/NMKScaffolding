$certName = "CN=BIMLEAD CodeSigning"
$existing = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $certName }
if (-not $existing) {
    Write-Host "Creating Self-Signed CodeSigning Certificate in My..."
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject $certName -CertStoreLocation "Cert:\CurrentUser\My" -NotAfter (Get-Date).AddYears(5)
} else {
    $cert = $existing[0]
}

Write-Host "Adding to TrustedPublisher..."
$pubStore = New-Object System.Security.Cryptography.X509Certificates.X509Store("TrustedPublisher", "CurrentUser")
$pubStore.Open("ReadWrite")
$pubStore.Add($cert)
$pubStore.Close()

$dll = "C:\ProgramData\Autodesk\Revit\Addins\2022\RevitFamilyMcpBridge\RevitFamilyMcpBridge.dll"
Write-Host "Signing $dll with certificate..."
$sig = Set-AuthenticodeSignature -FilePath $dll -Certificate $cert
Write-Host "Signature Status: $($sig.Status)"
