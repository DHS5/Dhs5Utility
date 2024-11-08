using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour, IDatabaseElement
{
    [SerializeField] private bool _showTexture;
    [SerializeField] private Texture2D _texture;
    [SerializeField] private bool _showName;
    [SerializeField] private string _name;

    public bool HasDatabaseElementName(out string name)
    {
        name = _name;
        return _showName;
    }

    public bool HasDatabaseElementTexture(out Texture2D texture)
    {
        texture = _texture;
        return _showTexture;
    }
}
