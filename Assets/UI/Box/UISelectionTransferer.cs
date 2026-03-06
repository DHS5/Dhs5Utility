using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public class UISelectionTransferer : MonoBehaviour, IUpdateSelectedHandler
    {
        #region Members

        [SerializeField] protected GameObject m_nextSelection;

        #endregion

        #region Properties

        public virtual GameObject NextSelection
        {
            get => m_nextSelection;
            set => m_nextSelection = value;
        }

        #endregion


        #region IUpdateSelectedHandler

        public virtual void OnUpdateSelected(BaseEventData eventData)
        {
            if (m_nextSelection != null)
            {
                eventData.selectedObject = m_nextSelection;
            }
        }

        #endregion
    }
}
