using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Windows.Forms;

namespace JIYU_killer
{
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private bool isAdmin;
        private System.Timers.Timer killTimer;

        // P/Invoke 声明
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern IntPtr CreateService(IntPtr hSCManager, string lpServiceName, string lpDisplayName, uint dwDesiredAccess, uint dwServiceType, uint dwStartType, uint dwErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern IntPtr OpenSCManager(string lpMachineName, string lpDatabaseName, uint dwDesiredAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool StartService(IntPtr hService, int dwNumServiceArgs, string[] lpServiceArgVectors);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool DeleteService(IntPtr hService);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool QueryServiceStatus(IntPtr hService, ref SERVICE_STATUS lpServiceStatus);

        // 常量
        private const uint SC_MANAGER_CREATE_SERVICE = 0x0002;
        private const uint SERVICE_QUERY_STATUS = 0x0004;
        private const uint SERVICE_START = 0x0010;
        private const uint SERVICE_STOP = 0x0020;
        private const uint SERVICE_DELETE = 0x0010000;
        private const uint SERVICE_KERNEL_DRIVER = 0x00000001;
        private const uint SERVICE_BOOT_START = 0x00000000;
        private const uint SERVICE_ERROR_IGNORE = 0x00000000;
        private const uint SERVICE_RUNNING = 0x00000004;
        private const uint SERVICE_STOPPED = 0x00000001;

        // 服务状态结构体
        [StructLayout(LayoutKind.Sequential)]
        private struct SERVICE_STATUS
        {
            public uint dwServiceType;
            public uint dwCurrentState;
            public uint dwControlsAccepted;
            public uint dwWin32ExitCode;
            public uint dwServiceSpecificExitCode;
            public uint dwCheckPoint;
            public uint dwWaitHint;
        }

        public TrayApplicationContext()
        {
            try
            {
                WriteLog("程序启动");
                
                InitializeTrayIcon();
                WriteLog("托盘图标初始化完成");
                
                CheckAdminPrivileges();
                WriteLog($"管理员权限: {isAdmin}");
                
                StartAppropriateMode();
                WriteLog("模式启动完成");
            }
            catch (Exception ex)
            {
                WriteLog($"启动错误: {ex.Message}");
                WriteLog($"堆栈跟踪: {ex.StackTrace}");
                MessageBox.Show($"程序启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void WriteLog(string message)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jiyu_killer.log");
                using (StreamWriter writer = new StreamWriter(logPath, true))
                {
                    writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
                }
            }
            catch { }
        }



        private void CheckAdminPrivileges()
        {
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
        }

        private void StartAppropriateMode()
        {
            string tooltip = "学生：000\u000B已连接到：0.0.0.0";
            if (isAdmin)
            {
                trayIcon.Text = tooltip;
                WriteLog("运行在管理员模式");
                RunAdminMode();
            }
            else
            {
                trayIcon.Text = tooltip;
                WriteLog("运行在普通模式");
                RunNonAdminMode();
            }
        }

        private void RunAdminMode()
        {
            InstallCertificate();
            LoadKernelDriver();
            KillJiyuProcesses();
            
            // 管理员模式下也定时查杀进程
            killTimer = new System.Timers.Timer(3000);
            killTimer.AutoReset = true;
            killTimer.Elapsed += (sender, e) => KillJiyuProcesses();
            killTimer.Start();
            WriteLog("管理员模式定时查杀定时器已启动");
        }

        private void RunNonAdminMode()
        {
            killTimer = new System.Timers.Timer(3000);
            killTimer.AutoReset = true;
            killTimer.Elapsed += (sender, e) => KillJiyuProcesses();
            killTimer.Start();
            WriteLog("定时查杀定时器已启动");
        }

        private void InstallCertificate()
        {
            try
            {
                string certPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jiyu.cer");
                if (File.Exists(certPath))
                {
                    X509Certificate2 cert = new X509Certificate2(certPath);
                    X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(cert);
                    store.Close();
                    WriteLog("证书安装成功");
                }
                else
                {
                    WriteLog("证书文件不存在，跳过安装");
                }
            }
            catch (Exception ex)
            {
                WriteLog($"证书安装失败: {ex.Message}");
            }
        }

        private void LoadKernelDriver()
        {
            try
            {
                string driverPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jiyu.sys");
                if (File.Exists(driverPath))
                {
                    WriteLog($"驱动文件路径: {driverPath}");
                    
                    IntPtr scManager = OpenSCManager(null, null, SC_MANAGER_CREATE_SERVICE);
                    if (scManager != IntPtr.Zero)
                    {
                        WriteLog("成功打开服务管理器");
                        
                        IntPtr service = CreateService(
                            scManager,
                            "JIYUKillerDriver",
                            "JIYU Killer Driver",
                            SERVICE_QUERY_STATUS | SERVICE_START | SERVICE_STOP | SERVICE_DELETE,
                            SERVICE_KERNEL_DRIVER,
                            SERVICE_BOOT_START,
                            SERVICE_ERROR_IGNORE,
                            driverPath,
                            null,
                            IntPtr.Zero,
                            null,
                            null,
                            null
                        );

                        if (service != IntPtr.Zero)
                        {
                            WriteLog("驱动服务创建成功");
                            
                            // 启动服务并检查返回值
                            bool startSuccess = StartService(service, 0, null);
                            if (startSuccess)
                            {
                                WriteLog("驱动服务启动命令已发送");
                                
                                // 查询服务状态验证是否真正启动
                                SERVICE_STATUS status = new SERVICE_STATUS();
                                if (QueryServiceStatus(service, ref status))
                                {
                                    if (status.dwCurrentState == SERVICE_RUNNING)
                                    {
                                        WriteLog("✅ 驱动已成功加载并运行");
                                    }
                                    else if (status.dwCurrentState == SERVICE_STOPPED)
                                    {
                                        WriteLog($"❌ 驱动服务已停止，退出码: {status.dwWin32ExitCode}");
                                    }
                                    else
                                    {
                                        WriteLog($"⚠️ 驱动状态: {GetServiceStateDescription(status.dwCurrentState)}");
                                    }
                                }
                                else
                                {
                                    int errorCode = Marshal.GetLastWin32Error();
                                    WriteLog($"查询服务状态失败，错误码: {errorCode}");
                                }
                            }
                            else
                            {
                                int errorCode = Marshal.GetLastWin32Error();
                                WriteLog($"❌ 驱动启动失败，错误码: {errorCode} ({GetWin32ErrorMessage(errorCode)})");
                            }
                            
                            CloseServiceHandle(service);
                        }
                        else
                        {
                            int errorCode = Marshal.GetLastWin32Error();
                            WriteLog($"❌ 驱动服务创建失败，错误码: {errorCode} ({GetWin32ErrorMessage(errorCode)})");
                        }
                        CloseServiceHandle(scManager);
                    }
                    else
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        WriteLog($"❌ 打开服务管理器失败，错误码: {errorCode} ({GetWin32ErrorMessage(errorCode)})");
                    }
                }
                else
                {
                    WriteLog("⚠️ 驱动文件不存在，跳过加载");
                }
            }
            catch (Exception ex)
            {
                WriteLog($"❌ 驱动加载异常: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private string GetServiceStateDescription(uint state)
        {
            switch (state)
            {
                case 0x00000001: return "STOPPED (已停止)";
                case 0x00000002: return "START_PENDING (正在启动)";
                case 0x00000003: return "STOP_PENDING (正在停止)";
                case 0x00000004: return "RUNNING (运行中)";
                case 0x00000005: return "CONTINUE_PENDING (继续挂起)";
                case 0x00000006: return "PAUSE_PENDING (暂停挂起)";
                case 0x00000007: return "PAUSED (已暂停)";
                default: return $"UNKNOWN (0x{state:X8})";
            }
        }

        private string GetWin32ErrorMessage(int errorCode)
        {
            switch (errorCode)
            {
                case 5: return "访问被拒绝 (需要管理员权限)";
                case 1058: return "服务无法启动，因为它被禁用或没有关联的启用设备";
                case 1059: return "指定了循环服务依赖";
                case 1060: return "指定的服务不存在";
                case 1061: return "服务无法在此时接受控制消息";
                case 1062: return "服务尚未启动";
                case 1067: return "进程意外终止";
                case 1070: return "启动后服务停留在启动挂起状态";
                case 1071: return "指定的服务数据库锁定无效";
                case 1072: return "指定的服务已标记为删除";
                case 1073: return "指定的服务已存在";
                case 1074: return "系统当前以最新的有效配置运行";
                case 1075: return "服务不存在，或已被标记为删除";
                case 1076: return "已接受最近启动的控制";
                case 1077: return "上次启动之后，服务没有再次启动";
                case 1078: return "名称已在使用中作为服务名或服务显示名";
                case 1083: return "配置成在该可执行程序中运行的这个服务不能执行该服务";
                case 193: return "不是有效的 Win32 应用程序";
                default: return $"未知错误 (0x{errorCode:X8})";
            }
        }

        private void KillJiyuProcesses()
        {
            try
            {
                string[] jiyuProcesses = { 
                    "StudentMain", "Student", "eClassClient", "eClass", "eClassServer", "eClassAgent", 
                    "JIYUSTUDENT", "JIYU", "Mythware", "mythware", "MythwareStudent", "MythwareClient",
                    "GATESRV", "XTRudent", "StudentService_Proxy", "HtServer", "StudentSrv"
                };

                foreach (string processName in jiyuProcesses)
                {
                    Process[] processes = Process.GetProcessesByName(processName);
                    foreach (Process process in processes)
                    {
                        try
                        {
                            process.Kill();
                            if (!process.WaitForExit(1000))
                            {
                                process.Kill(true);
                                process.WaitForExit(1000);
                            }
                            WriteLog($"已终止进程: {processName}");
                        }
                        catch (Exception ex)
                        {
                            WriteLog($"终止进程 {processName} 失败: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"查杀进程失败: {ex.Message}");
            }
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon();
            
            // 读取配置文件
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
            string trayText = "";
            if (File.Exists(configPath))
            {
                try
                {
                    trayText = File.ReadAllText(configPath).Trim();
                    WriteLog($"读取配置文件成功: {trayText}");
                }
                catch (Exception ex)
                {
                    WriteLog($"读取配置文件失败: {ex.Message}");
                }
            }
            
            // 加载图标
            try
            {
                // 优先加载同目录下的葫芦侠.ico
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "葫芦侠.ico");
                if (!File.Exists(iconPath))
                {
                    // 如果不存在，加载logo.ico
                    iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.ico");
                }
                
                WriteLog($"图标路径: {iconPath}");
                WriteLog($"图标文件存在: {File.Exists(iconPath)}");
                
                if (File.Exists(iconPath))
                {
                    trayIcon.Icon = new Icon(iconPath);
                    WriteLog("图标加载成功");
                }
                else
                {
                    trayIcon.Icon = SystemIcons.Application;
                    WriteLog("警告：未找到图标文件，使用系统默认图标");
                }
            }
            catch (Exception ex)
            {
                trayIcon.Icon = SystemIcons.Application;
                WriteLog($"图标加载失败，使用系统默认图标: {ex.Message}");
            }
            
            // 设置悬浮文字
            trayIcon.Text = "学生：000\u000B已连接到：0.0.0.0";
            trayIcon.Visible = true;
            WriteLog($"托盘图标可见性: {trayIcon.Visible}");
            WriteLog($"托盘悬浮文字: {trayIcon.Text}");

            // 创建原生右键菜单
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            
            // 添加可点击的设置菜单项（实际是退出）
            ToolStripMenuItem settingsItem = new ToolStripMenuItem("设置...");
            settingsItem.Click += (sender2, e2) => ExitApplication();
            contextMenu.Items.Add(settingsItem);
            
            // 添加菜单项
            ToolStripMenuItem raiseHandItem = new ToolStripMenuItem("举手");
            raiseHandItem.Click += (sender2, e2) => { };
            contextMenu.Items.Add(raiseHandItem);
            
            ToolStripMenuItem sendMessageItem = new ToolStripMenuItem("发送消息");
            sendMessageItem.Click += (sender2, e2) => { };
            contextMenu.Items.Add(sendMessageItem);
            
            ToolStripMenuItem viewFilesItem = new ToolStripMenuItem("查看文件");
            viewFilesItem.Click += (sender2, e2) => { };
            contextMenu.Items.Add(viewFilesItem);
            
            // 添加分隔线
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // 添加复选框菜单项
            ToolStripMenuItem showToolbarItem = new ToolStripMenuItem("显示浮动工具栏");
            showToolbarItem.Checked = true;
            showToolbarItem.Click += (sender2, e2) => { };
            contextMenu.Items.Add(showToolbarItem);
            
            ToolStripMenuItem showNotificationsItem = new ToolStripMenuItem("监控时显示通知消息");
            showNotificationsItem.Checked = true;
            showNotificationsItem.Click += (sender2, e2) => { };
            contextMenu.Items.Add(showNotificationsItem);
            
            // 添加分隔线
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // 添加关于和帮助菜单项
            ToolStripMenuItem aboutItem = new ToolStripMenuItem("关于");
            aboutItem.Click += (sender2, e2) => { };
            contextMenu.Items.Add(aboutItem);
            
            ToolStripMenuItem helpItem = new ToolStripMenuItem("帮助");
            helpItem.Click += (sender2, e2) => { };
            contextMenu.Items.Add(helpItem);
            
            // 添加分隔线
            contextMenu.Items.Add(new ToolStripSeparator());
            
            // 添加退出菜单项
            ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (sender2, e2) => ExitApplication();
            contextMenu.Items.Add(exitItem);
            
            // 设置托盘图标右键菜单
            trayIcon.ContextMenuStrip = contextMenu;

        }

        private void ExitApplication()
        {
            Form passwordForm = new Form();
            passwordForm.Text = "请输入解锁密码";
            passwordForm.Size = new Size(400, 130);
            passwordForm.StartPosition = FormStartPosition.CenterScreen;
            passwordForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            passwordForm.MaximizeBox = false;
            passwordForm.MinimizeBox = false;
            passwordForm.ControlBox = true;
            
            Label passwordLabel = new Label();
            passwordLabel.Text = "密码(P):";
            passwordLabel.Font = new Font(SystemFonts.DefaultFont.FontFamily, 12);
            passwordLabel.Location = new Point(10, 10);
            passwordLabel.AutoSize = true;
            passwordForm.Controls.Add(passwordLabel);
            
            TextBox passwordTextBox = new TextBox();
            passwordTextBox.PasswordChar = '*';
            passwordTextBox.Size = new Size(300, 22);
            passwordTextBox.Location = new Point(100, 10);
            passwordTextBox.Font = new Font(SystemFonts.DefaultFont.FontFamily, 12);
            passwordForm.Controls.Add(passwordTextBox);
            
            Button okButton = new Button();
            okButton.Text = "确定";
            okButton.Size = new Size(75, 23);
            okButton.Location = new Point(225, 50);
            okButton.Font = new Font(SystemFonts.DefaultFont.FontFamily, 12);
            okButton.Click += (sender, e) =>
            {
                string password = passwordTextBox.Text;
                string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.json");
                try
                {
                    var data = new { password = password };
                    string json = JsonSerializer.Serialize(data);
                    File.WriteAllText(dataPath, json);
                    WriteLog($"密码已保存到data.json");
                }
                catch (Exception ex)
                {
                    WriteLog($"保存密码失败: {ex.Message}");
                }
                
                WriteLog("正在退出程序");
                CleanupResources();
                
                killTimer?.Stop();
                killTimer?.Dispose();
                trayIcon.Visible = false;
                trayIcon.Dispose();
                
                WriteLog("程序已退出");
                Application.Exit();
            };
            passwordForm.Controls.Add(okButton);
            
            Button cancelButton = new Button();
            cancelButton.Text = "取消(C)";
            cancelButton.Size = new Size(75, 23);
            cancelButton.Location = new Point(305, 50);
            cancelButton.Font = new Font(SystemFonts.DefaultFont.FontFamily, 12);
            cancelButton.Click += (sender, e) =>
            {
                passwordForm.Close();
            };
            passwordForm.Controls.Add(cancelButton);
            
            passwordTextBox.Focus();
            
            passwordForm.ShowDialog();
        }

        private void CleanupResources()
        {
            UninstallCertificate();
            UnloadKernelDriver();
        }

        private void UninstallCertificate()
        {
            try
            {
                string certPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jiyu.cer");
                if (File.Exists(certPath))
                {
                    X509Certificate2 cert = new X509Certificate2(certPath);
                    X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
                    store.Open(OpenFlags.ReadWrite);
                    store.Remove(cert);
                    store.Close();
                    WriteLog("证书卸载成功");
                }
            }
            catch (Exception ex)
            {
                WriteLog($"证书卸载失败: {ex.Message}");
            }
        }

        private void UnloadKernelDriver()
        {
            try
            {
                IntPtr scManager = OpenSCManager(null, null, SC_MANAGER_CREATE_SERVICE);
                if (scManager != IntPtr.Zero)
                {
                    IntPtr service = OpenService(scManager, "JIYUKillerDriver", SERVICE_STOP | SERVICE_DELETE);
                    if (service != IntPtr.Zero)
                    {
                        DeleteService(service);
                        CloseServiceHandle(service);
                        WriteLog("驱动卸载成功");
                    }
                    CloseServiceHandle(scManager);
                }
            }
            catch (Exception ex)
            {
                WriteLog($"驱动卸载失败: {ex.Message}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CleanupResources();
                killTimer?.Dispose();
                trayIcon?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}