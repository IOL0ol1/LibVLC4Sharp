using System;

namespace LibVLCSharp.Core.Interop
{
    [AttributeUsage(AttributeTargets.Property
        | AttributeTargets.Field
        | AttributeTargets.Parameter
        | AttributeTargets.ReturnValue
        | AttributeTargets.Struct
        | AttributeTargets.Enum,
        AllowMultiple = false, Inherited = true)]
    public class NativeTypeNameAttribute : Attribute
    {
        public NativeTypeNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
