using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SourceGenerator
{
    internal class TemplateParam
    {
        public INamedTypeSymbol DbContextTypeSymbol { get; set; }
        public INamedTypeSymbol ClassTypeSymbol { get; set; }
        public string DbContextName => DbContextTypeSymbol.Name;
        public string Namespace => DbContextTypeSymbol.ContainingNamespace.ToDisplayString();
        public string TypeFullName => ClassTypeSymbol.ToDisplayString();
        public string TypeName => ClassTypeSymbol.Name;
        public IPropertySymbol[] Keys { get; set; }
    }
}
