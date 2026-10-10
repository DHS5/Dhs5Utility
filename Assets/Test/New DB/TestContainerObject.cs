using Dhs5.Utility.NewDatabase;
using UnityEngine;

[CreateAssetMenu(fileName = "TestContainerObject", menuName = "Dhs5 Utility/Test/Container Object")]
public class TestContainerObject : ScriptableObject, IContainerElement
{
    [SerializeField] private int m_uid;
    [SerializeField, ContainerDisplay(priority = 2, width = 100f), Tooltip("Oups")] private string m_alias;
    [Header("blabla")]
    [SerializeField, ContainerDisplay(priority = 5, width = 150f)] private Material m_mat;
    [Space(12f)]
    [SerializeField, ContainerDisplay(priority = 6, width = 200f)] private TestContainerObject m_obj;
    [SerializeField, ContainerDisplay(priority = 7, width = 200f)] private GameObject m_obj2;
    [SerializeField, ContainerDisplay(priority = 8, width = 200f)] private GameObject m_obj3;
    [SerializeField, ContainerDisplay(priority = 9, width = 200f)] private GameObject m_obj4;

    public int UID => m_uid;

    public string Alias => m_alias;

#if UNITY_EDITOR
    public void Editor_SetUID(int UID)
    {
        m_uid = UID;
    }
#endif
}
