﻿using NTMiner.Core;
using NTMiner.OverClock;
using NTMiner.Views;
using NTMiner.Vms;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace NTMiner {
    public partial class App : Application, IDisposable {
        public App() {
            Logging.LogDir.SetDir(Path.Combine(VirtualRoot.GlobalDirFullName, "Logs"));
            AppHelper.Init(this);
            InitializeComponent();
        }

        private bool createdNew;
        private Mutex appMutex;
        private static string s_appPipName = "ntminerclient";
        protected override void OnExit(ExitEventArgs e) {
            AppHelper.NotifyIcon?.Dispose();
            NTMinerRoot.Instance.Exit();
            HttpServer.Stop();
            base.OnExit(e);
            ConsoleManager.Hide();
        }

        protected override void OnStartup(StartupEventArgs e) {
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            if (!string.IsNullOrEmpty(CommandLineArgs.Upgrade)) {
                AppStatic.Upgrade(CommandLineArgs.Upgrade, () => {
                    Environment.Exit(0);
                });
            }
            else {
                try {
                    appMutex = new Mutex(true, s_appPipName, out createdNew);
                }
                catch (Exception) {
                    createdNew = false;
                }
                if (createdNew) {
                    if (!NTMiner.Windows.WMI.IsWmiEnabled) {
                        DialogWindow.ShowDialog(message: "开源矿工无法运行所需的组件，因为本机未开机WMI服务，开源矿工需要使用WMI服务检测windows的内存、显卡等信息，请先手动开启WMI。", title: "提醒", icon: "Icon_Error");
                        Shutdown();
                        Environment.Exit(0);
                    }
                    NTMinerOverClockUtil.ExtractResource();

                    AppStatic.SetIsMinerClient(true);
                    NotiCenterWindowViewModel.IsHotKeyEnabled = true;
                    SplashWindow splashWindow = new SplashWindow();
                    splashWindow.Show();
                    NotiCenterWindow.Instance.Show();
                    NTMinerRoot.Instance.Init(() => {
                        NTMinerRoot.KernelDownloader = new KernelDownloader();
                        UIThread.Execute(() => {
                            if (!NTMinerRegistry.GetIsNoUi() || !NTMinerRegistry.GetIsAutoStart()) {
                                Views.MainWindow.ShowMainWindow();
                            }
                            else {
                                NotiCenterWindowViewModel.Instance.Manager.ShowSuccessMessage("已切换为无界面模式运行", "开源矿工");
                            }
                            System.Drawing.Icon icon = new System.Drawing.Icon(GetResourceStream(new Uri("pack://application:,,,/NTMiner;component/logo.ico")).Stream);
                            AppHelper.NotifyIcon = ExtendedNotifyIcon.Create(icon, "开源矿工", isMinerStudio: false);
                            #region 处理显示主界面命令
                            VirtualRoot.Window<ShowMainWindowCommand>("处理显示主界面命令", LogEnum.None,
                                action: message => {
                                    UIThread.Execute(() => {
                                        MainWindow mainWindow = this.MainWindow as MainWindow;
                                        if (mainWindow == null) {
                                            AppContext.Enable();
                                            Views.MainWindow.ShowMainWindow();
                                            // 使状态栏显示显示最新状态
                                            if (NTMinerRoot.Instance.IsMining) {
                                                var coinShare = NTMinerRoot.Instance.CoinShareSet.GetOrCreate(NTMinerRoot.Instance.CurrentMineContext.MainCoin.GetId());
                                                VirtualRoot.Happened(new ShareChangedEvent(coinShare));
                                                if (NTMinerRoot.Instance.CurrentMineContext is IDualMineContext dualMineContext) {
                                                    coinShare = NTMinerRoot.Instance.CoinShareSet.GetOrCreate(dualMineContext.DualCoin.GetId());
                                                    VirtualRoot.Happened(new ShareChangedEvent(coinShare));
                                                }
                                                AppContext.Instance.GpuSpeedVms.Refresh();
                                            }
                                        }
                                        else {
                                            mainWindow.ShowThisWindow(message.IsToggle);
                                        }
                                    });
                                });
                            #endregion
                            splashWindow?.Close();
                            Task.Factory.StartNew(() => {
                                try {
                                    HttpServer.Start($"http://localhost:{WebApiConst.MinerClientPort}");
                                    NTMinerRoot.Instance.Start();
                                }
                                catch (Exception ex) {
                                    Logger.ErrorDebugLine(ex.Message, ex);
                                }
                            });
                        });
                    });
                    Link();
                }
                else {
                    try {
                        AppHelper.ShowMainWindow(this, MinerServer.NTMinerAppType.MinerClient);
                    }
                    catch (Exception) {
                        DialogWindow.ShowDialog(message: "另一个NTMiner正在运行，请手动结束正在运行的NTMiner进程后再次尝试。", title: "提醒", icon: "Icon_Error");
                        Process currentProcess = Process.GetCurrentProcess();
                        NTMiner.Windows.TaskKill.KillOtherProcess(currentProcess);
                    }
                }
            }
            base.OnStartup(e);
        }

        private void Link() {
            VirtualRoot.Window<CloseNTMinerCommand>("处理关闭NTMiner客户端命令", LogEnum.UserConsole,
                action: message => {
                    UIThread.Execute(() => {
                        try {
                            if (MainWindow != null) {
                                MainWindow.Close();
                            }
                            Shutdown();
                        }
                        catch (Exception e) {
                            Logger.ErrorDebugLine(e.Message, e);
                            Environment.Exit(0);
                        }
                    });
                });
            VirtualRoot.Window<CloseMainWindowCommand>("处理关闭主界面命令", LogEnum.DevConsole,
                action: message => {
                    UIThread.Execute(() => {
                        Write.SetConsoleUserLineMethod();
                        MainWindow = NotiCenterWindow.Instance;
                        foreach (Window window in Windows) {
                            if (window != NotiCenterWindow.Instance) {
                                window.Close();
                            }
                        }
                        AppContext.Disable();
                        NotiCenterWindowViewModel.Instance.Manager.ShowSuccessMessage("已切换为无界面模式运行", "开源矿工");
                    });
                });
            #region 周期确保守护进程在运行
            Daemon.DaemonUtil.RunNTMinerDaemon();
            VirtualRoot.On<Per20SecondEvent>("周期确保守护进程在运行", LogEnum.None,
                action: message => {
                    if (NTMinerRegistry.GetDaemonActiveOn().AddSeconds(20) < DateTime.Now) {
                        Daemon.DaemonUtil.RunNTMinerDaemon();
                    }
                });
            #endregion
            #region 1080小药丸
            VirtualRoot.On<MineStartedEvent>("开始挖矿后启动1080ti小药丸、挖矿开始后如果需要启动DevConsole则启动DevConsole", LogEnum.DevConsole,
                action: message => {
                    // 启动DevConsole
                    if (NTMinerRoot.IsUseDevConsole) {
                        var mineContext = message.MineContext;
                        string poolIp = mineContext.MainCoinPool.GetIp();
                        string consoleTitle = mineContext.MainCoinPool.Server;
                        Daemon.DaemonUtil.RunDevConsoleAsync(poolIp, consoleTitle);
                    }
                    OhGodAnETHlargementPill.OhGodAnETHlargementPillUtil.Start();
                });
            VirtualRoot.On<MineStopedEvent>("停止挖矿后停止1080ti小药丸", LogEnum.DevConsole,
                action: message => {
                    OhGodAnETHlargementPill.OhGodAnETHlargementPillUtil.Stop();
                });
            #endregion
            #region 处理开启A卡计算模式
            VirtualRoot.Window<SwitchRadeonGpuCommand>("处理开启A卡计算模式命令", LogEnum.DevConsole,
                action: message => {
                    if (NTMinerRoot.Instance.GpuSet.GpuType == GpuType.AMD) {
                        if (NTMinerRoot.Instance.IsMining) {
                            NTMinerRoot.Instance.StopMineAsync(() => {
                                SwitchRadeonGpuMode();
                                NTMinerRoot.Instance.StartMine();
                            });
                        }
                        else {
                            SwitchRadeonGpuMode();
                        }
                    }
                });
            #endregion
        }

        private static void SwitchRadeonGpuMode() {
            SwitchRadeonGpu.SwitchRadeonGpu.Run((isSuccess, e) => {
                if (isSuccess) {
                    NotiCenterWindowViewModel.Instance.Manager.ShowSuccessMessage("开启A卡计算模式成功");
                }
                else if (e != null) {
                    NotiCenterWindowViewModel.Instance.Manager.ShowErrorMessage(e.Message, delaySeconds: 4);
                }
                else {
                    NotiCenterWindowViewModel.Instance.Manager.ShowErrorMessage("开启A卡计算模式失败", delaySeconds: 4);
                }
            });
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing) {
            if (disposing) {
                if (appMutex != null) {
                    appMutex.Dispose();
                }
            }
        }
    }
}
