# Fixes / (re)creates the AlgoTradingEngine scheduled task correctly.
# RUN THIS ONCE FROM AN ADMIN POWERSHELL:
#   powershell -ExecutionPolicy Bypass -File "G:\PROJECT\BE\AlgoTrading\trading-engine\fix_scheduler.ps1"

$ErrorActionPreference = "Stop"

$action = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument '-NoProfile -ExecutionPolicy Bypass -File "G:\PROJECT\BE\AlgoTrading\trading-engine\start_engine.ps1"' `
    -WorkingDirectory "G:\PROJECT\BE\AlgoTrading\trading-engine"

# Weekdays 09:14 IST. Engine self-stops at 15:30 (square-off 15:20, collect to 15:30).
$trigger = New-ScheduledTaskTrigger -Weekly `
    -DaysOfWeek Monday,Tuesday,Wednesday,Thursday,Friday -At 9:14AM

$principal = New-ScheduledTaskPrincipal -UserId "$env:USERNAME" `
    -LogonType Interactive -RunLevel Highest

$settings = New-ScheduledTaskSettingsSet `
    -StartWhenAvailable `
    -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
    -ExecutionTimeLimit (New-TimeSpan -Hours 8) `
    -MultipleInstances IgnoreNew

Register-ScheduledTask -TaskName "AlgoTradingEngine" `
    -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force | Out-Null

Write-Host "AlgoTradingEngine task fixed." -ForegroundColor Green
Write-Host "Action  : powershell.exe -File start_engine.ps1"
Write-Host "Trigger : Mon-Fri 09:14, runs until the engine exits at 15:30"
Write-Host ""
Write-Host "Test it now with:  Start-ScheduledTask -TaskName AlgoTradingEngine"
