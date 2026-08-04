param ($VersionSoftware, $Configuration, $TargetName, $ProjectDir, $TargetPath, $TargetDir)
write-host $VersionSoftware
write-host $Configuration
write-host $TargetName
write-host $ProjectDir
write-host $TargetPath
write-host $TargetDir

$filePaths = @()

# Function to create folder and copy files
function CreateAndCopyContent {
    param (
        [string]$VersionSoftware,
        [string]$ProjectDir,
        [string]$TargetDir,
        [string]$AddinMainFolder
    )

    # Define the target folder path
    $contentFolder = Join-Path -Path $AddinMainFolder -ChildPath ("Contents\" + $VersionSoftware)

    # Create the folder if it doesn't exist
    if (-Not (Test-Path -Path $contentFolder)) {
        New-Item -ItemType Directory -Path $contentFolder | Out-Null
        Write-Host "Created folder: $contentFolder"
    } else {
        # Clear the folder's contents
        Remove-Item -Path "$contentFolder\*" -Recurse -Force
        Write-Host "Cleared contents of folder: $contentFolder"
    }

    # Copy entire build content to Contents\$VersionSoftware
    Copy-Item -Path $TargetDir\* -Destination $contentFolder -Recurse -Force
    Write-Host "Copied build content to $contentFolder"
}

function SignFiles($TargetDir) {
    Get-ChildItem ($TargetDir) -Recurse | Where-Object { $_.extension -in ".exe", ".dll" } |
    Foreach-Object {
        $signature = Get-AuthenticodeSignature -FilePath $_.FullName

        if ($signature.status -ne "Valid") {
            # $filePaths = $filePaths + "`'" + $_.FullName +"`'" + " "
            $filePaths += $_.FullName
        }
    }
    Write-Host $filePaths

    azuresigntool sign -du "https://www.eai.fr" `
      -fd sha384 -kvu https://kv-eai-util-fr-signing.vault.azure.net/ `
      -kvm `
      -kvc EAICodeSigningCertificate `
      -tr http://timestamp.digicert.com `
      -td sha384 `
      -v $filePaths
}

if ($Configuration -eq "Debug") {

    SignFiles $TargetDir
    # create the addin folder and copy files
    $addinMainFolder = ($env:APPDATA + "\Autodesk\ApplicationPlugins\GroupClashes.BM42.bundle\")

    # Create the main addin folder if it doesn't exist
    if (-Not (Test-Path -Path $addinMainFolder)) {
        New-Item -ItemType Directory -Path $addinMainFolder | Out-Null
        Write-Host "Created folder: $addinMainFolder"
    }

    # Copy PackageContents.xml to Contents folder
    $packageContentsSource = Join-Path -Path $ProjectDir -ChildPath "PackageContents.xml"
    $packageContentsTarget = Join-Path -Path $addinMainFolder -ChildPath "PackageContents.xml"
    Copy-Item -Path $packageContentsSource -Destination $packageContentsTarget -Force
    Write-Host "Copied PackageContents.xml to Contents folder"

    # Call the function to create folder and copy files
    CreateAndCopyContent -VersionSoftware $VersionSoftware -ProjectDir $ProjectDir -TargetDir $TargetDir -AddinMainFolder $addinMainFolder
}

