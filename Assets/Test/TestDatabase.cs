using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Database("Test/First", dataType = typeof(ScriptableObject))]
public class TestDatabase : FileDatabase<TestDatabase>
{
    
}
