using System;

namespace Dhs5.Utility.OldDatabase
{
    /// <summary>
    /// Attribute used to specify the type of the objects contained in a Database
    /// </summary>
    /// <remarks>
    /// Databases are <see cref="BaseDataContainer"/> Singletons accessible via <see cref="Database.Get{T}()"/><br></br>
    /// If you don't need your container to be a Singleton, use <see cref="DataContainerAttribute"/> instead
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class DatabaseAttribute : DataContainerAttribute
    {
        #region Constructor

        /// <summary>
        /// Use this constructor to implement your own database that will be shown in the Database window
        /// </summary>
        /// <param name="path">Path of your database in the Database window</param>
        public DatabaseAttribute(string path) : base()
        {
            this.path = path;
        }
        /// <summary>
        /// Use this constructor to implement a database with out-of-the-box editor features that will be shown in the Database window
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
        /// Path of your database in the Database window
        /// </summary>
        public readonly string path;

        public bool showInDatabaseWindow = true;

        #endregion
    }
}
