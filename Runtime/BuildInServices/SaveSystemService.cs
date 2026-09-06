using System.Collections.Generic;
using System.Linq;
using FTFoundation.BuildInReferences;
using FTFoundation.Core;

namespace FTFoundation.BuildInServices
{
    [InstantiateOnStartup]
    [Service(typeof(ISaveSystemService), ServiceType.SINGLETON)]
    public partial class SaveSystemService : ISaveSystemService
    {
        private readonly Dictionary<string, ISaveable> saveables = new();
        private IFileSaveService? _fileSaveService;
        private ILoggerService _loggerService = null!;

        void Inject(IReadOnlyList<ISaveSystemConfiguration> configurations, IDebugScreenService debugScreenService, ILoggerService loggerService, IFileSaveService? fileSaveService = null)
        {
            _fileSaveService = fileSaveService;
            _loggerService = loggerService;

            foreach (ISaveSystemConfiguration config in configurations)
            {
                foreach (ISaveable saveable in config.Saveables)
                {
                    if (saveables.ContainsKey(saveable.Id))
                    {
                        loggerService.LogWarning($"[SaveSystemService] Warning: Duplicate saveable ID '{saveable.Id}' found. Skipping.");
                        continue;
                    }
                    saveables[saveable.Id] = saveable;
                    if (saveable is Saveable<string> stringSaveable) debugScreenService.AddValueWatcher(saveable.Id, stringSaveable);
                    else if (saveable is Saveable<int> intSaveable) debugScreenService.AddValueWatcher(saveable.Id, intSaveable);
                    else if (saveable is Saveable<float> floatSaveable) debugScreenService.AddValueWatcher(saveable.Id, floatSaveable);
                    else if (saveable is Saveable<bool> boolSaveable) debugScreenService.AddValueWatcher(saveable.Id, boolSaveable);
                }
            }

            Restore();
        }

        public Saveable<T> GetSaveable<T>(string id)
        {
            if (!saveables.ContainsKey(id)) throw new SaveableDoesNotExistException(id);
            if (saveables[id] is not Saveable<T> stringSaveable) throw new WrongSaveableTypeException(id, typeof(Saveable<T>), saveables[id].GetType());
            return stringSaveable;
        }

        public void SaveAll()
        {
            saveables.Values.ToList().ForEach(saveable => saveable.Save());
            _fileSaveService?.Flush();
        }

        public void Restore()
        {
            foreach (ISaveable saveable in saveables.Values)
            {
                try
                {
                    saveable.Restore();
                }
                catch (System.Exception e)
                {
                    _loggerService.LogWarning($"[SaveSystemService] Failed to restore saveable '{saveable.Id}': {e.Message}");
                }
            }
        }
    }
}