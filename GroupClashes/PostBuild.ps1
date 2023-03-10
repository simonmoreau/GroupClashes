param ($Configuration, $TargetName, $ProjectDir, $TargetPath, $TargetDir)
write-host $Configuration
write-host $TargetName
write-host $ProjectDir
write-host $TargetPath
write-host $TargetDir

# sign the dll
$thumbPrint = "e729567d4e9be8ffca04179e3375b7669bccf272"
$cert=Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert | Where { $_.Thumbprint -eq $thumbPrint}

Set-AuthenticodeSignature -FilePath $TargetPath -Certificate $cert -IncludeChain All -TimestampServer "http://timestamp.comodoca.com/authenticode"

function CopyToFolder($revitVersion, $addinFolder) {

    if (Test-Path $addinFolder) {
        try {
            # Remove previous versions
            Get-ChildItem -Path $addinFolder | Remove-Item -Recurse
            
            # Copy the addin file
            Write-Host "copy all files" + ($TargetDir) + $addinFolder
            xcopy ($TargetDir) ($addinFolder) /s /e /y
        }
        catch {
            Write-Host "An error occurred:"
            Write-Host $_
        }
    }
}


$revitVersion = $Configuration.replace('Debug','').replace('Release','')

# Copy to Addin folder for debug
$addinMainFolder = ($env:APPDATA + "\Autodesk\ApplicationPlugins\GroupClashes.BM42.bundle\")
xcopy /Y ($ProjectDir + "PackageContents.xml") $addinMainFolder
$addinFolder = ($addinMainFolder + "Contents\" + $revitVersion + "\")
Write-Host "addin folder" + $addinFolder
CopyToFolder $revitVersion $addinFolder


# Copy to release folder for building the package
$ReleasePath="G:\My Drive\05 - Travail\Revit Dev\GroupClashes\Releases\Current Release\GroupClashes.BM42.bundle\"
xcopy /Y ($ProjectDir + "PackageContents.xml") $ReleasePath
$releaseFolder = ($ReleasePath + "Contents\" + $revitVersion + "\")
Write-Host "release folder" + $releaseFolder
CopyToFolder $revitVersion $releaseFolder


## Zip the package

$BundleFolder = (get-item $ReleasePath ).parent.FullName

$ReleaseZip = ($BundleFolder + "\" + $TargetName + ".zip")
if (Test-Path $ReleaseZip) { Remove-Item $ReleaseZip }

if ( Test-Path -Path $ReleasePath ) {
  7z a -tzip $ReleaseZip ($BundleFolder + "\GroupClashes.BM42.bundle\")
}