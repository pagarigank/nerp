$p = Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess
if ($p) { Stop-Process -Id $p -Force }
Start-Sleep -Seconds 2
$env:ASPNETCORE_ENVIRONMENT = 'Development'
Start-Process -FilePath 'C:\Program Files\dotnet\dotnet.exe' -ArgumentList 'run --project D:\nerp\src\ERP.Api\ERP.Api.csproj --no-build --no-launch-profile' -WorkingDirectory 'D:\nerp' -WindowStyle Hidden -RedirectStandardOutput 'D:\nerp\api_stdout.log' -RedirectStandardError 'D:\nerp\api_stderr.log'
Write-Output 'restarted'
