﻿using MahApps.Metro.Controls;
using NTMiner.MinerServer;
using NTMiner.Vms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NTMiner.Views {
    public partial class MinerClientsWindow : MetroWindow, IMainWindow {
        private static MinerClientsWindow _sWindow = null;
        public static MinerClientsWindow ShowWindow() {
            if (_sWindow == null) {
                _sWindow = new MinerClientsWindow();
            }
            _sWindow.Show();
            if (_sWindow.WindowState == WindowState.Minimized) {
                _sWindow.WindowState = WindowState.Normal;
            }
            _sWindow.Activate();
            return _sWindow;
        }

        public MinerClientsWindowViewModel Vm {
            get {
                return AppContext.Instance.MinerClientsWindowVm;
            }
        }

        private MinerClientsWindow() {
            Width = SystemParameters.FullPrimaryScreenWidth * 0.95;
            Height = SystemParameters.FullPrimaryScreenHeight * 0.95;
            this.DataContext = AppContext.Instance.MinerClientsWindowVm;
            InitializeComponent();
            this.On<Per1SecondEvent>("刷新倒计时秒表", LogEnum.None,
                action: message => {
                    var minerClients = Vm.MinerClients.ToArray();
                    if (Vm.CountDown > 0) {
                        Vm.CountDown = Vm.CountDown - 1;
                        foreach (var item in minerClients) {
                            item.OnPropertyChanged(nameof(item.LastActivedOnText));
                        }
                    }
                });
            this.On<Per10SecondEvent>("周期刷新在线客户端列表", LogEnum.DevConsole,
                action: message => {
                    AppContext.Instance.MinerClientsWindowVm.QueryMinerClients();
                });
            EventHandler changeNotiCenterWindowLocation = Wpf.Util.ChangeNotiCenterWindowLocation(this);
            this.Activated += changeNotiCenterWindowLocation;
            this.LocationChanged += changeNotiCenterWindowLocation;
            AppContext.Instance.MinerClientsWindowVm.QueryMinerClients();
            Write.UserLine("小提示：鼠标配合ctrl和shift可以多选矿机", ConsoleColor.Yellow);
        }

        protected override void OnClosing(CancelEventArgs e) {
            VirtualRoot.Execute(new ChangeServerAppSettingsCommand(
                new AppSettingData[]{
                        new AppSettingData {
                            Key = "FrozenColumnCount",
                            Value = Vm.FrozenColumnCount
                        },new AppSettingData {
                            Key = "MaxTemp",
                            Value = Vm.MaxTemp
                        },new AppSettingData {
                            Key = "MinTemp",
                            Value = Vm.MinTemp
                        },new AppSettingData {
                            Key = "RejectPercent",
                            Value = Vm.RejectPercent
                        }
            }));
            base.OnClosing(e);
        }

        public void ShowThisWindow(bool isToggle) {
            AppHelper.ShowWindow(this, isToggle);
        }

        protected override void OnClosed(EventArgs e) {
            _sWindow = null;
            base.OnClosed(e);
        }

        private void MetroWindow_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                this.DragMove();
            }
        }

        private void MinerClientsGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            Vm.SelectedMinerClients = ((DataGrid)sender).SelectedItems.Cast<MinerClientViewModel>().ToArray();
        }

        private void ScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            Wpf.Util.ScrollViewer_PreviewMouseDown(sender, e);
        }

        private void MenuItemWork_Click(object sender, RoutedEventArgs e) {
            PopMineWork.IsOpen = !PopMineWork.IsOpen;
        }

        private void MenuItemGroup_Click(object sender, RoutedEventArgs e) {
            PopMinerGroup.IsOpen = !PopMinerGroup.IsOpen;
        }

        private void PopupButton_Click(object sender, RoutedEventArgs e) {
            PopMineWork.IsOpen = PopMinerGroup.IsOpen = PopUpgrade.IsOpen = false;
        }

        private void MenuItemUpgrade_Click(object sender, RoutedEventArgs e) {
            OfficialServer.FileUrlService.GetNTMinerFilesAsync(NTMinerAppType.MinerClient, (ntMinerFiles, ex) => {
                Vm.NTMinerFileList = (ntMinerFiles ?? new List<NTMinerFileData>()).OrderByDescending(a => a.GetVersion()).ToList();
            });
            PopUpgrade.IsOpen = !PopUpgrade.IsOpen;
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (Vm.SelectedMinerClients != null && Vm.SelectedMinerClients.Length != 0) {
                Vm.SelectedMinerClients[0].RemoteDesktop.Execute(null);
            }
        }

        private void DataGrid_OnSorting(object sender, DataGridSortingEventArgs e) {
            e.Handled = true;
        }
    }
}
