﻿using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace NTMiner {
    public static class NTMinerRegistry {
        private const string AutoRunSubKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public static void SetAutoBoot(string valueName, bool isAutoBoot, string otherParams = null) {
            string exeFileFullName = Process.GetCurrentProcess().MainModule.FileName;
            if (isAutoBoot == true) {
                string value = exeFileFullName;
                if (!string.IsNullOrEmpty(otherParams)) {
                    value = value + " " + otherParams;
                }
                Windows.Registry.SetValue(Registry.CurrentUser, AutoRunSubKey, valueName, value);
            }
            else {
                Windows.Registry.DeleteValue(Registry.CurrentUser, AutoRunSubKey, valueName);
            }
        }

        public const string NTMinerRegistrySubKey = @".DEFAULT\Software\NTMiner";

        #region IsShowInTaskbar
        public static bool GetIsShowInTaskbar() {
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, "IsShowInTaskbar");
            return value == null || value.ToString() == "True";
        }

        public static void SetIsShowInTaskbar(bool value) {
            Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, "IsShowInTaskbar", value);
        }
        #endregion

        #region IsNoUi
        public static bool GetIsNoUi() {
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, "IsNoUi");
            return value == null || value.ToString() == "True";
        }

        public static void SetIsNoUi(bool value) {
            Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, "IsNoUi", value);
        }
        #endregion

        #region AutoNoUi
        public static bool GetIsAutoNoUi() {
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, "IsAutoNoUi");
            return value == null || value.ToString() == "True";
        }

        public static void SetIsAutoNoUi(bool value) {
            Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, "IsAutoNoUi", value);
        }
        #endregion

        #region AutoNoUiMinutes
        public static int GetAutoNoUiMinutes() {
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, "AutoNoUiMinutes");
            if (value == null) {
                return 10;
            }
            int v;
            int.TryParse(value.ToString(), out v);
            return v;
        }

        public static void SetAutoNoUiMinutes(int value) {
            Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, "AutoNoUiMinutes", value);
        }
        #endregion

        #region IsShowNotifyIcon
        public static bool GetIsShowNotifyIcon() {
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, "IsShowNotifyIcon");
            return value == null || value.ToString() == "True";
        }

        public static void SetIsShowNotifyIcon(bool value) {
            Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, "IsShowNotifyIcon", value);
        }
        #endregion

        #region Location
        public static string GetLocation() {
            string valueName = "Location";
            if (VirtualRoot.IsMinerStudio) {
                valueName = "MinerStudioLocation";
            }
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, valueName);
            if (value != null) {
                return (string)value;
            }
            return string.Empty;
        }

        public static void SetLocation(string location) {
            string valueName = "Location";
            if (VirtualRoot.IsMinerStudio) {
                valueName = "MinerStudioLocation";
            }
            Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, valueName, location);
        }
        #endregion

        #region Arguments
        public static string GetArguments() {
            string valueName = "Arguments";
            if (VirtualRoot.IsMinerStudio) {
                valueName = "MinerStudioArguments";
            }
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, valueName);
            if (value != null) {
                return (string)value;
            }
            return string.Empty;
        }

        public static void SetArguments(string arguments) {
            string valueName = "Arguments";
            if (VirtualRoot.IsMinerStudio) {
                valueName = "MinerStudioArguments";
            }
            Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, valueName, arguments);
        }
        #endregion

        #region IsAutoCloseServices
        public static bool GetIsAutoCloseServices() {
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, "IsAutoCloseServices");
            return value != null && value.ToString() == "True";
        }

        public static void SetIsAutoCloseServices(bool value) {
            Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, "IsAutoCloseServices", value);
        }
        #endregion

        #region IsAutoBoot
        public static bool GetIsAutoBoot() {
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, "IsAutoBoot");
            return value != null && value.ToString() == "True";
        }

        public static void SetIsAutoBoot(bool value) {
            Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, "IsAutoBoot", value);
        }
        #endregion

        #region IsAutoStart
        public static bool GetIsAutoStart() {
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, "IsAutoStart");
            return value != null && value.ToString() == "True";
        }

        public static void SetIsAutoStart(bool value) {
            Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, "IsAutoStart", value);
        }
        #endregion

        #region CurrentVersion
        public static string GetCurrentVersion() {
            string valueName = "CurrentVersion";
            if (VirtualRoot.IsMinerStudio) {
                valueName = "MinerStudioCurrentVersion";
            }
            string currentVersion = "1.0.0.0";
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, valueName);
            if (value != null) {
                currentVersion = (string)value;
            }
            if (string.IsNullOrEmpty(currentVersion)) {
                return "1.0.0.0";
            }
            return currentVersion;
        }

        public static void SetCurrentVersion(string version) {
            string valueName = "CurrentVersion";
            if (VirtualRoot.IsMinerStudio) {
                valueName = "MinerStudioCurrentVersion";
            }
            Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, valueName, version);
        }
        #endregion

        #region CurrentVersionTag
        public static string GetCurrentVersionTag() {
            string valueName = "CurrentVersionTag";
            if (VirtualRoot.IsMinerStudio) {
                valueName = "MinerStudioCurrentVersionTag";
            }
            string currentVersionTag = string.Empty;
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, valueName);
            if (value != null) {
                currentVersionTag = (string)value;
            }
            return currentVersionTag;
        }

        public static void SetCurrentVersionTag(string versionTag) {
            string valueName = "CurrentVersionTag";
            if (VirtualRoot.IsMinerStudio) {
                valueName = "MinerStudioCurrentVersionTag";
            }
            Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, valueName, versionTag);
        }
        #endregion

        #region ControlCenterHost
        public const string DefaultControlCenterHost = "localhost";
        public static string GetControlCenterHost() {
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, "ControlCenterHost");
            if (value == null) {
                return DefaultControlCenterHost;
            }
            return (string)value;
        }

        public static void SetControlCenterHost(string host) {
            if (string.IsNullOrEmpty(host)) {
                host = DefaultControlCenterHost;
            }
            Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, "ControlCenterHost", host);
        }
        #endregion

        #region DaemonVersion
        public static string GetDaemonVersion() {
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, "DaemonVersion");
            if (value == null) {
                return string.Empty;
            }
            return (string)value;
        }

        public static void SetDaemonVersion(string version) {
            if (version == null) {
                version = string.Empty;
            }
            Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, "DaemonVersion", version);
        }
        #endregion

        #region DaemonVersion
        public static DateTime GetDaemonActiveOn() {
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, "DaemonActiveOn");
            if (value == null) {
                return DateTime.MinValue;
            }
            string str = value.ToString();
            DateTime dateTime;
            if (!DateTime.TryParse(str, out dateTime)) {
                return DateTime.MinValue;
            }
            return dateTime;
        }

        public static void SetDaemonActiveOn(DateTime version) {
            Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, "DaemonActiveOn", version.ToString());
        }
        #endregion

        #region GetClientId
        public static Guid GetClientId() {
            Guid id;
            object value = Windows.Registry.GetValue(Registry.Users, NTMinerRegistrySubKey, "ClientId");
            if (value == null) {
                id = Guid.NewGuid();
                Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, "ClientId", id.ToString());
            }
            else if (!Guid.TryParse((string)value, out id)) {
                id = Guid.NewGuid();
                Windows.Registry.SetValue(Registry.Users, NTMinerRegistrySubKey, "ClientId", id.ToString());
            }
            return id;
        }
        #endregion
    }
}
