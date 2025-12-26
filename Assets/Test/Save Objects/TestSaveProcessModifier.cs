using System;
using System.IO;
using Dhs5.Utility.SaveLoad;
using UnityEngine;

public class TestSaveProcessModifier : SaveProcessModifier
{
    public override string CreateSavePath(SaveObject saveObject, ISaveParameter parameter)
    {
        return Application.persistentDataPath + "/Save/SAVE.txt";
    }

    public override string GetDecryptedContent(string encryptedContent)
    {
        return encryptedContent;
    }

    public override string GetEncryptedContent(string content)
    {
        return content;
    }

    public override string ReadSelectedSaveFileFromDisk()
    {
        return File.ReadAllText(Application.persistentDataPath + "/Save/SAVE.txt");
    }

    public override bool TryHandleTypeDeserializationError(string typeName, out Type type)
    {
        type = null;
        return false;
    }

    public override void WriteToDisk(string path, string content, ISaveParameter parameter)
    {
        File.WriteAllText(path, content);
    }

    public override bool TryHandleLoadException(Exception exception)
    {
        if (exception is IndexOutOfRangeException)
        {
            return false;
        }

        return true;
    }
}
