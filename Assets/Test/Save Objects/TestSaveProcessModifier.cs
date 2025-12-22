using System;
using Dhs5.Utility.SaveLoad;
using UnityEngine;

public class TestSaveProcessModifier : SaveProcessModifier
{
    public override string CreateSavePath(SaveObject saveObject)
    {
        throw new NotImplementedException();
    }

    public override string GetDecryptedContent(string encryptedContent)
    {
        throw new NotImplementedException();
    }

    public override string GetEncryptedContent(string content)
    {
        throw new NotImplementedException();
    }

    public override string ReadSelectedSaveFileFromDisk()
    {
        throw new NotImplementedException();
    }

    public override bool TryHandleTypeDeserializingError(string typeName, out Type type)
    {
        throw new NotImplementedException();
    }

    public override void WriteToDisk(string path, string content)
    {
        throw new NotImplementedException();
    }
}
