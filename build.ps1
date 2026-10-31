param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$frameworkCsc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path -LiteralPath $frameworkCsc)) {
    $frameworkCsc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path -LiteralPath $frameworkCsc)) {
    throw "找不到 Windows 自带的 C# 编译器 csc.exe。"
}

$iconGenerator = Join-Path $projectRoot "IconGenerator.exe"
$exe = Join-Path $projectRoot "NetInitTool.exe"
$icon = Join-Path $projectRoot "NetInitTool.ico"
$source = Join-Path $projectRoot "Program.cs"
$manifest = Join-Path $projectRoot "app.manifest"

& $frameworkCsc /nologo /target:exe /out:$iconGenerator /r:System.dll /r:System.Drawing.dll (Join-Path $projectRoot "IconGenerator.cs")
if ($LASTEXITCODE -ne 0) { throw "图标生成失败。" }
& $iconGenerator $icon
if ($LASTEXITCODE -ne 0) { throw "图标文件生成失败。" }
& $frameworkCsc /nologo /target:winexe /platform:anycpu /optimize+ /out:$exe /win32icon:$icon /win32manifest:$manifest /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll $source
if ($LASTEXITCODE -ne 0) { throw "主程序编译失败。" }

Write-Host "已生成：$exe"
