using System;

namespace FTFoundation.Core
{
    /// <summary>
    /// This attribute defines that an assembly contains content that requires injection
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class InjectionTargetAssemblyAttribute : Attribute
    {
    
    }
}
