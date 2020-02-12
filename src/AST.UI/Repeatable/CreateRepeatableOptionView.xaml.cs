﻿using System;
using System.Windows.Controls;

namespace OneCSharp.AST.UI
{
    public partial class CreateRepeatableOptionView : TextBlock
    {
        public CreateRepeatableOptionView()
        {
            InitializeComponent();
        }
        private void HideOptionsAnimation_Completed(object sender, EventArgs args)
        {
            if (DataContext is ISyntaxNodeViewModel vm)
            {
                vm.HideOptionsCommand.Execute(args);
            }
        }
    }
}