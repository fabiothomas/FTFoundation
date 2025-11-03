using System;

namespace Scripts.Foundation.Attributes
{
    /// <summary>
    /// <para> Ensures that the singleton service is instantiated on startup instead of it's first injection </para>
    /// <para> This attribute only functions on Services of type Singleton and will otherwise be ignored </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class InstantiateOnStartupAttribute : Attribute
    {
        
    }
}