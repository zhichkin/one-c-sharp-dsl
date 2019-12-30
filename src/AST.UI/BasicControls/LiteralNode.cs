﻿using OneCSharp.AST.Model;

namespace OneCSharp.AST.UI
{
    public sealed class LiteralNode : SyntaxNode
    {
        private string _literal = string.Empty;
        public LiteralNode(ISyntaxNode owner) : base(owner) { }
        public LiteralNode(ISyntaxNode owner, Concept model) : base(owner, model) { }
        public string Literal
        {
            get { return _literal; }
            set { _literal = value; OnPropertyChanged(nameof(Literal)); }
        }
    }
}