using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SourceGenerator
{
    /// <summary>
    /// Created on demand before each generation pass
    /// </summary>
    internal class DbContextSyntaxReceiver : ISyntaxContextReceiver
    {
        public List<INamedTypeSymbol> Classes { get; } = new List<INamedTypeSymbol>();
        public Dictionary<INamedTypeSymbol, string[]> Keys { get; } = new Dictionary<INamedTypeSymbol, string[]>();

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            //has to be a class declaration with at least one base type:
            if (!IsClassDeclarationWithBaseClass(context.Node)) return;
            //if (!(context.Node is ClassDeclarationSyntax classDeclarationSyntax)
            //    || classDeclarationSyntax.BaseList == null
            //    || !classDeclarationSyntax.BaseList.Types.Any())
            //    return;
            var classDeclarationSyntax = context.Node as ClassDeclarationSyntax;

            //has to be a partial declaration:
            if (!HasPartialModifier(classDeclarationSyntax)) return;

            //base type has to derive from DbContext
            if (!DerivesFromDbContext(classDeclarationSyntax)) return;

            // save the DbContext class:
            var symbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDeclarationSyntax) as INamedTypeSymbol; //is there an ISymbol interface more specific to interfaces?
            Classes.Add(symbol);
            
            // Scan OnModelCreating method for key declarations:
            var onModelCreatingMethodBody = ScanForOnModelCreatingMethodCall(classDeclarationSyntax);

            if (onModelCreatingMethodBody == null) return;

            // find statements containing a call to HasKey method
            var keyDefinitions = ScanForHasKeyMethodCalls(onModelCreatingMethodBody);

            // check statements for type and key field name data:
            foreach (var hasKeyMethodSyntax in keyDefinitions
                         .Select(s => s.Expression)
                         .OfType<InvocationExpressionSyntax>())
            {
                FindKeyProperties(context, hasKeyMethodSyntax);
            }
        }

        private static bool DerivesFromDbContext(ClassDeclarationSyntax classDeclarationSyntax)
        {
            return classDeclarationSyntax.BaseList.Types.Any(t => t.ToString().EndsWith("DbContext"));
        }

        private static bool HasPartialModifier(ClassDeclarationSyntax classDeclarationSyntax)
        {
            return classDeclarationSyntax.Modifiers
                .Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        }

        private static bool IsClassDeclarationWithBaseClass(SyntaxNode node)
        {
            return node is ClassDeclarationSyntax s && s.BaseList != null && s.BaseList.Types.Any();
        }

        private void FindKeyProperties(GeneratorSyntaxContext context, InvocationExpressionSyntax hasKeyMethodSyntax)
        {
            // get the type the key def is for:
            var typeForKeyDefinition = GetHasKeyGenericTypeArgument(context, hasKeyMethodSyntax);

            // get the property names that compose the key:
            if (hasKeyMethodSyntax.DescendantNodes().OfType<LambdaExpressionSyntax>().Any())
            {
                FindKeyPropertiesInLambdaDeclaration(hasKeyMethodSyntax, typeForKeyDefinition);
            }
            else
            {
                FindKeyPropertiesInStringParamDeclaration(hasKeyMethodSyntax, typeForKeyDefinition);
            }
        }

        private void FindKeyPropertiesInStringParamDeclaration(InvocationExpressionSyntax hasKeyMethodSyntax,
            INamedTypeSymbol typeForKeyDefinition)
        {
            // for the other version: modelBuilder.Entity<C>().HasKey("Key1", "Key2")
            var fields = hasKeyMethodSyntax
                .ArgumentList
                .Arguments
                .Select(s => s.Expression)
                .OfType<LiteralExpressionSyntax>()
                .Select(s => s.Token.Value as string)
                .ToArray();

            Keys.Add(typeForKeyDefinition, fields);
        }

        private void FindKeyPropertiesInLambdaDeclaration(InvocationExpressionSyntax hasKeyMethodSyntax,
            INamedTypeSymbol typeForKeyDefinition)
        {
            // for this form: modelBuilder.Entity<C>().HasKey(e => new {e.Key1, e.Key2})
            var fields = hasKeyMethodSyntax
                .DescendantNodes()
                .OfType<AnonymousObjectMemberDeclaratorSyntax>()
                .Select(s => s.Expression)
                .OfType<MemberAccessExpressionSyntax>()
                .Select(s => s.Name.ToString())
                .ToArray();

            Keys.Add(typeForKeyDefinition, fields);
        }

        private static ExpressionStatementSyntax[] ScanForHasKeyMethodCalls(BlockSyntax onModelCreatingMethodBody)
        {
            return onModelCreatingMethodBody
                .Statements
                .Where(s => s.DescendantNodes().OfType<IdentifierNameSyntax>().Any(n => n.ToString().Equals("HasKey")))
                .OfType<ExpressionStatementSyntax>()
                .ToArray();
        }

        private static BlockSyntax ScanForOnModelCreatingMethodCall(ClassDeclarationSyntax classDeclarationSyntax)
        {
            return classDeclarationSyntax
                .Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(s => s.Identifier.ToString().Equals("OnModelCreating"))
                ?.Body;
        }


        private static INamedTypeSymbol GetHasKeyGenericTypeArgument(GeneratorSyntaxContext context, InvocationExpressionSyntax HasKeyMethodSyntax)
        {
            var symbolInfo = context.SemanticModel.GetSymbolInfo(HasKeyMethodSyntax);

            var methodSymbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
            return (methodSymbol as IMethodSymbol).ContainingType.TypeArguments.OfType<INamedTypeSymbol>().First();
        }

    }
}
