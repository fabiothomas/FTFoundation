using FTFoundation.BuildInReferences;
using FTFoundation.Core;

namespace FTFoundation.Samples.GettingStarted
{
  // [Service] is all it takes to register a class with the container. No manual wiring, and no
  // reference back to whatever ends up consuming it. SINGLETON means one instance for the whole
  // application; see the README's "Service Lifetimes" section for SCOPED and TRANSIENT.
  [Service(typeof(IGreetingService), ServiceType.SINGLETON)]
  public class GreetingService : IGreetingService
  {
    // Built-in services are injected exactly the same way as your own.
    [Inject] private ILoggerService Logger { get; set; }

    public string Greet(string name)
    {
      string message = $"Hello, {name}! Welcome to FTFoundation.";
      Logger.Log(message);
      return message;
    }
  }
}
