﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace NTMiner.Vms {
    public class CoinPageViewModel : ViewModelBase {
        private string _coinKeyword;
        private bool _isPoolTabSelected;
        private bool _isWalletTabSelected;
        private bool _isKernelTabSelected;

        public ICommand Add { get; private set; }
        public ICommand ClearKeyword { get; private set; }

        public CoinPageViewModel() {
            if (Design.IsInDesignMode) {
                return;
            }
            this.Add = new DelegateCommand(() => {
                int sortNumber = NTMinerRoot.Instance.CoinSet.Count == 0 ? 1 : NTMinerRoot.Instance.CoinSet.Max(a => a.SortNumber) + 1;
                new CoinViewModel(Guid.NewGuid()) {
                    SortNumber = sortNumber
                }.Edit.Execute(FormType.Add);
            });
            this.ClearKeyword = new DelegateCommand(() => {
                this.CoinKeyword = string.Empty;
            });
            CoinViewModel coinVm;
            if (AppContext.Instance.CoinVms.TryGetCoinVm(MinerProfile.CoinId, out coinVm)) {
                _currentCoin = coinVm;
            }
        }

        public AppContext.MinerProfileViewModel MinerProfile {
            get {
                return AppContext.Instance.MinerProfileVm;
            }
        }

        #region coin

        public string CoinKeyword {
            get => _coinKeyword;
            set {
                if (_coinKeyword != value) {
                    _coinKeyword = value;
                    OnPropertyChanged(nameof(CoinKeyword));
                    OnPropertyChanged(nameof(List));
                }
            }
        }

        private CoinViewModel _currentCoin;
        public CoinViewModel CurrentCoin {
            get { return _currentCoin; }
            set {
                if (_currentCoin != value && value != null && value.Id != Guid.Empty) {
                    _currentCoin = value;
                    OnPropertyChanged(nameof(CurrentCoin));
                }
            }
        }

        public List<CoinViewModel> List {
            get {
                List<CoinViewModel> list;
                if (!string.IsNullOrEmpty(CoinKeyword)) {
                    string keyword = this.CoinKeyword.ToLower();
                    list = AppContext.Instance.CoinVms.AllCoins.
                        Where(a => (!string.IsNullOrEmpty(a.Code) && a.Code.ToLower().Contains(keyword))
                            || (!string.IsNullOrEmpty(a.Algo) && a.Algo.ToLower().Contains(keyword))
                            || (!string.IsNullOrEmpty(a.EnName) && a.EnName.ToLower().Contains(keyword))).OrderBy(a => a.SortNumber).ToList();
                }
                else {
                    list = AppContext.Instance.CoinVms.AllCoins.OrderBy(a => a.SortNumber).ToList();
                }
                if (list.Count == 1) {
                    CurrentCoin = list.FirstOrDefault();
                }
                if (CurrentCoin == null) {
                    CurrentCoin = list.FirstOrDefault();
                }
                else {
                    CurrentCoin = list.FirstOrDefault(a => a.Id == CurrentCoin.Id);
                }
                return list;
            }
        }
        #endregion

        public bool IsPoolTabSelected {
            get => _isPoolTabSelected;
            set {
                if (_isPoolTabSelected != value) {
                    _isPoolTabSelected = value;
                    OnPropertyChanged(nameof(IsPoolTabSelected));
                }
            }
        }
        public bool IsWalletTabSelected {
            get => _isWalletTabSelected;
            set {
                if (_isWalletTabSelected != value) {
                    _isWalletTabSelected = value;
                    OnPropertyChanged(nameof(IsWalletTabSelected));
                }
            }
        }

        public bool IsKernelTabSelected {
            get => _isKernelTabSelected;
            set {
                if (_isKernelTabSelected != value) {
                    _isKernelTabSelected = value;
                    OnPropertyChanged(nameof(IsKernelTabSelected));
                }
            }
        }
    }
}
