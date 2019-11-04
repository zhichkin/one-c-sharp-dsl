﻿using Microsoft.VisualStudio.PlatformUI;
using System.Windows;

namespace OneCSharp.VisualStudio.UI
{
    public sealed class AddServerDialog : DialogWindow
    {
        private readonly AddServerDialogViewModel viewModel;
        public AddServerDialog()
        {
            //this.HasMaximizeButton = true;
            //this.HasMinimizeButton = true;
            this.Title = "Add server...";
            this.SizeToContent = SizeToContent.WidthAndHeight;
            viewModel = new AddServerDialogViewModel();
            viewModel.OnCancel = OnCancel;
            viewModel.OnConfirm = OnConfirm;
            this.Content = new AddServerDialogView(viewModel);
        }
        public object Result { get; private set; }
        private void OnConfirm(object result)
        {
            Result = result;
            this.Close();
        }
        private void OnCancel()
        {
            this.Close();
        }
    }
}
