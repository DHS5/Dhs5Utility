using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public class UIGenericTransitioner : UITransitioner
    {
        #region Members

        [SerializeField] protected List<Graphic> m_graphics = new();
        [SerializeField] protected List<UIGenericTransitionInstance> m_transitionInstances = new();

        #endregion

        #region Properties

        public int GraphicsCount => m_graphics.Count;
        public IEnumerable<Graphic> Graphics => m_graphics;

        #endregion

        #region Setters

        public virtual bool TryGetGraphicAtIndex(int index, out Graphic graphic)
        {
            return m_graphics.IsIndexValid(index, out graphic);
        }
        public virtual void AddGraphic(Graphic graphic)
        {
            m_graphics ??= new();
            if (!m_graphics.Contains(graphic))
            {
                m_graphics.Add(graphic);
            }
        }
        public virtual void InsertGraphic(int index, Graphic graphic)
        {
            m_graphics ??= new();
            if (!m_graphics.Contains(graphic))
            {
                m_graphics.Insert(index, graphic);
            }
        }
        public virtual bool RemoveGraphic(Graphic graphic)
        {
            return m_graphics != null && m_graphics.Remove(graphic);
        }
        public virtual void RemoveGraphicAt(int index)
        {
            m_graphics?.RemoveAt(index);
        }
        public virtual void ClearGraphics()
        {
            m_graphics?.Clear();
        }

        #endregion

        #region Process

        public override void UpdateState(FUIState oldStates, FUIState newStates, bool instant, IUITransitionParam param)
        {
            var graphics = m_graphics.Where(g => g != null);

            if (m_transitionInstances.IsValid())
            {
                foreach (var instance in m_transitionInstances)
                {
                    instance.UpdateState(graphics, oldStates, newStates, instant, param);
                }
            }
        }

        #endregion
    }
}
