using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif

namespace Dhs5.Utility.UI
{
    public abstract class UINavBox : Selectable, IUpdateSelectedHandler, IUIBoxable
    {
        #region Members

        [Tooltip("Whether to setup children on Start or let the setup be done manually\n" +
            "If this box has a parent box, uncheck this option as the parent will trigger the setup")]
        [SerializeField] protected bool m_setupOnStart;

        private UINavBox m_box;

        #endregion

        #region Properties

        public UINavBox Box
        {
            get => m_box;
            set
            {
                if (m_box != value)
                {
                    m_box = value;
                    OnSetParentBox(value);
                }
            }
        }

        public Selectable NextSelection { get; protected set; }

        #endregion

        #region Core Behaviour

        protected override void Start()
        {
            base.Start();

            if (m_setupOnStart)
            {
                SetupChildren();
            }
        }

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

        protected virtual void SetupChild(Selectable selectable, Navigation navigation)
        {
            selectable.navigation = navigation;

            if (selectable is IUIBoxable boxable)
            {
                boxable.Box = this;
            }
        }

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

        #region Child Navigation

        public abstract Selectable FindSelectableOnChildFailed(Selectable child, AxisEventData axisEventData);

        #endregion


        #region Box

        protected virtual void OnSetParentBox(UINavBox box)
        {
            SetupChildren();
        }

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(UINavBox), editorForChildClasses:true)]
    public class UINavBoxEditor : SelectableEditor
    {
        #region Members

        protected UINavBox m_box;

        protected List<string> m_excludedProperties = new();

        protected SerializedProperty p_script;
        protected SerializedProperty p_targetGraphic;
        protected SerializedProperty p_transition;
        protected SerializedProperty p_colors;
        protected SerializedProperty p_spriteState;
        protected SerializedProperty p_animationTriggers;
        protected SerializedProperty p_interactable;

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            m_box = target as UINavBox;

            p_script = serializedObject.FindProperty("m_Script");
            p_targetGraphic = serializedObject.FindProperty("m_TargetGraphic");
            p_transition = serializedObject.FindProperty("m_Transition");
            p_colors = serializedObject.FindProperty("m_Colors");
            p_spriteState = serializedObject.FindProperty("m_SpriteState");
            p_animationTriggers = serializedObject.FindProperty("m_AnimationTriggers");
            p_interactable = serializedObject.FindProperty("m_Interactable");

            m_excludedProperties.Add(p_script.propertyPath);
            m_excludedProperties.Add(p_targetGraphic.propertyPath);
            m_excludedProperties.Add(p_transition.propertyPath);
            m_excludedProperties.Add(p_colors.propertyPath);
            m_excludedProperties.Add(p_spriteState.propertyPath);
            m_excludedProperties.Add(p_animationTriggers.propertyPath);
            m_excludedProperties.Add(p_interactable.propertyPath);
        }

        #endregion

        #region GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(p_script);
            }

            EditorGUILayout.PropertyField(p_interactable);

            DrawPropertiesExcluding(serializedObject, m_excludedProperties.ToArray());

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }

#endif

    #endregion
}
