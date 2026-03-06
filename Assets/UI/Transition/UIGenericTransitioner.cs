using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public class UIGenericTransitioner : UITransitioner
    {
        #region Members

        [SerializeField] protected List<Graphic> m_graphics;
        [SerializeField] protected List<UIGenericTransitionInstance> m_transitionInstances;

        #endregion

        #region Process

        public override void UpdateState(FUIState oldStates, FUIState newStates, bool instant, IUITransitionParam param)
        {
            var graphics = m_graphics.Where(g => g != null);

            foreach (var instance in m_transitionInstances)
            {
                instance.UpdateState(graphics, oldStates, newStates, instant, param);
            }
        }

        #endregion
    }
}
