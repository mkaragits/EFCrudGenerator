using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp
{
    [AttributeUsage(AttributeTargets.All)]
    internal sealed class TestAttribute : Attribute
    {
        public int Id { get; set; }
    }
}
