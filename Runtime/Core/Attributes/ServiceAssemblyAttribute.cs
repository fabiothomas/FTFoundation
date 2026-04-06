using System;

namespace FTFoundation.Core
{
    /// <summary>
    /// This attribute defines that an assembly contains content that can be injected 
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ServiceAssemblyAttribute : Attribute
    {
    
    }
}
