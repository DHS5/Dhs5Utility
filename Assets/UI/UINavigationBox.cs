using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public class UINavigationBox : UISelectable, IUpdateSelectedHandler
    {
        [SerializeField] private Selectable m_selectable;

        public void OnUpdateSelected(BaseEventData eventData)
        {
            eventData.selectedObject = m_selectable.gameObject;
        }
    }
}
