﻿using NTMiner.Vms;
using System.Windows.Controls;

namespace NTMiner.Views.Ucs {
    public partial class AboutPage : UserControl {
        public static void ShowWindow(string appType) {
            ContainerWindow.ShowWindow(new ContainerWindowViewModel {
                Title = "关于",
                IconName = "Icon_About",
                CloseVisible = System.Windows.Visibility.Visible,
                FooterVisible = System.Windows.Visibility.Collapsed
            }, ucFactory: (window) => new AboutPage(appType), fixedSize: true);
        }

        public AboutPageViewModel Vm {
            get {
                return (AboutPageViewModel)this.DataContext;
            }
        }

        public AboutPage(string appType) {
            InitializeComponent();
        }
    }
}
