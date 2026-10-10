using Dhs5.Utility.NewDatabase;
using UnityEngine;

[CreateAssetMenu(fileName = "TestContainer", menuName = "Dhs5 Utility/Test/Container")]
[Database("Test", typeof(TestContainerObject))]
public class TestContainer : Container
{
    
}
