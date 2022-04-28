using AutoInterface;

namespace ConsoleApp
{
    [GenerateForType(typeof(Class1), typeof(int), "Classes")]
    public partial interface Interface1
    {
        void DoSomething();
    }
}
