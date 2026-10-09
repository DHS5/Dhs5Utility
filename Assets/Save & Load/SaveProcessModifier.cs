using System;
using UnityEngine;

namespace Dhs5.Utility.SaveLoad
{
    public abstract class SaveProcessModifier : ScriptableObject
    {
        #region Error Handling

        /// <summary>
        /// Called when the type of a BaseSaveInfo or BaseSaveSubObject<br></br>
        /// can't be retrieved from <paramref name="typeName"/> to try to still retrieve it
        /// </summary>
        /// <returns>TRUE if able to find a correct type</returns>
        public abstract bool TryHandleTypeDeserializationError(string typeName, out Type type);

        /// <summary>
        /// Called when the load process encounters an <paramref name="exception"/>
        /// </summary>
        /// <returns>TRUE if the load process can continue</returns>
        public abstract bool TryHandleLoadException(Exception exception);

        #endregion

        #region Data Serialization

        /// <summary>
        /// Serializes the content of a save info or a sub object.<br></br>
        /// Override to use another serializer than <see cref="JsonUtility"/> (default), e.g. Newtonsoft Json.<br></br>
        /// Only the data of the save info and sub objects goes through this method :
        /// the save file structure itself (version, date, type and category names) always uses <see cref="JsonUtility"/>.<br></br>
        /// UnityEngine.Object references can't be saved whatever the serializer. Exceptions make the save process fail.
        /// </summary>
        /// <param name="data">The save info or sub object</param>
        public virtual string SerializeData(ScriptableObject data)
        {
            return JsonUtility.ToJson(data);
        }

        /// <summary>
        /// Fills <paramref name="data"/>, an already created save info or sub object, from <paramref name="content"/>.<br></br>
        /// Override to use another serializer than <see cref="JsonUtility"/> (default), together with <see cref="SerializeData"/>.<br></br>
        /// Save infos and sub objects are ScriptableObjects, created by the save system : the serializer must populate them, not create them
        /// (e.g. Newtonsoft's JsonConvert.PopulateObject).<br></br>
        /// Exceptions make the save info or sub object ignored (loadables get <see cref="ILoadable.LoadDefault"/> instead).
        /// </summary>
        /// <param name="saveVersion">Version of the save file, e.g. to keep reading saves written with a former serializer</param>
        public virtual void DeserializeData(string content, ScriptableObject data, int saveVersion)
        {
            JsonUtility.FromJsonOverwrite(content, data);
        }

        #endregion

        #region Save & Load Process

        // ENCRYPTION

        /// <summary>
        /// If the save file need to be encrypted, do it here.<br></br>
        /// Else, just return <paramref name="content"/>.
        /// </summary>
        /// <param name="content">Non-encrypted content that should be encrypted</param>
        public abstract string GetEncryptedContent(string content);
        /// <summary>
        /// If the save file need to be decrypted, do it here.<br></br>
        /// Else, just return <paramref name="encryptedContent"/>.
        /// </summary>
        /// <param name="encryptedContent">Encrypted content that should decrypted</param>
        public abstract string GetDecryptedContent(string encryptedContent);

        // PATH

        /// <summary>
        /// Create a save path, including the name of the save file and the extension, for <paramref name="saveObject"/>.
        /// </summary>
        public abstract string CreateSavePath(SaveObject saveObject, ISaveParameter parameter);

        // WRITE

        /// <summary>
        /// Writes the <paramref name="content"/> of the save file at <paramref name="path"/> using the method of your choice.<br></br>
        /// <paramref name="path"/> is a temporary path next to the one returned by <see cref="CreateSavePath"/> :
        /// once the file is written there, the save system replaces the actual save file with it,
        /// so an interrupted write never corrupts the previous save.<br></br>
        /// If nothing is written at <paramref name="path"/> (e.g. custom storage), the save system does nothing more.<br></br>
        /// Exceptions are caught and logged, and the save is considered failed.
        /// </summary>
        public abstract void WriteToDisk(string path, string content, ISaveParameter parameter);
        
        // READ

        /// <summary>
        /// To implement here :<br></br>
        /// - the identification of the save file selected from game/menu logic<br></br>
        /// - the reading of this save file
        /// </summary>
        public abstract string ReadSelectedSaveFileFromDisk();

        #endregion
    }
}
