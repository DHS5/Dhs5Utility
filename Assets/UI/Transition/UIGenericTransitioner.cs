using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public class UIGenericTransitioner : UITransitioner
    {
        #region Members

        [SerializeField] private List<Graphic> m_graphics;
        [SerializeField] private List<UIGenericTransitionData> m_transitionDatas;
        [SerializeField] private UIGenericTransitionSelector m_selector;

        #endregion

        #region Process

        public override void UpdateState(FUIState oldStates, FUIState newStates, bool instant, IUITransitionParam param)
        {
            var graphics = m_graphics.Where(g => g != null);

            foreach (var data in m_transitionDatas)
            {
                data.UpdateState(graphics, oldStates, newStates, instant, param);
            }
        }

        #endregion
    }
}
