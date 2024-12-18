using System;

namespace Dhs5.Utility.Databases
{
    public class DataContainerAttribute : Attribute
    {
        #region Constructor

        /// <summary>
        /// Use this attribute to implement your own data container
        /// </summary>
        public DataContainerAttribute()
        {
            this.anyType = true;
        }
        /// <summary>
        /// Use this attribute to implement a data container with out-of-the-box editor features
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dataType"></param>
        public DataContainerAttribute(Type dataType)
        {
            this.dataType = dataType;
            this.anyType = false;
        }

        #endregion

        #region Members

        /// <summary>
        /// Type of the data contained in the data container, must implement at least <see cref="IDataContainerElement"/> interface
        /// </summary>
        public readonly Type dataType;
        public readonly bool anyType;

        #endregion
    }
}
