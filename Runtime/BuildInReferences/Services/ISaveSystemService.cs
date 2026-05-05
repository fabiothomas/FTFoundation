namespace FTFoundation.BuildInReferences
{
    public interface ISaveSystemService
    {
        public Saveable<T> GetSaveable<T>(string id);
        public void SaveAll();
        public void Restore();
    }
}