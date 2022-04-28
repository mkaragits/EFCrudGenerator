#pragma warning disable CS8625
namespace SourceGenerator.Common
{
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class GenerateForTypeAttribute : Attribute
    {
        public Type Type { get; }
        public Type IdType { get; }
        public string TypeNamePlural { get; }

        public GenerateForTypeAttribute(Type type, Type idType, string typeNamePlural = null)
        {
            Type = type;
            IdType = idType;
            TypeNamePlural = typeNamePlural;
        }
    }
}