param(
    [ValidateRange(5,60)][int]$Seconds = 15,
    [string]$OutputPath,
    [switch]$SelfTest
)
$ErrorActionPreference = 'Stop'
function Get-CpuCapacityPercent([double]$DeltaSeconds,[double]$ElapsedSeconds,[int]$LogicalCores) {
    if ($ElapsedSeconds -le 0 -or $LogicalCores -lt 1) { throw 'Intervalo ou quantidade de processadores inválida.' }
    if ($DeltaSeconds -lt 0) { return $null }
    return [Math]::Round([Math]::Min(100,100*$DeltaSeconds/$ElapsedSeconds/$LogicalCores),2)
}
if ($SelfTest) {
    $cases = @(@(1,1,4,25),@(4,1,4,100),@(0,15,4,0),@(2,2,4,25))
    foreach($case in $cases) {
        if ((Get-CpuCapacityPercent $case[0] $case[1] $case[2]) -ne $case[3]) { throw 'Cálculo de CPU incorreto.' }
    }
    if ($null -ne (Get-CpuCapacityPercent -1 1 4)) { throw 'Delta negativo deve ser desconhecido.' }
    $rejected=$false;try {Get-CpuCapacityPercent 1 0 4 | Out-Null} catch {$rejected=$true}
    if (-not $rejected) {throw 'Intervalo zero deve ser rejeitado.'}
    Write-Output 'PASS 6 testes de cálculo/validação; sem leitura de hardware.'
    exit 0
}
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'Execute no Windows. Este script não altera ajustes do sistema ou do Lunar.' }
if (-not $OutputPath) {
    $desktop=[Environment]::GetFolderPath('DesktopDirectory')
    if (-not $desktop) {$desktop=$PSScriptRoot}
    $OutputPath=Join-Path $desktop ('Diagnostico-Lunar-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.json')
}
$os=Get-CimInstance Win32_OperatingSystem
$cpu=Get-CimInstance Win32_Processor | Select-Object -First 1
$gpu=@(Get-CimInstance Win32_VideoController | Select-Object Name,DriverVersion,CurrentHorizontalResolution,CurrentVerticalResolution,CurrentRefreshRate)
$totalMemory=(Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory
$cores=[Environment]::ProcessorCount
$before=@{}
$names=@('java','javaw','Lunar Client','LunarClient')
foreach($p in @(Get-Process -Name $names -ErrorAction SilentlyContinue)) {
    try {$before[$p.Id]=[double]$p.TotalProcessorTime.TotalSeconds} catch { }
}
Write-Output "Observando candidatos Java/Lunar por $Seconds segundos. Jogue normalmente; não é uma medição de FPS."
$watch=[Diagnostics.Stopwatch]::StartNew()
Start-Sleep -Seconds $Seconds
$elapsed=$watch.Elapsed.TotalSeconds
$processes=@(foreach($p in @(Get-Process -Name $names -ErrorAction SilentlyContinue)) {
    try {
        $percent=$null
        if ($before.ContainsKey($p.Id)) {$percent=Get-CpuCapacityPercent ($p.TotalProcessorTime.TotalSeconds-$before[$p.Id]) $elapsed $cores}
        [pscustomobject]@{Id=$p.Id;Name=$p.ProcessName;AverageCpuPercentOfTotalCapacity=$percent;PrivateMemoryMiB=[Math]::Round($p.PrivateMemorySize64/1MB,1);WorkingSetMiB=[Math]::Round($p.WorkingSet64/1MB,1)}
    } catch { }
})
$report=[ordered]@{
    SchemaVersion=1;CreatedUtc=[DateTimeOffset]::UtcNow.ToString('o')
    Windows=$os.Caption;Cpu=$cpu.Name;LogicalProcessors=$cores;PhysicalMemoryGiB=[Math]::Round($totalMemory/1GB,2)
    FreeMemoryGiBAtStart=[Math]::Round($os.FreePhysicalMemory*1KB/1GB,2)
    FreeMemoryGiBAtEnd=[Math]::Round((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory*1KB/1GB,2)
    Video=$gpu;SampleSeconds=[Math]::Round($elapsed,2);CandidateProcesses=$processes
    Fps=$null;FrameTimes=$null;NetworkLatency=$null
    Limitations=@('FPS, frametime e ping não foram medidos.','Média de CPU do processo não identifica gargalo de uma thread.','Processos Java podem pertencer a outros programas; não são identificados como Lunar com certeza.','Resolução/Hz vêm do driver; zero ou ausente é desconhecido.','Sem leitura de comandos de processo, caminhos, usuário, servidor, teclas ou trajetórias. Nenhum dado é enviado.','Amostra não diagnostica temperatura, saúde física ou defeito de hardware.')
}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $OutputPath -Encoding UTF8
Write-Output "Relatório local salvo: $OutputPath"
