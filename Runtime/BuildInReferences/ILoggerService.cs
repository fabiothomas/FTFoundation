namespace Scripts.References.Interfaces
{
    public interface ILoggerService
    {
        public bool Disabled { get; set; }

        public void Log(string message);
        public void LogWarning(string message);
        public void LogError(string message);
    }
}