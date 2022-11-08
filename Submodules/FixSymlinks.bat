REM delete the old links/files if links haven't been setup yet
rmdir "%~dp0..\Assets\Thirdparty\FishNet"
IF exist "%~dp0..\Assets\Thirdparty\FishNet" del "%~dp0..\Assets\Thirdparty\FishNet"			REM delete it as a file if a folder doesn't yet exist
rmdir "%~dp0..\Assets\Thirdparty\UltimateXR"
IF exist "%~dp0..\Assets\Thirdparty\UltimateXR" del "%~dp0..\Assets\Thirdparty\UltimateXR"		REM delete it as a file if a folder doesn't yet exist

REM create new links
mklink /D "%~dp0..\Assets\Thirdparty\FishNet" "%~dp0\FishNet\Assets\FishNet" 
mklink /D "%~dp0..\Assets\Thirdparty\UltimateXR" "%~dp0\UltimateXR" 
