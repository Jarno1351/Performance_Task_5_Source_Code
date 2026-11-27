@echo off
setlocal
if not exist "%~dp0HubLink-Service\HubLink.Api.exe" (
    echo HubLink.Api.exe was not found. Extract the entire ZIP first.
    pause
    exit /b 1
)
echo Starting HubLink on http://127.0.0.1:5080
echo Keep this window open. Press Ctrl+C or close it to stop the service.
echo If port 5080 is occupied, use Start-HubLink-5081.cmd instead.
echo.
pushd "%~dp0HubLink-Service"
"%~dp0HubLink-Service\HubLink.Api.exe" --urls http://127.0.0.1:5080
popd
echo.
echo HubLink has stopped. If it did not start, check the message above.
pause
