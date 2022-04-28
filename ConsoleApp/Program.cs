using System;
using System.Linq;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Types in this assembly:");
            foreach (Type t in typeof(Program).Assembly.GetTypes())
            {
                Console.WriteLine(t.FullName);
            }
            Console.WriteLine(Environment.NewLine);
            foreach (var method in typeof(Interface1).GetMethods().Select(m=>m.Name))
                Console.WriteLine(method);
        }
    }
}
