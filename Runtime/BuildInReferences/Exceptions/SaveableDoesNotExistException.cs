using System;

namespace FTFoundation.BuildInReferences
{
    public class SaveableDoesNotExistException : Exception
    {
        public SaveableDoesNotExistException(string id)
            : base($"Saveable with id '{id}' does not exist")
        {
        }
    }
}