namespace FTFoundation.BuildInReferences
{
    public class SerializableSaveable<T> : Saveable<T>
    {
        public SerializableSaveable(string id, T defaultValue)
            : base(id, defaultValue)
        {
        }

        public override void Save() { }
        public override void Restore() { }
    }
}