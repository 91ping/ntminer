﻿using NTMiner.Core;
using NTMiner.MinerServer;
using NTMiner.Profile;
using NTMiner.User;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace NTMiner.Controllers {
    public class ControlCenterController : ApiController, IControlCenterController {
        private string ClientIp {
            get {
                return Request.GetWebClientIp();
            }
        }

        private static string s_sha1 = null;
        public static string Sha1 {
            get {
                if (s_sha1 == null) {
                    s_sha1 = HashUtil.Sha1(File.ReadAllBytes(Process.GetCurrentProcess().MainModule.FileName));
                }
                return s_sha1;
            }
        }

        [HttpPost]
        public string GetServicesVersion() {
            return Sha1;
        }

        [HttpPost]
        public void CloseServices() {
            HostRoot.WaitHandle.Set();
        }

        [HttpPost]
        public void RefreshNotifyIcon() {
            HostRoot.NotifyIcon?.RefreshIcon();
        }

        #region ActiveControlCenterAdmin
        [HttpPost]
        public ResponseBase ActiveControlCenterAdmin([FromBody]string password) {
            if (string.IsNullOrEmpty(password)) {
                return ResponseBase.InvalidInput(Guid.Empty, "密码不能为空");
            }
            IUser user = HostRoot.Current.UserSet.GetUser("admin");
            if (user == null) {
                var userData = new UserData {
                    LoginName = "admin",
                    IsEnabled = true,
                    Description = "中控初始用户",
                    Password = password
                };
                VirtualRoot.Execute(new AddUserCommand(userData));
                return ResponseBase.Ok(Guid.Empty);
            }
            else {
                return ResponseBase.Forbidden(Guid.Empty);
            }
        }
        #endregion

        #region LoginControlCenter
        [HttpPost]
        public ResponseBase LoginControlCenter([FromBody]SignatureRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out IUser user, out ResponseBase response)) {
                    return response;
                }
                Write.DevLine($"{request.LoginName}登录");
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region Users
        [HttpPost]
        public DataResponse<List<UserData>> Users([FromBody]DataRequest<Guid?> request) {
            if (request == null) {
                return ResponseBase.InvalidInput<DataResponse<List<UserData>>>(Guid.Empty, "参数错误");
            }
            try {
                if (!request.Data.HasValue) {
                    // request.Data是ClientId，如果未传ClientId表示是中控客户端，中控客户端获取用户表需验证身份
                    if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out DataResponse<List<UserData>> response)) {
                        return response;
                    }
                }
                var data = HostRoot.Current.UserSet.Cast<UserData>().ToList();
                if (request.Data.HasValue) {
                    // request.Data是ClientId，挖矿端获取用户表无需验证身份但获取到的用户表的密码是加密的和中控客户端获取到的不同的
                    data = data.Select(a => new UserData(a)).ToList();
                    foreach (var user in data) {
                        user.Password = HashUtil.Sha1(HashUtil.Sha1(user.Password) + request.Data.Value);
                    }
                }
                return DataResponse<List<UserData>>.Ok(request.MessageId, data);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<DataResponse<List<UserData>>>(request.MessageId, e.Message);
            }
        }
        #endregion

        #region AddUser
        [HttpPost]
        public ResponseBase AddUser([FromBody]DataRequest<UserData> request) {
            if (request == null || request.Data == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                VirtualRoot.Execute(new AddUserCommand(request.Data));
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region UpdateUser
        [HttpPost]
        public ResponseBase UpdateUser([FromBody]DataRequest<UserData> request) {
            if (request == null || request.Data == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                VirtualRoot.Execute(new UpdateUserCommand(request.Data));
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region RemoveUser
        [HttpPost]
        public ResponseBase RemoveUser([FromBody]DataRequest<String> request) {
            if (request == null || string.IsNullOrEmpty(request.Data)) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                VirtualRoot.Execute(new RemoveUserCommand(request.Data));
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region ChangePassword
        [HttpPost]
        public ResponseBase ChangePassword([FromBody]ChangePasswordRequest request) {
            if (request == null || string.IsNullOrEmpty(request.LoginName) 
                || string.IsNullOrEmpty(request.OldPassword) 
                || string.IsNullOrEmpty(request.NewPassword)) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                IUser user = HostRoot.Current.UserSet.GetUser(request.LoginName);
                if (user == null) {
                    return ResponseBase.ClientError(request.MessageId, $"登录名不存在");
                }
                if (user.Password == request.NewPassword) {
                    return ResponseBase.Ok(request.MessageId);
                }
                if (user.Password != request.OldPassword) {
                    return ResponseBase.ClientError(request.MessageId, $"旧密码不正确");
                }
                VirtualRoot.Execute(new ChangePasswordCommand(request.LoginName, request.OldPassword, request.NewPassword, request.Description));
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region QueryClients
        [HttpPost]
        public QueryClientsResponse QueryClients([FromBody]QueryClientsRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput<QueryClientsResponse>(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out QueryClientsResponse response)) {
                    return response;
                }
                var data = HostRoot.Current.ClientSet.QueryClients(
                               request.PageIndex,
                                request.PageSize,
                                request.GroupId,
                                request.WorkId,
                                request.MinerIp,
                                request.MinerName,
                                request.MineState,
                                request.Coin,
                                request.Pool,
                                request.Wallet,
                                request.Version,
                                request.Kernel,
                                out int total,
                                out int miningCount) ?? new List<ClientData>();
                return QueryClientsResponse.Ok(request.MessageId, data, total, miningCount);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<QueryClientsResponse>(request.MessageId, e.Message);
            }
        }
        #endregion

        #region LatestSnapshots
        [HttpPost]
        public GetCoinSnapshotsResponse LatestSnapshots([FromBody]GetCoinSnapshotsRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput<GetCoinSnapshotsResponse>(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out GetCoinSnapshotsResponse response)) {
                    return response;
                }
                List<CoinSnapshotData> data = HostRoot.Current.CoinSnapshotSet.GetLatestSnapshots(
                    request.Limit,
                    out int totalMiningCount,
                    out int totalOnlineCount) ?? new List<CoinSnapshotData>();
                return GetCoinSnapshotsResponse.Ok(request.MessageId, data, totalMiningCount, totalOnlineCount);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<GetCoinSnapshotsResponse>(request.MessageId, e.Message);
            }
        }
        #endregion

        #region AddClients

        [HttpPost]
        public ResponseBase AddClients([FromBody]AddClientRequest request) {
            if (request == null || request.ClientIps == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }

            if (request.ClientIps.Count > 101) {
                return ResponseBase.InvalidInput(request.MessageId, "最多支持一次添加101个IP");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }

                if (request.ClientIps.Any(a => !IPAddress.TryParse(a, out IPAddress ip))) {
                    return ResponseBase.InvalidInput(request.MessageId, "IP格式不正确");
                }

                foreach (var clientIp in request.ClientIps) {
                    ClientData clientData = HostRoot.Current.ClientSet.FirstOrDefault(a => a.MinerIp == clientIp);
                    if (clientData != null) {
                        continue;
                    }
                    HostRoot.Current.ClientSet.AddMiner(clientIp);
                }
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region RemoveClients
        [HttpPost]
        public ResponseBase RemoveClients([FromBody]MinerIdsRequest request) {
            if (request == null || request.ObjectIds == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }

            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }

                foreach (var objectId in request.ObjectIds) {
                    HostRoot.Current.ClientSet.Remove(objectId);
                }
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region UpdateClient
        [HttpPost]
        public ResponseBase UpdateClient([FromBody]UpdateClientRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                HostRoot.Current.ClientSet.UpdateClient(request.ObjectId, request.PropertyName, request.Value);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region RefreshClients

        public DataResponse<List<ClientData>> RefreshClients([FromBody]MinerIdsRequest request) {
            if (request == null || request.ObjectIds == null) {
                return ResponseBase.InvalidInput<DataResponse<List<ClientData>>>(Guid.Empty, "参数错误");
            }

            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out DataResponse<List<ClientData>> response)) {
                    return response;
                }

                var data = HostRoot.Current.ClientSet.RefreshClients(request.ObjectIds);
                return DataResponse<List<ClientData>>.Ok(request.MessageId, data);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<DataResponse<List<ClientData>>>(request.MessageId, e.Message);
            }
        }
        #endregion

        #region UpdateClientProperties
        [HttpPost]
        public ResponseBase UpdateClientProperties([FromBody]UpdateClientPropertiesRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                HostRoot.Current.ClientSet.UpdateClientProperties(request.ObjectId, request.Values);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region MinerGroups
        [HttpPost]
        public DataResponse<List<MinerGroupData>> MinerGroups([FromBody]SignatureRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput<DataResponse<List<MinerGroupData>>>(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out DataResponse<List<MinerGroupData>> response)) {
                    return response;
                }
                var data = HostRoot.Current.MinerGroupSet.GetAll();
                return DataResponse<List<MinerGroupData>>.Ok(request.MessageId, data);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<DataResponse<List<MinerGroupData>>>(request.MessageId, e.Message);
            }
        }
        #endregion

        #region AddOrUpdateMinerGroup
        [HttpPost]
        public ResponseBase AddOrUpdateMinerGroup([FromBody]DataRequest<MinerGroupData> request) {
            if (request == null || request.Data == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                HostRoot.Current.MinerGroupSet.AddOrUpdate(request.Data);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region RemoveMinerGroup
        [HttpPost]
        public ResponseBase RemoveMinerGroup([FromBody]DataRequest<Guid> request) {
            if (request == null || request.Data == Guid.Empty) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                IMinerGroup minerGroup = HostRoot.Current.MinerGroupSet.GetMinerGroup(request.Data);
                if (minerGroup == null) {
                    return ResponseBase.Ok(request.MessageId);
                }
                if (HostRoot.Current.ClientSet.IsAnyClientInGroup(request.Data)) {
                    return ResponseBase.ClientError(request.MessageId, $"组{minerGroup.Name}下有矿机，请先移除矿机再做删除操作");
                }
                HostRoot.Current.MinerGroupSet.Remove(request.Data);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region AddOrUpdateMineWork
        [HttpPost]
        public ResponseBase AddOrUpdateMineWork([FromBody]DataRequest<MineWorkData> request) {
            if (request == null || request.Data == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                HostRoot.Current.MineWorkSet.AddOrUpdate(request.Data);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region RemoveMineWork
        [HttpPost]
        public ResponseBase RemoveMineWork([FromBody]DataRequest<Guid> request) {
            if (request == null || request.Data == Guid.Empty) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                IMineWork mineWork = HostRoot.Current.MineWorkSet.GetMineWork(request.Data);
                if (mineWork == null) {
                    return ResponseBase.Ok(request.MessageId);
                }
                if (HostRoot.Current.ClientSet.IsAnyClientInWork(request.Data)) {
                    return ResponseBase.ClientError(request.MessageId, $"作业{mineWork.Name}下有矿机，请先移除矿机再做删除操作");
                }
                HostRoot.Current.MineWorkSet.Remove(request.Data);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region MineWorks
        [HttpPost]
        public DataResponse<List<MineWorkData>> MineWorks([FromBody]SignatureRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput<DataResponse<List<MineWorkData>>>(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out DataResponse<List<MineWorkData>> response)) {
                    return response;
                }
                var data = HostRoot.Current.MineWorkSet.GetAll();
                return DataResponse<List<MineWorkData>>.Ok(request.MessageId, data);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<DataResponse<List<MineWorkData>>>(request.MessageId, e.Message);
            }
        }
        #endregion

        #region ExportMineWork
        [HttpPost]
        public ResponseBase ExportMineWork([FromBody]ExportMineWorkRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput<ResponseBase>(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                string localJsonFileFullName = SpecialPath.GetMineWorkLocalJsonFileFullName(request.MineWorkId);
                string serverJsonFileFullName = SpecialPath.GetMineWorkServerJsonFileFullName(request.MineWorkId);
                File.WriteAllText(localJsonFileFullName, request.LocalJson);
                File.WriteAllText(serverJsonFileFullName, request.ServerJson);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<ResponseBase>(request.MessageId, e.Message);
            }
        }
        #endregion

        [HttpPost]
        public DataResponse<string> GetLocalJson([FromBody]DataRequest<Guid> request) {
            if (request == null) {
                return ResponseBase.InvalidInput<DataResponse<string>>(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out DataResponse<string> response)) {
                    return response;
                }
                string localJsonFileFullName = SpecialPath.GetMineWorkLocalJsonFileFullName(request.Data);
                string data = string.Empty;
                if (File.Exists(localJsonFileFullName)) {
                    data = File.ReadAllText(localJsonFileFullName);
                }
                return DataResponse<string>.Ok(request.MessageId, data);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<DataResponse<string>>(request.MessageId, e.Message);
            }
        }

        #region MinerProfile
        [HttpPost]
        public DataResponse<MinerProfileData> MinerProfile([FromBody]DataRequest<Guid> request) {
            if (request == null) {
                return ResponseBase.InvalidInput<DataResponse<MinerProfileData>>(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out DataResponse<MinerProfileData> response)) {
                    return response;
                }
                var data = HostRoot.Current.MineProfileManager.GetMinerProfile(request.Data);
                return DataResponse<MinerProfileData>.Ok(request.MessageId, data);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<DataResponse<MinerProfileData>>(request.MessageId, e.Message);
            }
        }
        #endregion

        #region SetMinerProfile
        [HttpPost]
        public ResponseBase SetMinerProfile([FromBody]SetWorkProfileRequest<MinerProfileData> request) {
            if (request == null || request.Data == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                if (!HostRoot.Current.MineWorkSet.Contains(request.WorkId)) {
                    return ResponseBase.InvalidInput(request.MessageId, "给定的workId不存在");
                }
                HostRoot.Current.MineProfileManager.SetMinerProfile(request.WorkId, request.Data);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region CoinProfile
        [HttpPost]
        public DataResponse<CoinProfileData> CoinProfile([FromBody]WorkProfileRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput<DataResponse<CoinProfileData>>(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out DataResponse<CoinProfileData> response)) {
                    return response;
                }
                var data = HostRoot.Current.MineProfileManager.GetCoinProfile(request.WorkId, request.DataId);
                return DataResponse<CoinProfileData>.Ok(request.MessageId, data);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<DataResponse<CoinProfileData>>(request.MessageId, e.Message);
            }
        }
        #endregion

        #region SetCoinProfile
        [HttpPost]
        public ResponseBase SetCoinProfile([FromBody]SetWorkProfileRequest<CoinProfileData> request) {
            if (request == null || request.Data == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                if (!HostRoot.Current.MineWorkSet.Contains(request.WorkId)) {
                    return ResponseBase.InvalidInput(request.MessageId, "给定的workId不存在");
                }
                HostRoot.Current.MineProfileManager.SetCoinProfile(request.WorkId, request.Data);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region PoolProfile
        [HttpPost]
        public DataResponse<PoolProfileData> PoolProfile([FromBody]WorkProfileRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput<DataResponse<PoolProfileData>>(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out DataResponse<PoolProfileData> response)) {
                    return response;
                }
                var data = HostRoot.Current.MineProfileManager.GetPoolProfile(request.WorkId, request.DataId);
                return DataResponse<PoolProfileData>.Ok(request.MessageId, data);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<DataResponse<PoolProfileData>>(request.MessageId, e.Message);
            }
        }
        #endregion

        #region SetPoolProfile
        [HttpPost]
        public ResponseBase SetPoolProfile([FromBody]SetWorkProfileRequest<PoolProfileData> request) {
            if (request == null || request.Data == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                if (!HostRoot.Current.MineWorkSet.Contains(request.WorkId)) {
                    return ResponseBase.InvalidInput(request.MessageId, "给定的workId不存在");
                }
                HostRoot.Current.MineProfileManager.SetPoolProfile(request.WorkId, request.Data);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region CoinKernelProfile
        [HttpPost]
        public DataResponse<CoinKernelProfileData> CoinKernelProfile([FromBody]WorkProfileRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput<DataResponse<CoinKernelProfileData>>(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out DataResponse<CoinKernelProfileData> response)) {
                    return response;
                }
                var data = HostRoot.Current.MineProfileManager.GetCoinKernelProfile(request.WorkId, request.DataId);
                return DataResponse<CoinKernelProfileData>.Ok(request.MessageId, data);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<DataResponse<CoinKernelProfileData>>(request.MessageId, e.Message);
            }
        }
        #endregion

        #region SetCoinKernelProfile
        [HttpPost]
        public ResponseBase SetCoinKernelProfile([FromBody]SetWorkProfileRequest<CoinKernelProfileData> request) {
            if (request == null || request.Data == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                if (!HostRoot.Current.MineWorkSet.Contains(request.WorkId)) {
                    return ResponseBase.InvalidInput(request.MessageId, "给定的workId不存在");
                }
                HostRoot.Current.MineProfileManager.SetCoinKernelProfile(request.WorkId, request.Data);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region SetMinerProfileProperty
        [HttpPost]
        public ResponseBase SetMinerProfileProperty([FromBody]SetMinerProfilePropertyRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                if (!HostRoot.Current.MineWorkSet.Contains(request.WorkId)) {
                    return ResponseBase.InvalidInput(request.MessageId, "给定的workId不存在");
                }
                HostRoot.Current.MineProfileManager.SetMinerProfileProperty(request.WorkId, request.PropertyName, request.Value);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region SetCoinProfileProperty
        [HttpPost]
        public ResponseBase SetCoinProfileProperty([FromBody]SetCoinProfilePropertyRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                if (!HostRoot.Current.MineWorkSet.Contains(request.WorkId)) {
                    return ResponseBase.InvalidInput(request.MessageId, "给定的workId不存在");
                }
                HostRoot.Current.MineProfileManager.SetCoinProfileProperty(request.WorkId, request.CoinId, request.PropertyName, request.Value);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region SetPoolProfileProperty
        [HttpPost]
        public ResponseBase SetPoolProfileProperty([FromBody]SetPoolProfilePropertyRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                if (!HostRoot.Current.MineWorkSet.Contains(request.WorkId)) {
                    return ResponseBase.InvalidInput(request.MessageId, "给定的workId不存在");
                }
                HostRoot.Current.MineProfileManager.SetPoolProfileProperty(request.WorkId, request.PoolId, request.PropertyName, request.Value);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region SetCoinKernelProfileProperty
        [HttpPost]
        public ResponseBase SetCoinKernelProfileProperty([FromBody]SetCoinKernelProfilePropertyRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                if (!HostRoot.Current.MineWorkSet.Contains(request.WorkId)) {
                    return ResponseBase.InvalidInput(request.MessageId, "给定的workId不存在");
                }
                HostRoot.Current.MineProfileManager.SetCoinKernelProfileProperty(request.WorkId, request.CoinKernelId, request.PropertyName, request.Value);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region Pools
        [HttpPost]
        public DataResponse<List<PoolData>> Pools([FromBody]SignatureRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput<DataResponse<List<PoolData>>>(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out DataResponse<List<PoolData>> response)) {
                    return response;
                }
                var data = HostRoot.Current.PoolSet.GetAll();
                return DataResponse<List<PoolData>>.Ok(request.MessageId, data);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<DataResponse<List<PoolData>>>(request.MessageId, e.Message);
            }
        }
        #endregion

        #region AddOrUpdatePool
        [HttpPost]
        public ResponseBase AddOrUpdatePool([FromBody]DataRequest<PoolData> request) {
            if (request == null || request.Data == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                HostRoot.Current.PoolSet.AddOrUpdate(request.Data);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region RemovePool
        [HttpPost]
        public ResponseBase RemovePool([FromBody]DataRequest<Guid> request) {
            if (request == null || request.Data == Guid.Empty) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                HostRoot.Current.PoolSet.Remove(request.Data);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region Wallets
        [HttpPost]
        public DataResponse<List<WalletData>> Wallets([FromBody]SignatureRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput<DataResponse<List<WalletData>>>(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out DataResponse<List<WalletData>> response)) {
                    return response;
                }
                var data = HostRoot.Current.WalletSet.GetAll();
                return DataResponse<List<WalletData>>.Ok(request.MessageId, data);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<DataResponse<List<WalletData>>>(request.MessageId, e.Message);
            }
        }
        #endregion

        #region AddOrUpdateWallet
        [HttpPost]
        public ResponseBase AddOrUpdateWallet([FromBody]DataRequest<WalletData> request) {
            if (request == null || request.Data == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                HostRoot.Current.WalletSet.AddOrUpdate(request.Data);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region RemoveWallet
        [HttpPost]
        public ResponseBase RemoveWallet([FromBody]DataRequest<Guid> request) {
            if (request == null || request.Data == Guid.Empty) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                HostRoot.Current.WalletSet.Remove(request.Data);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region CalcConfigs
        // 挖矿端实时展示理论收益的功能需要调用此服务所以调用此方法不需要登录
        [HttpPost]
        public DataResponse<List<CalcConfigData>> CalcConfigs([FromBody]CalcConfigsRequest request) {
            try {
                var data = HostRoot.Current.CalcConfigSet.GetAll();
                return DataResponse<List<CalcConfigData>>.Ok(request.MessageId, data);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<DataResponse<List<CalcConfigData>>>(request.MessageId, e.Message);
            }
        }
        #endregion

        #region SaveCalcConfigs
        [HttpPost]
        public ResponseBase SaveCalcConfigs([FromBody]SaveCalcConfigsRequest request) {
            if (request == null || request.Data == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                HostRoot.Current.CalcConfigSet.SaveCalcConfigs(request.Data);
                Write.DevLine("SaveCalcConfigs");
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region ColumnsShows
        [HttpPost]
        public DataResponse<List<ColumnsShowData>> ColumnsShows([FromBody]SignatureRequest request) {
            if (request == null) {
                return ResponseBase.InvalidInput<DataResponse<List<ColumnsShowData>>>(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out DataResponse<List<ColumnsShowData>> response)) {
                    return response;
                }
                var data = HostRoot.Current.ColumnsShowSet.GetAll();
                return DataResponse<List<ColumnsShowData>>.Ok(request.MessageId, data);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError<DataResponse<List<ColumnsShowData>>>(request.MessageId, e.Message);
            }
        }
        #endregion

        #region AddOrUpdateColumnsShow
        [HttpPost]
        public ResponseBase AddOrUpdateColumnsShow([FromBody]DataRequest<ColumnsShowData> request) {
            if (request == null || request.Data == null) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                HostRoot.Current.ColumnsShowSet.AddOrUpdate(request.Data);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion

        #region RemoveColumnsShow
        [HttpPost]
        public ResponseBase RemoveColumnsShow([FromBody]DataRequest<Guid> request) {
            if (request == null || request.Data == Guid.Empty) {
                return ResponseBase.InvalidInput(Guid.Empty, "参数错误");
            }
            try {
                if (!request.IsValid(HostRoot.Current.UserSet.GetUser, ClientIp, out ResponseBase response)) {
                    return response;
                }
                HostRoot.Current.ColumnsShowSet.Remove(request.Data);
                return ResponseBase.Ok(request.MessageId);
            }
            catch (Exception e) {
                Logger.ErrorDebugLine(e.Message, e);
                return ResponseBase.ServerError(request.MessageId, e.Message);
            }
        }
        #endregion
    }
}
