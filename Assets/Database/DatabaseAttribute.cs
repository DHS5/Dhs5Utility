using System;

namespace Dhs5.Utility.Databases
{
    public class DatabaseAttribute : Attribute
    {
        #region Constructor

        /// <summary>
        /// Use this attribute to implement your own database that will be shown in the <see cref="DatabaseWindow"/>
        /// </summary>
        /// <param name="path">Path of your database in the <see cref="DatabaseWindow"/></param>
        public DatabaseAttribute(string path)
        {
            this.path = path;
            this.anyType = true;
        }
        /// <summary>
        /// Use this attribute to implement a database with out-of-the-box editor features that will be shown in the <see cref="DatabaseWindow"/>
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dataType"></param>
        public DatabaseAttribute(string path, Type dataType)
        {
            this.path = path;
            this.dataType = dataType;
            this.anyType = false;
        }

        #endregion

        #region Members

        /// <summary>
        /// Path of your database in the <see cref="DatabaseWindow"/>
        /// </summary>
        public readonly string path;
        /// <summary>
        /// Type of the data contained in the database, must implement at least <see cref="IDatabaseElement"/> interface
        /// </summary>
        public readonly Type dataType;
        public readonly bool anyType;

        public bool showInDatabaseWindow = true;

        #endregion
    }
}
