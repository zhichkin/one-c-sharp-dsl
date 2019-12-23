﻿using OneCSharp.AST.Model;
using OneCSharp.Core;
using OneCSharp.Core.Services;
using OneCSharp.MVVM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace OneCSharp.AST.UI
{
    public sealed class Module : IModule
    {
        private const string MODULE_FILE = "module.json";
        private const string CATALOG_PATH = "OneCSharp.AST.UI";
        public const string SOLUTION = "pack://application:,,,/OneCSharp.AST.UI;component/images/Solution.png";
        public const string ADD_LANGUAGE = "pack://application:,,,/OneCSharp.AST.UI;component/images/AddLanguage.png";
        public const string ADD_NAMESPACE = "pack://application:,,,/OneCSharp.AST.UI;component/images/AddNamespace.png";
        public const string NAMESPACE_PUBLIC = "pack://application:,,,/OneCSharp.AST.UI;component/images/NamespacePublic.png";

        private IShell _shell;
        private readonly Dictionary<Type, IController> _controllers = new Dictionary<Type, IController>();
        private readonly IOneCSharpJsonSerializer _serializer;
        public Module()
        {
            _serializer = new OneCSharpJsonSerializer();
            var knownTypes = _serializer.Binder.KnownTypes;
            knownTypes.Add(1, typeof(Language));
            knownTypes.Add(2, typeof(Namespace));
        }
        private string GetModuleFilePath()
        {
            string path = Path.Combine(_shell.AppCatalogPath, CATALOG_PATH);
            if (!Directory.Exists(path))
            {
                _ = Directory.CreateDirectory(path);
            }
            string file = Path.Combine(path, MODULE_FILE);
            if (!File.Exists(file))
            {
                using (StreamWriter writer = File.CreateText(file))
                {
                    writer.Close();
                }
            }
            return file;
        }
        public void Initialize(IShell shell)
        {
            _shell = shell;

            _controllers.Add(typeof(Language), new LanguageController(this));
            _controllers.Add(typeof(Namespace), new NamespaceController(this));
            
            _shell.AddMenuItem(new MenuItemViewModel()
            {
                MenuItemIcon = new BitmapImage(new Uri(ADD_LANGUAGE)),
                MenuItemHeader = "Add new language",
                MenuItemCommand = new RelayCommand(AddLanguage),
                MenuItemPayload = this
            });

            ReadModuleFromFile();
        }
        public IController GetController<T>()
        {
            return _controllers[typeof(T)];
        }
        public IController GetController(Type type)
        {
            return _controllers[type];
        }
        private void AddLanguage(object parameter)
        {
            InputStringDialog dialog = new InputStringDialog();
            _ = dialog.ShowDialog();
            if (dialog.Result == null) return;

            Language language = new Language()
            {
                Name = (string)dialog.Result
            };

            IController controller = GetController<Language>();
            controller.BuildTreeNode(language, out TreeNodeViewModel treeNode);

            Persist(language);
            _shell.AddTreeNode(treeNode);
        }
        public void Persist(Entity model)
        {
            string json = _serializer.ToJson(model);

            string filePath = GetModuleFilePath();
            using (StreamWriter writer = File.CreateText(filePath))
            {
                writer.Write(json);
            }
        }
        private void ReadModuleFromFile()
        {
            string filePath = GetModuleFilePath();
            string json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json)) return;

            Entity entity = _serializer.FromJson(json);
            BuildTreeView(entity);
        }
        private void BuildTreeView(Entity entity)
        {
            BuildTreeNodeRecursively(entity, out TreeNodeViewModel treeNode);
            _shell.AddTreeNode(treeNode);
        }
        private void BuildTreeNodeRecursively(Entity entity, out TreeNodeViewModel target)
        {
            IController controller = GetController(entity.GetType());
            controller.BuildTreeNode(entity, out target);

            Type entityType = entity.GetType();
            foreach(PropertyInfo property in entityType.GetProperties())
            {
                PropertyPurposeAttribute purpose = property.GetCustomAttribute<PropertyPurposeAttribute>();
                if (purpose != null && purpose.Purpose == PropertyPurpose.Children)
                {
                    IEnumerable source = (IEnumerable)property.GetValue(entity);
                    BuildTreeNodesRecursively(source, target.TreeNodes);
                }
            }
        }
        private void BuildTreeNodesRecursively(IEnumerable entities, ObservableCollection<TreeNodeViewModel> treeNodes)
        {
            foreach (Entity entity in entities)
            {
                BuildTreeNodeRecursively(entity, out TreeNodeViewModel treeNode);
                treeNodes.Add(treeNode);
            }
        }
    }
}