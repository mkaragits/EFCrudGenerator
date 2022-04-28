using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGenerator
{
    [Generator]
    public class Generator : ISourceGenerator
    {
        //        private const string AttributeText = @"
        //using System;
        //namespace AutoInterface
        //{
        //    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
        //    internal sealed class GenerateInterfaceAttribute : Attribute
        //    {
        //        public GenerateIniterfaceAttribute() { }
        //    }

        //    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
        //    internal sealed class InterfaceKeyAttribute : Attribute
        //    {
        //        public IniterfaceKeyAttribute() { }
        //    }

        //}
        //";

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register the attribute source
            //context.RegisterForPostInitialization((i) => i.AddSource("GenerateForTypeAttribute.cs", AttributeText));
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new DbContextSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // retrieve the populated receiver 
            if (!(context.SyntaxContextReceiver is DbContextSyntaxReceiver receiver))
                return;

            var template = ReadResource("InterfaceTemplate.txt");

            foreach (var symbol in receiver.Classes)
            {
                var filename = symbol.Name + ".g.cs";
                var ifSource = ProcessClass(symbol, template, receiver.Keys);
                context.AddSource(filename, ifSource);
                Debug.WriteLine($"added source file {filename}");
            }
        }

        private string ProcessClass(INamedTypeSymbol classSymbol, string template, Dictionary<INamedTypeSymbol, string[]> keysDictionary)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return null; //TODO: issue a diagnostic that it must be top level
            }
            
            var classesToGenerateFor = classSymbol
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Select(ps => ps.Type)
                .OfType<INamedTypeSymbol>()
                .Where(nts => nts.IsGenericType && nts.Arity == 1)
                .Select(nts => nts.TypeArguments.First())
                .OfType<INamedTypeSymbol>()
                .Select(c => new TemplateParam()
                {
                    DbContextTypeSymbol = classSymbol,
                    ClassTypeSymbol = c
                })
                .ToArray();
            
            foreach (var templateParam in classesToGenerateFor)
            {
                FindKeys(templateParam, keysDictionary);
            }

            //TODO: generate generic crud accessor functions
            //TODO: generate interfaces
            //TODO: generate interface implementations

            //var source = template
            //    .Replace("{{NameSpace}}", namespaceName)
            //    .Replace("{{TypeName}}", typeFullName)
            //    .Replace("{{TypeShortName}}", typeName)
            //    .Replace("{{LowercaseTypeName}}", char.ToLower(typeName[0]) + typeName.Substring(1));


            return string.Empty;
        }

        private static void FindKeys(TemplateParam templateParam, Dictionary<INamedTypeSymbol, string[]> keysDictionary)
        {
            var properties = templateParam
                .ClassTypeSymbol
                .GetMembers()
                .OfType<IPropertySymbol>()
                .ToArray();

            //check if keys were found by the SyntaxReceiver:
            var key = keysDictionary.Keys.SingleOrDefault(k => k.Name == templateParam.ClassTypeSymbol.Name);
            if (key != null)
            {
                var props = properties.Where(p => keysDictionary[key].Contains(p.Name)).ToArray();
                templateParam.Keys = props;
                return;
            }


            //if NOT then check for KeyAttribute attributes:
            var propsHavingKeyAttribute = properties
                .Where(p => p.GetAttributes().Any(ad => ad.AttributeClass.Name.EndsWith("KeyAttribute")))
                .ToArray();
            if (propsHavingKeyAttribute.Length > 0)
            {
                templateParam.Keys = propsHavingKeyAttribute;
                return;
            }

            //failing the above find a property called Id or <Classname>Id
            var propsEndingInId = properties
                .Where(ps => ps.Name.ToLowerInvariant().Equals("id")
                    || ps.Name.ToLowerInvariant().Equals( templateParam.TypeName.ToLowerInvariant() + "id"))
                .ToArray();
            templateParam.Keys = propsEndingInId;
        }

        private static string ReadResource(string name)
        {
            // Determine path
            var assembly = Assembly.GetExecutingAssembly();
            var resourcePath = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(name));

            using (var stream = assembly.GetManifestResourceStream(resourcePath))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}
