using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace NetInitTool
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private const string InternetSettingsPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
        private const int InternetOptionRefresh = 37;
        private const int InternetOptionSettingsChanged = 39;
        private const int HwndBroadcast = 0xffff;
        private const uint WmSettingChange = 0x001a;
        private const uint SmtoAbortIfHung = 0x0002;

        private CheckBox _proxyCheckBox;
        private CheckBox _winHttpCheckBox;
        private CheckBox _dnsCheckBox;
        private CheckBox _winsockCheckBox;
        private CheckBox _tcpIpCheckBox;
        private CheckBox _renewCheckBox;
        private readonly RichTextBox _logBox;
        private Label _statusLabel;
        private Button _quickButton;
        private Button _runSelectedButton;
        private Button _copyLogButton;
        private readonly NotifyIcon _notifyIcon;
        private readonly ToolTip _toolTip;

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint msg,
            IntPtr wParam,
            string lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        public MainForm()
        {
            Text = "网络初始化工具";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(760, 560);
            Size = new Size(860, 650);
            BackColor = Color.FromArgb(246, 248, 251);
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            Icon = GetApplicationIcon();

            _toolTip = new ToolTip();
            _notifyIcon = new NotifyIcon
            {
                Icon = Icon,
                Text = "网络初始化工具",
                Visible = true
            };
            _notifyIcon.DoubleClick += delegate { ShowFromTray(); };
            _notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            _notifyIcon.ContextMenuStrip.Items.Add("打开工具", null, delegate { ShowFromTray(); });
            _notifyIcon.ContextMenuStrip.Items.Add("退出", null, delegate { Close(); });

            FormClosing += delegate
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(24, 20, 24, 20),
                BackColor = BackColor
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 205F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Controls.Add(root);

            var header = BuildHeader();
            root.Controls.Add(header, 0, 0);

            var options = BuildOptions();
            root.Controls.Add(options, 0, 1);

            var buttons = BuildButtons(out _quickButton, out _runSelectedButton, out _copyLogButton);
            root.Controls.Add(buttons, 0, 2);

            var logPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            _logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(45, 52, 62),
                Font = new Font("Consolas", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
                DetectUrls = false,
                HideSelection = false,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Padding = new Padding(10)
            };
            logPanel.Padding = new Padding(1);
            logPanel.Controls.Add(_logBox);
            root.Controls.Add(logPanel, 0, 3);

            _statusLabel = (Label)buttons.Controls[0];
            AppendLog("网络初始化工具已启动。建议先点击“刷新代理（不重启）”。");
            AppendLog("如仍然无法恢复，再勾选 Winsock 和 TCP/IP 重置后执行；这两项可能需要重启 Windows。", Color.FromArgb(180, 105, 25));
        }

        private Control BuildHeader()
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            var title = new Label
            {
                AutoSize = true,
                Text = "网络初始化工具",
                Font = new Font("Microsoft YaHei UI", 19F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(29, 41, 57),
                Location = new Point(0, 0)
            };
            var subtitle = new Label
            {
                AutoSize = true,
                Text = "代理工具退出后刷新 Windows 网络状态，帮助 Steam 等应用恢复连接。",
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(92, 103, 117),
                Location = new Point(2, 42)
            };
            panel.Controls.Add(title);
            panel.Controls.Add(subtitle);
            return panel;
        }

        private Control BuildOptions()
        {
            var group = new GroupBox
            {
                Dock = DockStyle.Fill,
                Text = "初始化项目",
                ForeColor = Color.FromArgb(47, 57, 70),
                Padding = new Padding(18, 12, 18, 10)
            };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(2, 4, 2, 0)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            for (var i = 0; i < 3; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
            }

            _proxyCheckBox = CreateCheckBox("关闭系统代理并广播刷新", true);
            _winHttpCheckBox = CreateCheckBox("重置 WinHTTP 代理", true);
            _dnsCheckBox = CreateCheckBox("清空 DNS 缓存", true);
            _winsockCheckBox = CreateCheckBox("重置 Winsock（可能需要重启）", false);
            _tcpIpCheckBox = CreateCheckBox("重置 TCP/IP 协议栈（可能需要重启）", false);
            _renewCheckBox = CreateCheckBox("重新获取 IP 地址（会短暂断网）", false);

            _toolTip.SetToolTip(_proxyCheckBox, "修改当前用户的系统代理开关，并通知 Windows 和已监听设置变化的程序。");
            _toolTip.SetToolTip(_winHttpCheckBox, "执行 netsh winhttp reset proxy，清除 WinHTTP 代理。");
            _toolTip.SetToolTip(_dnsCheckBox, "执行 ipconfig /flushdns，清空本机 DNS 缓存。");
            _toolTip.SetToolTip(_winsockCheckBox, "执行 netsh winsock reset，修复 Winsock 目录。通常需要重启才能完全生效。");
            _toolTip.SetToolTip(_tcpIpCheckBox, "执行 netsh int ip reset，重置 TCP/IP 参数。通常需要重启才能完全生效。");
            _toolTip.SetToolTip(_renewCheckBox, "执行 ipconfig /release 和 ipconfig /renew，适合 DHCP 地址异常的情况。");

            table.Controls.Add(_proxyCheckBox, 0, 0);
            table.Controls.Add(_winHttpCheckBox, 1, 0);
            table.Controls.Add(_dnsCheckBox, 0, 1);
            table.Controls.Add(_winsockCheckBox, 1, 1);
            table.Controls.Add(_tcpIpCheckBox, 0, 2);
            table.Controls.Add(_renewCheckBox, 1, 2);
            group.Controls.Add(table);
            return group;
        }

        private CheckBox CreateCheckBox(string text, bool isChecked)
        {
            return new CheckBox
            {
                AutoSize = true,
                Text = text,
                Checked = isChecked,
                Anchor = AnchorStyles.Left,
                ForeColor = Color.FromArgb(54, 65, 80),
                Margin = new Padding(4, 5, 4, 4)
            };
        }

        private Control BuildButtons(out Button quickButton, out Button runSelectedButton, out Button copyLogButton)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));

            var status = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Text = "就绪",
                ForeColor = Color.FromArgb(42, 132, 83),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                Margin = new Padding(2, 4, 0, 0)
            };
            panel.Controls.Add(status, 0, 0);

            quickButton = CreateButton("刷新代理（不重启）", Color.FromArgb(31, 111, 235));
            quickButton.Click += async delegate { await ExecuteAsync(false); };
            panel.Controls.Add(quickButton, 1, 0);

            runSelectedButton = CreateButton("执行选中项目", Color.FromArgb(53, 71, 92));
            runSelectedButton.Click += async delegate { await ExecuteAsync(true); };
            panel.Controls.Add(runSelectedButton, 2, 0);

            copyLogButton = CreateButton("复制日志", Color.FromArgb(112, 123, 138));
            copyLogButton.Click += delegate
            {
                if (!string.IsNullOrWhiteSpace(_logBox.Text))
                {
                    Clipboard.SetText(_logBox.Text);
                    SetStatus("日志已复制", Color.FromArgb(42, 132, 83));
                }
            };
            panel.Controls.Add(copyLogButton, 3, 0);
            return panel;
        }

        private static Button CreateButton(string text, Color backColor)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatAppearance = { BorderSize = 0 },
                Margin = new Padding(5, 2, 0, 2),
                Cursor = Cursors.Hand
            };
            return button;
        }

        private async Task ExecuteAsync(bool runSelected)
        {
            SetButtonsEnabled(false);
            _logBox.Clear();
            SetStatus("正在执行...", Color.FromArgb(31, 111, 235));
            AppendLog(runSelected ? "开始执行选中的初始化项目。" : "开始刷新代理相关设置。", Color.FromArgb(31, 111, 235));

            var steps = new List<Func<Task>>();
            if (_proxyCheckBox.Checked)
            {
                steps.Add(async delegate { await RunStepAsync("刷新系统代理", delegate { return RefreshSystemProxy(); }); });
            }
            if (_winHttpCheckBox.Checked)
            {
                steps.Add(async delegate { await RunCommandStepAsync("重置 WinHTTP 代理", "netsh winhttp reset proxy"); });
            }
            if (_dnsCheckBox.Checked)
            {
                steps.Add(async delegate { await RunCommandStepAsync("清空 DNS 缓存", "ipconfig /flushdns"); });
            }
            if (runSelected && _winsockCheckBox.Checked)
            {
                steps.Add(async delegate { await RunCommandStepAsync("重置 Winsock", "netsh winsock reset"); });
            }
            if (runSelected && _tcpIpCheckBox.Checked)
            {
                steps.Add(async delegate { await RunCommandStepAsync("重置 TCP/IP", "netsh int ip reset"); });
            }
            if (runSelected && _renewCheckBox.Checked)
            {
                steps.Add(async delegate { await RunCommandStepAsync("释放 IP 地址", "ipconfig /release"); });
                steps.Add(async delegate { await RunCommandStepAsync("重新获取 IP 地址", "ipconfig /renew"); });
            }

            if (steps.Count == 0)
            {
                AppendLog("没有选中任何项目。", Color.FromArgb(180, 105, 25));
                SetStatus("没有可执行项目", Color.FromArgb(180, 105, 25));
                SetButtonsEnabled(true);
                return;
            }

            var failed = false;
            foreach (var step in steps)
            {
                try
                {
                    await step();
                }
                catch (Exception ex)
                {
                    failed = true;
                    AppendLog("异常：" + ex.Message, Color.FromArgb(185, 55, 55));
                }
            }

            if (failed)
            {
                SetStatus("执行完成，但有项目失败", Color.FromArgb(185, 55, 55));
                AppendLog("请查看上面的错误信息；部分命令可能被安全软件或策略拦截。", Color.FromArgb(185, 55, 55));
            }
            else
            {
                SetStatus("执行完成", Color.FromArgb(42, 132, 83));
                AppendLog("执行完成。建议重新打开 Steam 或点击 Steam 的“重新连接”。", Color.FromArgb(42, 132, 83));
            }
            SetButtonsEnabled(true);
        }

        private async Task RunStepAsync(string title, Func<string> action)
        {
            AppendLog("[开始] " + title, Color.FromArgb(54, 65, 80));
            var result = await Task.Run(action);
            AppendLog(result, result.StartsWith("[成功]") ? Color.FromArgb(42, 132, 83) : Color.FromArgb(185, 55, 55));
        }

        private async Task RunCommandStepAsync(string title, string command)
        {
            AppendLog("[开始] " + title + "  >  " + command, Color.FromArgb(54, 65, 80));
            var result = await RunCommandAsync(command);
            var color = result.ExitCode == 0 ? Color.FromArgb(42, 132, 83) : Color.FromArgb(185, 55, 55);
            AppendLog("[" + (result.ExitCode == 0 ? "成功" : "失败") + "] " + title + "，退出码：" + result.ExitCode, color);
            if (!string.IsNullOrWhiteSpace(result.Output))
            {
                AppendLog(result.Output.TrimEnd(), Color.FromArgb(92, 103, 117));
            }
            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                AppendLog(result.Error.TrimEnd(), Color.FromArgb(185, 55, 55));
            }
        }

        private static string RefreshSystemProxy()
        {
            int oldValue = -1;
            using (var key = Registry.CurrentUser.CreateSubKey(InternetSettingsPath))
            {
                if (key == null)
                {
                    return "[失败] 无法打开当前用户的 Internet Settings 注册表项。";
                }
                var old = key.GetValue("ProxyEnable");
                if (old != null)
                {
                    oldValue = Convert.ToInt32(old);
                }
                key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
            }

            var settingsChanged = InternetSetOption(IntPtr.Zero, InternetOptionSettingsChanged, IntPtr.Zero, 0);
            var refreshed = InternetSetOption(IntPtr.Zero, InternetOptionRefresh, IntPtr.Zero, 0);
            IntPtr result;
            var broadcasted = SendMessageTimeout(
                new IntPtr(HwndBroadcast),
                WmSettingChange,
                IntPtr.Zero,
                "Internet Settings",
                SmtoAbortIfHung,
                2000,
                out result) != IntPtr.Zero;

            return "[成功] 已将系统代理从 " + (oldValue < 0 ? "未设置" : (oldValue == 0 ? "关闭" : "开启")) +
                   " 调整为关闭，并通知 Windows 刷新代理设置。" +
                   Environment.NewLine +
                   "WinINet 更新：" + (settingsChanged ? "成功" : "失败") +
                   "；刷新：" + (refreshed ? "成功" : "失败") +
                   "；广播：" + (broadcasted ? "成功" : "超时或无响应");
        }

        private static async Task<CommandResult> RunCommandAsync(string command)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/d /c " + command,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.Default,
                StandardErrorEncoding = Encoding.Default,
                WorkingDirectory = Environment.SystemDirectory
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                await Task.Run(delegate { process.WaitForExit(); });
                await Task.WhenAll(outputTask, errorTask);
                return new CommandResult(process.ExitCode, outputTask.Result, errorTask.Result);
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _quickButton.Enabled = enabled;
            _runSelectedButton.Enabled = enabled;
            _copyLogButton.Enabled = enabled;
            _proxyCheckBox.Enabled = enabled;
            _winHttpCheckBox.Enabled = enabled;
            _dnsCheckBox.Enabled = enabled;
            _winsockCheckBox.Enabled = enabled;
            _tcpIpCheckBox.Enabled = enabled;
            _renewCheckBox.Enabled = enabled;
            UseWaitCursor = !enabled;
        }

        private void SetStatus(string text, Color color)
        {
            _statusLabel.Text = text;
            _statusLabel.ForeColor = color;
        }

        private void AppendLog(string message, Color? color = null)
        {
            var line = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine;
            var start = _logBox.TextLength;
            _logBox.AppendText(line);
            _logBox.Select(start, line.Length);
            _logBox.SelectionColor = color ?? Color.FromArgb(45, 52, 62);
            _logBox.SelectionLength = 0;
            _logBox.ScrollToCaret();
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private static Icon GetApplicationIcon()
        {
            try
            {
                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private sealed class CommandResult
        {
            public CommandResult(int exitCode, string output, string error)
            {
                ExitCode = exitCode;
                Output = output;
                Error = error;
            }

            public int ExitCode { get; private set; }
            public string Output { get; private set; }
            public string Error { get; private set; }
        }
    }
}


