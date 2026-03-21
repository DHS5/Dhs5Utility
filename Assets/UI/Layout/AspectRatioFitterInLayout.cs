using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public class AspectRatioFitterInLayout : UIBehaviour, ILayoutElement
    {
        #region ENUM AspectMode

        /// <summary>
        /// Specifies a mode to use to enforce an aspect ratio.
        /// </summary>
        public enum EAspectMode
        {
            /// <summary>
            /// The aspect ratio is not enforced
            /// </summary>
            None,
            /// <summary>
            /// Changes the height of the rectangle to match the aspect ratio.
            /// </summary>
            WidthControlsHeight,
            /// <summary>
            /// Changes the width of the rectangle to match the aspect ratio.
            /// </summary>
            HeightControlsWidth,
        }

        #endregion


        #region Members

        [SerializeField] protected int m_layoutPriority = 1;
        [SerializeField] protected EAspectMode m_aspectMode = EAspectMode.None;
        [SerializeField] protected float m_aspectRatio = 1f;

        protected float m_value;

        [System.NonSerialized] protected RectTransform m_rect;

#if UNITY_EDITOR
        // This "delayed" mechanism is required for case 1014834.
        protected bool m_delayedSetDirty = false;
#endif

        #endregion

        #region Properties

        /// <summary>
        /// The mode to use to enforce the aspect ratio.
        /// </summary>
        public virtual EAspectMode AspectMode 
        { 
            get => m_aspectMode; 
            set 
            { 
                if (SetPropertyUtility.SetStruct(ref m_aspectMode, value)) 
                    SetDirty(); 
            } 
        }

        /// <summary>
        /// The aspect ratio to enforce. This means width divided by height.
        /// </summary>
        public virtual float AspectRatio 
        { 
            get => m_aspectRatio; 
            set 
            { 
                if (SetPropertyUtility.SetStruct(ref m_aspectRatio, value)) 
                    SetDirty(); 
            } 
        }

        protected virtual RectTransform RectTransform
        {
            get
            {
                if (m_rect == null)
                    m_rect = GetComponent<RectTransform>();
                return m_rect;
            }
        }

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            SetDirty();
        }

        protected override void Start()
        {
            base.Start();

            //Disable the component if the aspect mode is not valid or the object state/setup is not supported with AspectRatio setup.
            if (!IsComponentValidOnObject())
                this.enabled = false;
        }

        protected override void OnDisable()
        {
            LayoutRebuilder.MarkLayoutForRebuild(RectTransform);

            base.OnDisable();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            SetDirty();
        }

        /// <summary>
        /// Function called when this RectTransform or parent RectTransform has changed dimensions.
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

        #endregion


        #region Value

        protected virtual void UpdateValue()
        {
            if (!IsActive() || !IsComponentValidOnObject())
                return;

            switch (m_aspectMode)
            {
#if UNITY_EDITOR
                case EAspectMode.None:
                    {
                        if (!Application.isPlaying)
                            m_aspectRatio = Mathf.Clamp(RectTransform.rect.width / RectTransform.rect.height, 0.001f, 1000f);

                        break;
                    }
#endif
                case EAspectMode.HeightControlsWidth:
                    {
                        m_value = RectTransform.rect.height * m_aspectRatio;
                        break;
                    }
                case EAspectMode.WidthControlsHeight:
                    {
                        m_value = RectTransform.rect.width / m_aspectRatio;
                        break;
                    }
            }
        }

        #endregion

        #region ILayoutElement

        public virtual float minWidth => m_aspectMode == EAspectMode.HeightControlsWidth ? m_value : -1f;

        public virtual float preferredWidth => m_aspectMode == EAspectMode.HeightControlsWidth ? m_value : -1f;
               
        public virtual float flexibleWidth => -1f;
               
        public virtual float minHeight => m_aspectMode == EAspectMode.WidthControlsHeight ? m_value : -1f;
               
        public virtual float preferredHeight => m_aspectMode == EAspectMode.WidthControlsHeight ? m_value : -1f;
               
        public virtual float flexibleHeight => -1f;
               
        public virtual int layoutPriority => m_layoutPriority;

        public virtual void CalculateLayoutInputHorizontal()
        {
            if (m_aspectMode == EAspectMode.HeightControlsWidth)
            {
                UpdateValue();
            }
        }

        public virtual void CalculateLayoutInputVertical()
        {
            if (m_aspectMode == EAspectMode.WidthControlsHeight)
            {
                UpdateValue();
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Mark the AspectRatioFitter as dirty.
        /// </summary>
        protected virtual void SetDirty()
        {
            if (!CanvasUpdateRegistry.IsRebuildingLayout())
            {
                LayoutRebuilder.MarkLayoutForRebuild(RectTransform);
            }
        }

        public virtual bool IsComponentValidOnObject()
        {
            Canvas canvas = gameObject.GetComponent<Canvas>();
            if (canvas && canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace)
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Editor

#if UNITY_EDITOR

        /// <summary>
        /// Update the rect based on the delayed dirty.
        /// Got around issue of calling onValidate from OnEnable function.
        /// </summary>
        protected virtual void Update()
        {
            if (m_delayedSetDirty)
            {
                m_delayedSetDirty = false;
                SetDirty();
            }
        }

        protected override void OnValidate()
        {
            m_aspectRatio = Mathf.Clamp(m_aspectRatio, 0.001f, 1000f);
            m_delayedSetDirty = true;
        }

#endif

        #endregion
    }
}
