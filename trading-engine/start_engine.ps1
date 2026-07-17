# AlgoTrading paper engine launcher — Task Scheduler entry point.
# Starts the confluence paper-trading engine, which runs until the 15:20 IST
# square-off and then exits on its own. All output goes to a daily log file.
$ErrorActionPreference = "Continue"

$root = "g:\PROJECT\BE\AlgoTrading\trading-engine"
Set-Location $root

# Make sure PostgreSQL is up (the task must run with highest privileges for this
# to succeed; harmless no-op if it is already running).
try { Start-Service "postgresql-x64-17" -ErrorAction SilentlyContinue } catch {}
Start-Sleep -Seconds 3

New-Item -ItemType Directory -Force -Path "$root\logs" | Out-Null
$log = "$root\logs\engine_$(Get-Date -Format 'yyyyMMdd').log"

# Force UTF-8 + unbuffered so the log is clean and updates live.
$env:PYTHONIOENCODING = "utf-8"
$env:PYTHONUNBUFFERED = "1"

Add-Content -Path $log -Encoding utf8 -Value "==== engine start $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===="

# Run via cmd so redirection writes the engine's raw UTF-8 bytes (PowerShell's own
# *>> operator rewrites everything as UTF-16, which garbles the log).
cmd /c "`"$root\.venv\Scripts\python.exe`" `"$root\run_paper.py`" >> `"$log`" 2>&1"

Add-Content -Path $log -Encoding utf8 -Value "==== engine end   $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===="
