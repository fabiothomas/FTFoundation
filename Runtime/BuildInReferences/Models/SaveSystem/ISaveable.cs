namespace FTFoundation.BuildInReferences
{
  public interface ISaveable
  {
    string Id { get; set; }
    bool IsDirty { get; }
    void Save();
    void Restore();
  }
}