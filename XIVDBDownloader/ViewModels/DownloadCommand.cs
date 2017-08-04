﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using XIVDBDownloader.Constants;
using XIVDBDownloader.Models;

namespace XIVDBDownloader.ViewModels
{
    public class DownloadCommand :
        ICommand
    {
        private MainWindowViewModel viewModel;

        public DownloadCommand(
            MainWindowViewModel viewModel)
        {
            this.viewModel = viewModel;

            this.viewModel.PropertyChanged += (s, e) =>
            {
                this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            };
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) =>
            !string.IsNullOrEmpty(this.viewModel.SaveDirectory);

        public async void Execute(object parameter)
        {
            try
            {
                this.viewModel.IsEnabledDownload = false;

                await Task.Run(() => this.ExecuteCore());
            }
            finally
            {
                this.viewModel.IsEnabledDownload = true;
            }
        }

        private void AppendLineMessages(
            string message)
        {
            var action = new Action(() =>
            {
                this.viewModel.Messages += message + Environment.NewLine;
            });

            this.viewModel.View.Dispatcher.BeginInvoke(
                action,
                DispatcherPriority.Normal,
                null);
        }

        private void ExecuteCore()
        {
            switch (this.viewModel.DataModel)
            {
                case DataModels.Action:
                    this.DownloadAction();
                    break;

                default:
                    break;
            }
        }

        #region Download Action

        private void DownloadAction()
        {
            this.AppendLineMessages("Download Action.");

            var model = new ActionModel();

            // XIVDB からActionのリストを取得する
            model.GET(this.viewModel.Language);

            // 取得したリストをCSVに保存する
            model.SaveToCSV(
                Path.Combine(this.viewModel.SaveDirectory, "Action.csv"));

            this.AppendLineMessages("Download Action. Done.");
        }

        #endregion Download Action
    }
}