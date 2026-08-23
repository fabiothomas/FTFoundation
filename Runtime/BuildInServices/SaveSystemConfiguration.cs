using System;
using System.Collections.Generic;
using FTFoundation.BuildInReferences;
using FTFoundation.Core;

namespace FTFoundation.BuildInServices
{
    [InstantiateOnStartup]
    [Service(typeof(ISaveSystemConfiguration), ServiceType.SINGLETON)]
    public class DefaultSaveSystemConfiguration : ISaveSystemConfiguration
    {
        public IReadOnlyList<ISaveable> Saveables { get; private set; } = new List<ISaveable>();

        void Inject(IFileSaveService fileSaveService)
        {
            Saveables = new List<ISaveable>
            {
                PrefsSaveable<string>.Create("test", "hello world"),
                PrefsSaveable<int>.Create("testInt", 42),
                PrefsSaveable<float>.Create("testFloat", 3.14f),
                PrefsSaveable<bool>.Create("testBool", true),

                FileSaveable<string>.Create("fileTest", "hello file world", fileSaveService),
                FileSaveable<int>.Create("fileTestInt", 123, fileSaveService),
                FileSaveable<float>.Create("fileTestFloat", 6.28f, fileSaveService),
                FileSaveable<bool>.Create("fileTestBool", false, fileSaveService),
                FileSaveable<TestData>.Create("fileTestData", new TestData("test", 1, 1.0f, true), fileSaveService)
            };
        }

        [Serializable]
        public class TestData
        {
            public string StringValue;
            public int IntValue;
            public float FloatValue;
            public bool BoolValue;

            public TestData(string stringValue, int intValue, float floatValue, bool boolValue)
            {
                StringValue = stringValue;
                IntValue = intValue;
                FloatValue = floatValue;
                BoolValue = boolValue;
            }

            public override string ToString()
            {
                return $"TestData(StringValue={StringValue}, IntValue={IntValue}, FloatValue={FloatValue}, BoolValue={BoolValue})";
            }
        }
    }
}