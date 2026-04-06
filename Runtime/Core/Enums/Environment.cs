using System;

namespace FTFoundation.Core
{
    [Flags]
    public enum Environment
    {
        /// <summary>
        /// Development environment
        /// </summary>
        DEV = 0,
        /// <summary>
        /// Testing environment
        /// </summary>
        TEST = 1,
        /// <summary>
        /// Production environment
        /// </summary>
        PROD = 2,
        /// <summary>
        /// All environments
        /// </summary>
        ALL = DEV | TEST | PROD
    }
}