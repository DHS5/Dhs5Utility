using System;

namespace Dhs5.Utility.Databases
{
    /// <summary>
    /// Attribute used to specify the type of the objects contained in a Database
    /// </summary>
    /// <remarks>
    /// Databases are <see cref="BaseDataContainer"/> Singletons accessible via <see cref="Database.Get{T}()"/><br></br>
    /// If you don't need your container to be a Singleton, use <see cref="DataContainerAttribute"/> instead
    /// </remarks>
    public class DatabaseAttribute : DataContainerAttribute
    {
        #region Constructor

        /// <summary>
        /// Use this constructor to implement your own database that will be shown in the <see cref="DatabaseWindow"/>
        /// </summary>
        /// <param name="path">Path of your database in the <see cref="DatabaseWindow"/></param>
        public DatabaseAttribute(string path) : base()
        {
            this.path = path;
        }
        /// <summary>
        /// Use this constructor to implement a database with out-of-the-box editor features that will be shown in the <see cref="DatabaseWindow"/>
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dataType"></param>
        public DatabaseAttribute(string path, Type dataType) : base(dataType)
        {
            this.path = path;
        }

        #endregion

        #region Members

        /// <summary>
        /// Path of your database in the <see cref="DatabaseWindow"/>
        /// </summary>
        public readonly string path;

        public bool showInDatabaseWindow = true;

        #endregion
    }
}
