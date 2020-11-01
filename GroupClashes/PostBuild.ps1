param ($Configuration, $TargetName, $ProjectDir, $TargetPath)
write-host $Configuration
write-host $TargetName
write-host $ProjectDir
write-host $TargetPath


## powershell -ExecutionPolicy Unrestricted $(ProjectDir)PostBuild.ps1 -Configuration $(Configuration) -TargetName $(TargetName) -ProjectDir $(ProjectDir) -TargetPath $(TargetPath)

if ($Configuration -eq "Debug") {
    IF (Test-Path "C:\Program Files\Autodesk\Navisworks Manage 2021\Plugins\$TargetName\") {
      Remove-Item ("C:\Program Files\Autodesk\Navisworks Manage 2021\Plugins\" + $TargetName + "\") -Recurse
    }
    xcopy /Y "$TargetPath" ("C:\Program Files\Autodesk\Navisworks Manage 2021\Plugins\" + $TargetName + "\")
    ## mkdir ("C:\Program Files\Autodesk\Navisworks Manage 2021\Plugins\" + $TargetName + "\en-US")
    xcopy /Y ($ProjectDir + "en-US\*.*") ("C:\Program Files\Autodesk\Navisworks Manage 2021\Plugins\" + $TargetName + "\en-US\")
    xcopy /Y ($ProjectDir + "Images\*.*") ("C:\Program Files\Autodesk\Navisworks Manage 2021\Plugins\" + $TargetName + "\Images\")
    xcopy /Y ($ProjectDir + "Help\*.*") ("C:\Program Files\Autodesk\Navisworks Manage 2021\Plugins\" + $TargetName + "\Help\")
    xcopy /Y ($ProjectDir + "Help\Resources\*.*") ("C:\Program Files\Autodesk\Navisworks Manage 2021\Plugins\" + $TargetName + "\Help\Resources\")
} else {
    $releaseFolder="G:\My Drive\05 - Travail\Revit Dev\GroupClashes\Releases\Current Release\GroupClashes.BM42.bundle"
    IF (Test-Path $releaseFolder) { xcopy /Y ($ProjectDir + "PackageContents.xml") $releaseFolder }
    IF (Test-Path ($releaseFolder + "\Contents\" + $Configuration + "\")) { 
        xcopy /Y $TargetPath  ($releaseFolder + "\Contents\" + $Configuration + "\")
        xcopy /Y ($ProjectDir + "en-US\*.*")  ($releaseFolder + "\Contents\$Configuration\en-US\")
        xcopy /Y ($ProjectDir + "Images\*.*") ($releaseFolder + "\Contents\$Configuration\Images\") 
        xcopy /Y ($ProjectDir + "Help\*.*") ($releaseFolder + "\Contents\$Configuration\Help\")
        xcopy /Y ($ProjectDir + "Help\Resources\*.*") ($releaseFolder + "\Contents\$Configuration\Help\Resources\") 
    }
}