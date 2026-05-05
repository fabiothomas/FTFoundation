using System;

namespace FTFoundation.BuildInReferences
{
    public class WrongSaveableTypeException : Exception
    {
        public WrongSaveableTypeException(string id, System.Type expected, System.Type actual)
            : base($"Saveable with id '{id}' is of type '{actual}', but expected '{expected}'")
        {
        }
    }
}