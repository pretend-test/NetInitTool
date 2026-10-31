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
