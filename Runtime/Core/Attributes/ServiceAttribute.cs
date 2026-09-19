using System;
using UnityEngine.Scripting;

namespace FTFoundation.Core
{
  /// <summary>
  /// This attribute defines a service that can be injected
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  public class ServiceAttribute : PreserveAttribute
  {
    public readonly Type Interface;
    public readonly ServiceType Type;
    public ServiceAttribute(Type _interface, ServiceType _type)
    {
      Interface = _interface;
      Type = _type;
    }
  }
}