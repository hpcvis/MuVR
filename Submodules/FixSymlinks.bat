REM delete the old links/files if links haven't been setup yet
rmdir "..\Assets\Thirdparty\FishNet"
IF exist "..\Assets\Thirdparty\FishNet" del "..\Assets\Thirdparty\FishNet"			REM delete it as a file if a folder doesn't yet exist
rmdir "..\Assets\Thirdparty\UltimateXR"
IF exist "..\Assets\Thirdparty\UltimateXR" del "..\Assets\Thirdparty\UltimateXR"		REM delete it as a file if a folder doesn't yet exist

REM create new links
mklink /d "..\Assets\Thirdparty\FishNet" "..\..\Submodules\FishNet\Assets\FishNet" 
mklink /d "..\Assets\Thirdparty\UltimateXR" "..\..\Submodules\UltimateXR" 
