using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SourceGenerator
{
    internal class Context
    {
        public INamedTypeSymbol Class { get; set; }

        public string[] EntityTypes { get; set; }
    }
}
