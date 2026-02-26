using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public abstract class UINavigationBox : Selectable, IUpdateSelectedHandler
    {
        #region Properties

        public Selectable NextSelection { get; protected set; }

        #endregion


        #region Overrides

        public override sealed void OnSelect(BaseEventData eventData)
        {
            OnBeforeSelect(eventData);

            if (eventData is AxisEventData axisEventData)
            {
                NextSelection = GetFirstChildByDirection(axisEventData.moveDir);
            }
            else
            {
                NextSelection = GetDefaultFirstChild();
            }

            OnAfterSelect(eventData);
        }

        #endregion

        #region IUpdateSelectedHandler

        public void OnUpdateSelected(BaseEventData eventData)
        {
            OnBeforeNavigateToChild(eventData);

            eventData.selectedObject = NextSelection.gameObject;

            OnAfterNavigateToChild(eventData);
        }

        #endregion

        #region Virtuals

        // SELECT
        protected virtual void OnBeforeSelect(BaseEventData eventData) { }
        protected virtual void OnAfterSelect(BaseEventData eventData) { }

        // NAVIGATE
        protected virtual void OnBeforeNavigateToChild(BaseEventData eventData) { }
        protected virtual void OnAfterNavigateToChild(BaseEventData eventData) { }

        #endregion


        #region Child Setup

        /// <summary>
        /// Setup the <see cref="Selectable"/> children of this box
        /// </summary>
        /// <remarks>
        /// Should be possible to call this function repeatedly without problems
        /// </remarks>
        public abstract void SetupChildren();

        #endregion

        #region Child Selection

        /// <summary>
        /// Returns the default child to be selected when selecting this box without move direction
        /// </summary>
        protected abstract Selectable GetDefaultFirstChild();
        /// <summary>
        /// Returns the child to be selected when selecting this box with <paramref name="moveDirection"/>
        /// </summary>
        protected abstract Selectable GetFirstChildByDirection(MoveDirection moveDirection);

        #endregion
    }
}
