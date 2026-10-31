# 网络初始化工具

这是一个 Windows C# WinForms 小工具，针对“代理软件关闭系统代理后，Steam 仍然没有恢复连接”的情况，提供一个快速的本机网络状态刷新入口。

## 功能

- 关闭当前用户的系统代理，并通过 WinINet 和 Windows 设置广播刷新代理状态
- 执行 `netsh winhttp reset proxy`
- 执行 `ipconfig /flushdns`
- 可选执行 `netsh winsock reset`
- 可选执行 `netsh int ip reset`
- 可选执行 `ipconfig /release` 和 `ipconfig /renew`
- 托盘图标、实时命令输出和日志复制

程序清单声明了 `requireAdministrator`，启动时会弹出 UAC。Winsock 和 TCP/IP 重置可能仍然需要重启 Windows 才能完全生效；程序不会自动重启电脑。

## 使用建议

1. 代理工具退出后，先点击“刷新代理（不重启）”。
2. 等待几秒，然后重新打开 Steam，或在 Steam 中点击重新连接。
3. 如果仍然不通，再勾选 Winsock 和 TCP/IP 重置，点击“执行选中项目”。
4. 查看窗口日志；失败命令会显示退出码和错误输出。

## 编译

如果使用 Visual Studio，可创建 .NET Framework 4.8 WinForms 项目并替换源码。当前发布包使用 Windows 自带的 .NET Framework C# 编译器生成，适合 Windows 10/11。

---

# Network Initialization Tool

A small Windows C# WinForms utility for refreshing local network settings after a proxy application has been closed but Windows or Steam has not fully refreshed its connection state.

This tool is useful when Steam disconnects after a system proxy is disabled and only restarting Windows appears to restore the connection.

## Features

- Disables the current user's WinINet system proxy and broadcasts a Windows settings refresh
- Resets the WinHTTP proxy with `netsh winhttp reset proxy`
- Clears the local DNS cache with `ipconfig /flushdns`
- Optionally resets the Winsock catalog with `netsh winsock reset`
- Optionally resets the TCP/IP stack with `netsh int ip reset`
- Optionally releases and renews the DHCP address with `ipconfig /release` and `ipconfig /renew`
- System tray icon, live command output, and log copying

The application manifest requests administrator privileges, so Windows displays a UAC prompt when the application starts. Winsock and TCP/IP resets may still require a Windows restart to take full effect. The tool does not restart the computer automatically.

## Recommended usage

1. Close your proxy application.
2. Start `NetInitTool.exe` and accept the administrator prompt.
3. Click **Refresh Proxy (No Restart)**.
4. Wait a few seconds, then reopen Steam or use Steam's reconnect option.
5. If the connection is still unavailable, select the Winsock and TCP/IP reset options and click **Run Selected Items**.
6. Review the command log. Failed commands show their exit codes and error output.

The quick refresh action runs the proxy refresh, WinHTTP proxy reset, and DNS cache flush. The more disruptive Winsock, TCP/IP, and DHCP options are available separately so they do not run on every quick refresh.

## Requirements

- Windows 10 or Windows 11
- .NET Framework 4.8 for building and running the WinForms application
- Administrator privileges for network reset commands

## Build from source

The repository includes a Visual Studio project file and a PowerShell build script. To build with Visual Studio, open `NetInitTool.csproj` and build the project as a .NET Framework 4.8 WinForms application.

To build with the C# compiler included with Windows, run this from the repository directory:

```powershell
.\build.ps1
```

The build script generates the application icon and compiles `NetInitTool.exe`. The prebuilt executable is available in `dist\NetInitTool.exe`.
