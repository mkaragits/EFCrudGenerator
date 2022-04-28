using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using SourceGenerator;

namespace TestConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            const string source = @"
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Foo
{
    public class A
    {
        public int AId { get; set; }
        public int OtherProp { get; set; }
    }

    public class B
    {
        [Key]
        public int KeyProp { get; set; }
        public int OtherProp { get; set; }
    }

    public class C
    {
        public int Key1 { get; set; }
        public string Key2 { get; set; }
    }

    internal partial class FooDbContext : DbContext
    {

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<C>()
                .HasKey(e => new
            {
                e.Key1,
                e.Key2
            });
            //modelBuilder.Entity<A>().HasKey(""KeyA"", ""KeyB"");
            modelBuilder.Entity<B>().HasIndex(e => e.OtherProp);

        }
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        // => modelBuilder.Entity<TraderMapping>().HasKey(e => new
        // {
        //     e.SourceId,
        //     e.TraderUsername
        // });

        public virtual DbSet<A> A { get; set; }
        public virtual DbSet<B> B { get; set; }
        public virtual DbSet<C> C { get; set; }
    }
}";

            var (diagnostics, output) = GetGeneratedOutput(source);

            if (diagnostics.Length > 0)
            {
                Console.WriteLine("Diagnostics:");
                foreach (var diag in diagnostics)
                {
                    Console.WriteLine("   " + diag.ToString());
                }
                Console.WriteLine();
                Console.WriteLine("Output:");
            }

            Console.WriteLine(output);
        }

        private static (ImmutableArray<Diagnostic>, string) GetGeneratedOutput(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = new List<MetadataReference>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (!assembly.IsDynamic)
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }
            references.Add(MetadataReference.CreateFromFile(typeof(DbContext).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(KeyAttribute).Assembly.Location));

            var compilation = CSharpCompilation.Create("foo", new SyntaxTree[] { syntaxTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // TODO: Uncomment these lines if you want to return immediately if the injected program isn't valid _before_ running generators
            //
            //ImmutableArray<Diagnostic> compilationDiagnostics = compilation.GetDiagnostics();

            //if (compilationDiagnostics.Any())
            //{
            //    return (compilationDiagnostics, "");
            //}

            ISourceGenerator generator = new Generator();

            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);

            return (generateDiagnostics, outputCompilation.SyntaxTrees.Last().ToString());
        }
    }
}
