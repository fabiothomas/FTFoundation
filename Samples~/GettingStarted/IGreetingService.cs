namespace FTFoundation.Samples.GettingStarted
{
    // Your own services look exactly like this: a plain interface, with no dependency on
    // FTFoundation at all. The framework only cares about the implementation.
    public interface IGreetingService
    {
        string Greet(string name);
    }
}
