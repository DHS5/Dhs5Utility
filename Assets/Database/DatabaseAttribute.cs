using System;

namespace Dhs5.Utility.Databases
{
    public class DatabaseAttribute : Attribute
    {
        #region Constructor

        public DatabaseAttribute(string path)
        {
            this.path = path;
        }

        #endregion

        #region Members

        public readonly string path;
        public Type dataType;

        #endregion
    }
}
