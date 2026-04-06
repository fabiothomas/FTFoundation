using System;

namespace FTFoundation.Core
{
    /// <summary>
    /// This attribute defines that a property will have it's contend injected
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class InjectAttribute : Attribute
    {
        
    }
}