using FTFoundation.BuildInReferences;
using FTFoundation.Core;
using UnityEngine;

namespace FTFoundation.Samples.GettingStarted
{
  // Add this to any GameObject and press Play. Check the Console for the greeting.
  //
  // Demonstrates both injection styles on the same MonoBehaviour: property injection for the
  // service this behaviour actually needs, and method injection as an example of the alternative
  // (see the README's "Injecting Dependencies" section for when to prefer one over the other).
  public class GreetingBehaviour : MonoBehaviour
  {
    [Inject] private IGreetingService GreetingService { get; set; }

    [SerializeField] private string playerName = "Player";

    void Inject(ILoggerService logger)
    {
      logger.Log($"{nameof(GreetingBehaviour)} finished injection.");
    }

    void Awake()
    {
      ServiceProvider.Inject(this);
    }

    void Start()
    {
      GreetingService.Greet(playerName);
    }
  }
}
