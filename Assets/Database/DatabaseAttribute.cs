using System;

namespace Dhs5.Utility.Databases
{
    public class DatabaseAttribute : Attribute
    {
        #region Constructor

        public DatabaseAttribute(string path)
        {
            this.path = path;
            this.anyType = true;
        }
        public DatabaseAttribute(string path, Type dataType)
        {
            this.path = path;
            this.dataType = dataType;
            this.anyType = false;
        }

        #endregion

        #region Members

        public readonly string path;
        public readonly Type dataType;
        public readonly bool anyType;

        #endregion
    }
}
