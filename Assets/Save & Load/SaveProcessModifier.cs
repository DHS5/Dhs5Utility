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
        /// Writes the <paramref name="content"/> of the save file at <paramref name="path"/> using the method of your choice.
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
