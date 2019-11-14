﻿using Microsoft.VisualStudio.PlatformUI;
using OneCSharp.Metadata;
using OneCSharp.OQL.Model;
using OneCSharp.OQL.UI.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace OneCSharp.VisualStudio.UI
{
    public sealed class MetadataViewModel : ViewModelBase
    {
        private const string ONE_C_SHARP = "ONE-C-SHARP";
        private readonly MetadataProvider _metadataProvider = new MetadataProvider();
        public MetadataViewModel()
        {
            InitializeViewModel();
        }
        public ICommand AddServerCommand { get; private set; }
        private void InitializeViewModel()
        {
            this.AddServerCommand = new DelegateCommand(AddServer);
        }
        public ObservableCollection<ServerViewModel> Servers { get; } = new ObservableCollection<ServerViewModel>();
        public void AddServer()
        {
            AddServerDialog dialog = new AddServerDialog();
            _ = dialog.ShowModal();
            if (dialog.Result != null)
            {
                OnAddServer((string)dialog.Result);
            }
        }
        private void OnAddServer(string serverName)
        {
            DbServer server = _metadataProvider.Servers.Where(s => s.Address == serverName).FirstOrDefault();
            if (server != null) return;
            server = new DbServer() { Address = serverName };
            if (!_metadataProvider.CheckServerConnection(server))
            {
                var result = MessageBox.Show(
                    "Unable to connect server!",
                    ONE_C_SHARP,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            _metadataProvider.AddServer(server);
            ServerViewModel serverViewModel = new ServerViewModel(server, _metadataProvider);
            serverViewModel.Parent = this;
            Servers.Add(serverViewModel);
        }
        public object SelectedItem { get; set; }
        public void ImportInfoBase()
        {
            if (SelectedItem is ServerViewModel)
            {
                ServerViewModel server = (ServerViewModel)SelectedItem;
                List<InfoBase> infoBases = _metadataProvider.GetInfoBases(server.Model);
                SelectInfoBaseDialog dialog = new SelectInfoBaseDialog(infoBases);
                _ = dialog.ShowModal();
                if (dialog.Result != null)
                {
                    InfoBaseViewModel selectedItem = (InfoBaseViewModel)dialog.Result;
                    OnInfoBaseSelected(server, selectedItem);
                }
            }
        }
        private void OnInfoBaseSelected(ServerViewModel parent, InfoBaseViewModel child)
        {
            if (parent.Model.InfoBases
                .Where(ib => ib.Database == child.Model.Database)
                .FirstOrDefault() != null) return;

            child.Model.Server = parent.Model;
            _metadataProvider.ImportMetadata(child.Model);
            parent.AddInfoBase(child);
        }


        public void AddWebService()
        {
            if (SelectedItem == null)
            {
                _ = MessageBox.Show(
                    "Web service owner is not selected!",
                    ONE_C_SHARP,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string dbCatalogAddress;
            AddServerDialog dialog = new AddServerDialog();
            _ = dialog.ShowModal();
            if (dialog.Result == null)
            {
                return;
            }
            dbCatalogAddress = (string)dialog.Result;

            if (SelectedItem is ServerViewModel)
            {
                ServerViewModel server = (ServerViewModel)SelectedItem;
                server.CreateInfoBase(dbCatalogAddress);
            }
        }
        public void AddNamespace()
        {
            if (SelectedItem == null)
            {
                _ = MessageBox.Show(
                    "Namespace owner is not selected!",
                    ONE_C_SHARP,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string namespaceName;
            AddServerDialog dialog = new AddServerDialog();
            _ = dialog.ShowModal();
            if (dialog.Result == null)
            {
                return;
            }
            namespaceName = (string)dialog.Result;

            if (SelectedItem is InfoBaseViewModel)
            {
                InfoBaseViewModel ib = (InfoBaseViewModel)SelectedItem;
                ib.CreateNamespaceViewModel(namespaceName);

            }
            else if (SelectedItem is NamespaceViewModel)
            {
                NamespaceViewModel ns = (NamespaceViewModel)SelectedItem;
                ns.CreateNamespaceViewModel(namespaceName);
            }
        }
        public void AddProcedure()
        {
            if (SelectedItem == null)
            {
                _ = MessageBox.Show(
                    "Procedure owner is not selected!",
                    ONE_C_SHARP,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string name;
            AddServerDialog dialog = new AddServerDialog();
            _ = dialog.ShowModal();
            if (dialog.Result == null)
            {
                return;
            }
            name = (string)dialog.Result;

            if (SelectedItem is NamespaceViewModel)
            {
                NamespaceViewModel ns = (NamespaceViewModel)SelectedItem;
                ns.CreateProcedureViewModel(name);
            }
        }
        public void RenameProcedure()
        {
            if (SelectedItem == null)
            {
                _ = MessageBox.Show(
                    "Procedure is not selected!",
                    ONE_C_SHARP,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string name;
            AddServerDialog dialog = new AddServerDialog();
            _ = dialog.ShowModal();
            if (dialog.Result == null)
            {
                return;
            }
            name = (string)dialog.Result;

            if (SelectedItem is DbProcedureViewModel)
            {
                DbProcedureViewModel vm = (DbProcedureViewModel)SelectedItem;
                vm.Name = name;
                //TODO: think how to save changes !
            }
        }
        public void EditProcedure()
        {
            if (SelectedItem == null)
            {
                _ = MessageBox.Show(
                    "Procedure is not selected!",
                    ONE_C_SHARP,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            if (!(SelectedItem is DbProcedureViewModel procedure))
            {
                _ = MessageBox.Show(
                    $"{SelectedItem.GetType()} is not procedure !",
                    ONE_C_SHARP,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            if (!(SelectedItem is IOneCSharpCodeEditorConsumer consumer))
            {
                _ = MessageBox.Show(
                    $"{SelectedItem.GetType()} does not implement IOneCSharpCodeEditorConsumer interface !",
                    ONE_C_SHARP,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            // TODO: get Procedure from storage (ex. file)
            consumer.Metadata = _metadataProvider;
            Procedure syntaxNode = new Procedure() { Name = procedure.Name };
            ToolWindowManager.OpenOneCSharpCodeEditor(procedure.Name, consumer, syntaxNode);
        }
    }
}