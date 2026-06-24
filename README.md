# TipMolde_Frontend

## GitHub Actions release pipeline

This frontend is built by `.github/workflows/tipmolde-release.yml`.
It is meant to be run manually from GitHub Actions when you want to generate installable builds.

It creates two independent jobs:

1. `build-windows`, which publishes the Windows package for `TipMolde`.
2. `build-android`, which publishes the Android APK for `TipMolde`.

### Secrets you need in GitHub

Create these repository secrets in `Settings > Secrets and variables > Actions`:

1. `WINDOWS_PFX_BASE64`
2. `WINDOWS_PFX_PASSWORD`
3. `ANDROID_KEYSTORE_BASE64`
4. `ANDROID_KEYSTORE_PASSWORD`

### Where those values come from

1. `WINDOWS_PFX_BASE64`
   - Export or create a code-signing certificate as a `.pfx`.
   - Convert the `.pfx` file to Base64 and store the text in this secret.

2. `WINDOWS_PFX_PASSWORD`
   - The password used when exporting the Windows `.pfx`.

3. `ANDROID_KEYSTORE_BASE64`
   - Create an Android keystore or `.jks` file.
   - Convert it to Base64 and store the text in this secret.

4. `ANDROID_KEYSTORE_PASSWORD`
   - The password of the Android keystore.
   - In this workflow it is also used as the key password.

### How to create the files

Windows PFX example in PowerShell:

```powershell
$cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=TipMolde" -CertStoreLocation "Cert:\CurrentUser\My"
$pwd = ConvertTo-SecureString "YourStrongPasswordHere" -AsPlainText -Force
Export-PfxCertificate -Cert $cert -FilePath .\windows-cert.pfx -Password $pwd
```

The workflow expects the certificate subject to be `CN=TipMolde`.
If you use a different subject, update the workflow to match it.

Android keystore example with `keytool`:

```powershell
keytool -genkeypair -v -keystore android.keystore -alias ghactionskey -keyalg RSA -keysize 2048 -validity 10000
```

### How to convert them to Base64

PowerShell:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes(".\windows-cert.pfx")) | Set-Content .\windows-cert.txt
[Convert]::ToBase64String([IO.File]::ReadAllBytes(".\android.keystore")) | Set-Content .\android-keystore.txt
```

Then copy the text inside those `.txt` files into GitHub Secrets.

### Important

The workflow uses the Android alias `ghactionskey`.
If you generate the keystore with a different alias, update the workflow or use the same alias when creating the keystore.
