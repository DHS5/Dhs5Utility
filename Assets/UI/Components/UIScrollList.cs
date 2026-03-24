using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dhs5.Utility.UI
{
    public class UIScrollList : UISelectable
    {
        #region CLASS OptionData

        [Serializable]
        /// <summary>
        /// Class to store the text and/or image of a single option in the scroll list
        /// </summary>
        public class OptionData
        {
            #region Members

            [SerializeField] protected string m_text;
            [SerializeField] protected UIComponentOptionAsset m_asset;

            #endregion

            #region Properties

            /// <summary>
            /// The text associated with the option
            /// </summary>
            public virtual string Text
            {
                get => m_text;
                set => m_text = value;
            }

            /// <summary>
            /// Asset containing the option's extra datas
            /// </summary>
            public virtual UIComponentOptionAsset Asset
            {
                get => m_asset;
                set => m_asset = value;
            }

            #endregion

            #region Constructors

            public OptionData() { }

            public OptionData(string text)
            {
                this.Text = text;
            }

            public OptionData(UIComponentOptionAsset asset)
            {
                this.Asset = asset;
            }

            public OptionData(string text, UIComponentOptionAsset asset)
            {
                this.Text = text;
                this.Asset = asset;
            }

            #endregion
        }

        #endregion

        #region ENUM Direction

        public enum EDirection
        {
            /// <summary>
            /// From the left to the right
            /// </summary>
            LeftToRight,

            /// <summary>
            /// From the right to the left
            /// </summary>
            RightToLeft,

            /// <summary>
            /// From the bottom to the top.
            /// </summary>
            BottomToTop,

            /// <summary>
            /// From the top to the bottom.
            /// </summary>
            TopToBottom,
        }

        #endregion


        #region Members

        [Header("Scroll List")]
        [SerializeField] protected RectTransform m_viewportRect;
        [SerializeField] protected UIScrollListItem m_templateItem;
        [SerializeField] protected Graphic m_placeholder;
        [SerializeField] protected UIButton m_leftButton;
        [SerializeField] protected UIButton m_rightButton;
        [Space]
        [SerializeField] protected int m_value = 0;
        [SerializeField] protected List<OptionData> m_options;
        [Space]
        [SerializeField] protected EDirection m_direction = EDirection.LeftToRight;
        [SerializeField] protected bool m_wrapAround = true;
        [SerializeField] protected float m_scrollDuration = 0.5f;
        [SerializeField] protected float m_itemsSpacing = 20f;

        protected List<UIScrollListItem> m_items = new();

        #endregion

        #region Properties

        public virtual RectTransform ViewportRect
        {
            get => m_viewportRect;
            set
            {
                if (SetPropertyUtility.SetClass(ref m_viewportRect, value))
                {
                    RefreshItemsSetup();
                    RefreshShownValues();
                }
            }
        }
        public virtual UIScrollListItem TemplateItem
        {
            get => m_templateItem;
            set
            {
                if (SetPropertyUtility.SetClass(ref m_templateItem, value))
                {
                    RefreshItemsSetup();
                    RefreshShownValues();
                }
            }
        }
        public virtual Graphic Placeholder
        {
            get => m_placeholder;
            set
            {
                if (SetPropertyUtility.SetClass(ref m_placeholder, value))
                {
                    RefreshShownValues();
                }
            }
        }
        public virtual UIButton LeftButton
        {
            get => m_leftButton;
            set
            {
                if (
#if UNITY_EDITOR
                    Application.isPlaying &&
#endif
                    m_leftButton != null)
                    m_leftButton.Clicked -= OnLeftButtonClicked;

                m_leftButton = value;

                if (
#if UNITY_EDITOR
                    Application.isPlaying &&
#endif
                    m_leftButton != null)
                    m_leftButton.Clicked += OnLeftButtonClicked;
            }
        }
        public virtual UIButton RightButton
        {
            get => m_rightButton;
            set
            {
                if (
#if UNITY_EDITOR
                    Application.isPlaying &&
#endif
                    m_rightButton != null)
                    m_rightButton.Clicked -= OnRightButtonClicked;

                m_leftButton = value;

                if (
#if UNITY_EDITOR
                    Application.isPlaying &&
#endif
                    m_rightButton != null)
                    m_rightButton.Clicked += OnRightButtonClicked;
            }
        }

        public virtual int Value
        {
            get => m_value;
            set => Set(value);
        }
        public virtual IEnumerable<OptionData> Options => m_options;

        public virtual EDirection Direction
        {
            get => m_direction;
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_direction, value))
                {
#if UNITY_EDITOR
                    if (Application.isPlaying)
#endif
                    {
                        // Update direction
                    }
                }
            }
        }
        public virtual bool WrapAround
        {
            get => m_wrapAround;
            set => m_wrapAround = value;
        }
        public virtual float ScrollDuration
        {
            get => m_scrollDuration;
            set => m_scrollDuration = value;
        }
        public virtual float ItemsSpacing
        {
            get => m_itemsSpacing;
            set
            {
                if (SetPropertyUtility.SetStruct(ref m_itemsSpacing, value))
                {
                    RefreshItemsSetup();
                    RefreshShownValues();
                }
            }
        }

        #endregion

        #region Events

        public event Action<int> ValueChanged;
        protected void TriggerValueChanged()
        {
            UISystemProfilerApi.AddMarker("ScrollList.value", this);
            EventContext = this;
            ValueChanged?.Invoke(m_value);
        }

        #endregion


        #region Setters

        protected virtual void Set(int value, bool triggerEvent = true)
        {
            if (m_value == value) return;

            m_value = value;

            if (triggerEvent)
            {
                TriggerValueChanged();
            }
        }
        public virtual void SetValueWithoutNotify(int value) => Set(value, triggerEvent:false);

        #endregion

        #region Visuals Update

        public virtual void RefreshShownValues()
        {

        }

        #endregion

        #region Items

        protected virtual void RefreshItemsSetup()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
        }

        #endregion

        #region Options

        /// <summary>
        /// Add multiple options to the options of the ScrollList based on a list of OptionData objects
        /// </summary>
        /// <param name="options">The list of OptionData to add</param>
        public virtual void AddOptions(IEnumerable<OptionData> options)
        {
            m_options.AddRange(options);
            RefreshShownValues();
        }

        public virtual void AddOption(OptionData option)
        {
            m_options.Add(option);
            RefreshShownValues();
        }

        /// <summary>
        /// Add multiple text-only options to the options of the ScrollList based on a list of strings
        /// </summary>
        /// <param name="options">The list of text strings to add</param>
        public virtual void AddOptions(List<string> options)
        {
            for (int i = 0; i < options.Count; i++)
                m_options.Add(new OptionData(options[i]));

            RefreshShownValues();
        }

        public virtual void AddOption(string option)
        {
            m_options.Add(new OptionData(option));
            RefreshShownValues();
        }

        /// <summary>
        /// Clear the list of options in the ScrollList
        /// </summary>
        public virtual void ClearOptions()
        {
            m_options.Clear();
            m_value = m_placeholder ? -1 : 0;
            RefreshShownValues();
        }

        #endregion


        #region Callbacks

        protected virtual void OnLeftButtonClicked()
        {

        }
        protected virtual void OnRightButtonClicked()
        {

        }

        #endregion

        #region Index Utility

        protected virtual int GetPreviousValue(int value)
        {
            if (value > 0) return value - 1;
            else if (WrapAround)
            {
                var nextValue = m_options.Count - 1 - value;
                while (nextValue < 0)
                {
                    nextValue = m_options.Count - nextValue;
                }
                return nextValue;
            }

            return 0;
        }
        protected virtual int GetNextValue(int value)
        {
            if (value < m_options.Count - 1) return value + 1;
            else if (WrapAround)
            {
                var nextValue = value + 1 - m_options.Count;
                while (nextValue >= m_options.Count)
                {
                    nextValue -= m_options.Count;
                }
                return nextValue;
            }

            return m_options.Count - 1;
        }

        #endregion
    }
}
