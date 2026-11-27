param([int]$DelayMs = 75, [int]$SnapshotLossPercent = 3, [int]$JitterMs = 20)
$ErrorActionPreference = 'Stop'
$project = 'D:\67\Last67Seconds-Maps'
$exe = Join-Path $project 'Builds\CoopTest\CindersCoopTest.exe'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$reports = Join-Path $project "AgentScripts\_bridge\latency-$stamp"
New-Item -ItemType Directory -Path $reports | Out-Null
$room = 'NET-' + [Guid]::NewGuid().ToString('N').Substring(0, 8).ToUpperInvariant()
$processIds = @()
for ($i = 0; $i -lt 4; $i++) {
    $role = if ($i -eq 0) { 'host' } else { 'client' }
    $report = Join-Path $reports "player-$i.txt"
    $log = Join-Path $reports "player-$i.log"
    $arguments = "-batchmode -nographics -coop-latency $role $room `"$report`" -probe-delay $DelayMs -probe-loss $SnapshotLossPercent -probe-jitter $JitterMs -logFile `"$log`""
    $proc = Start-Process -FilePath $exe -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $processIds += $proc.Id
}
@{ room = $room; processes = $processIds; reports = $reports; delayPerDirectionMs = $DelayMs; snapshotLossPercent = $SnapshotLossPercent; jitterMs = $JitterMs } | ConvertTo-Json | Set-Content (Join-Path $reports 'run.json')
Get-Content (Join-Path $reports 'run.json')
