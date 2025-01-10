using Dhs5.Utility.Databases;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using Dhs5.Utility.Editors;
#endif

namespace Dhs5.Utility.Updates
{
    public class UpdateTimeline : BaseDataContainerScriptableElement, IUpdateTimeline
    {
        #region Members

        [SerializeField] private UpdatePicker m_update;
        [SerializeField] private int m_minutesDuration = 1;
        [SerializeField] private float m_secondsDuration;
        [SerializeField] private bool m_loop;
        [SerializeField] private float m_timescale = 1f;
        [SerializeField] private List<IUpdateTimeline.Event> m_events;

        #endregion

        #region IUpdateTimeline

        public int UpdateKey
        {
            get
            {
                if (m_update.TryGetUpdateKey(out int updateKey)) return updateKey;
                return 0;
            }
        }
        public float Duration => m_minutesDuration * 60f + m_secondsDuration;
        public bool Loop => m_loop;
        public float Timescale => m_timescale;

        public IEnumerable<IUpdateTimeline.Event> GetSortedEvents()
        {
            List<IUpdateTimeline.Event> sortedEvents = new(m_events);
            sortedEvents.Sort((e1, e2) => e1.normalizedTime.CompareTo(e2.normalizedTime));
            return sortedEvents;
        }

        #endregion
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(UpdateTimeline))]
    public class UpdateTimelineDatabaseElementEditor : BaseDataContainerScriptableElementEditor
    {
        #region Members

        protected UpdateTimeline m_element;

        protected SerializedProperty p_update;
        protected SerializedProperty p_minutesDuration;
        protected SerializedProperty p_secondsDuration;
        protected SerializedProperty p_loop;
        protected SerializedProperty p_timescale;
        protected SerializedProperty p_events;

        protected int m_selectedEventIndex = -1;

        // GUI Parameters
        protected GUIStyle m_eventTimeStyle;
        protected GUIStyle m_eventTimeSelectedStyle;
        protected Color m_timelineColor = new Color(.4f, .4f, .4f);
        protected Color m_timelineTimesColor = new Color(.6f, .6f, .6f);
        private float m_handleDemiWidth = 10f;

        #endregion

        #region Properties

        protected bool HasValidSelection => m_selectedEventIndex > -1 && m_selectedEventIndex < p_events.arraySize;

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            m_element = (UpdateTimeline)target;

            p_update = serializedObject.FindProperty("m_update");
            p_minutesDuration = serializedObject.FindProperty("m_minutesDuration");
            p_secondsDuration = serializedObject.FindProperty("m_secondsDuration");
            p_loop = serializedObject.FindProperty("m_loop");
            p_timescale = serializedObject.FindProperty("m_timescale");
            p_events = serializedObject.FindProperty("m_events");

            m_excludedProperties.Add(p_update.propertyPath);
            m_excludedProperties.Add(p_minutesDuration.propertyPath);
            m_excludedProperties.Add(p_secondsDuration.propertyPath);
            m_excludedProperties.Add(p_loop.propertyPath);
            m_excludedProperties.Add(p_timescale.propertyPath);
            m_excludedProperties.Add(p_events.propertyPath);

            m_eventTimeStyle = new GUIStyle()
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 10,
            };
            m_eventTimeSelectedStyle = new GUIStyle()
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 10,
                normal = new GUIStyleState()
                {
                    textColor = Color.cyan,
                }
            };
        }

        #endregion

        #region GUI

        protected override void OnGUI()
        {
            EditorGUILayout.PropertyField(p_update, true);
            p_timescale.floatValue = Mathf.Max( EditorGUILayout.FloatField("Timescale", p_timescale.floatValue) , 0f);

            EditorGUILayout.Space(10f);

            Rect rect;
            // Timeline Properties
            {
                rect = EditorGUILayout.GetControlRect(false, 20f);

                float loopRectWidth = 65f;
                p_loop.boolValue = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, loopRectWidth, rect.height), "Loop", p_loop.boolValue);

                float secondsDurationWidth = 60f;
                float minutesDurationWidth = 60f;
                float durationLabelWidth = 70f;
                float margin = 5f;

                float baseLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 15f;

                p_secondsDuration.floatValue = Mathf.Clamp( EditorGUI.DelayedFloatField(
                    new Rect(rect.x + rect.width - secondsDurationWidth, rect.y, secondsDurationWidth, rect.height), 
                    "s", p_secondsDuration.floatValue) , 0f, 60f);
                p_minutesDuration.intValue = Mathf.Max( EditorGUI.DelayedIntField(
                    new Rect(rect.x + rect.width - secondsDurationWidth - margin - minutesDurationWidth, rect.y, minutesDurationWidth, rect.height), 
                    "m", p_minutesDuration.intValue) , 0);
                EditorGUI.LabelField(
                    new Rect(rect.x + rect.width - secondsDurationWidth - margin - minutesDurationWidth - margin - durationLabelWidth, rect.y, durationLabelWidth, rect.height),
                    "Duration:");

                EditorGUIUtility.labelWidth = baseLabelWidth;
            }

            // Timeline
            {
                rect = EditorGUILayout.GetControlRect(false, 50f);

                float timesRectHeight = 15f;
                float handlesRectHeight = 22f;
                var eventsTimeRect = new Rect(rect.x, rect.y, rect.width, timesRectHeight);
                var eventsHandleRect = new Rect(rect.x, rect.y + rect.height - handlesRectHeight, rect.width, handlesRectHeight);

                EditorGUI.DrawRect(rect, m_timelineColor);
                EditorGUI.DrawRect(eventsTimeRect, m_timelineTimesColor);

                if (m_element.Duration > 0f)
                {
                    for (int i = 0; i < p_events.arraySize; i++)
                    {
                        if (i != m_selectedEventIndex)
                        {
                            DrawEventHandle(eventsTimeRect, eventsHandleRect, i, p_events.GetArrayElementAtIndex(i), false);
                        }
                    }
                    if (HasValidSelection)
                    {
                        DrawEventHandle(eventsTimeRect, eventsHandleRect, m_selectedEventIndex, p_events.GetArrayElementAtIndex(m_selectedEventIndex), true);
                    }
                }
            }

            // Timeline Buttons
            {
                rect = EditorGUILayout.GetControlRect(false, 20f);

                float buttonsWidth = 40f;

                Color guiColor = GUI.color;
                GUI.color = Color.green;
                EditorGUI.BeginDisabledGroup(m_element.Duration <= 0f);
                if (GUI.Button(new Rect(rect.x + rect.width - buttonsWidth, rect.y, buttonsWidth, rect.height), EditorGUIHelper.AddIcon))
                {
                    p_events.InsertArrayElementAtIndex(0);
                    p_events.GetArrayElementAtIndex(0).FindPropertyRelative("normalizedTime").floatValue = 0.5f;
                    p_events.GetArrayElementAtIndex(0).FindPropertyRelative("id").intValue = 0;
                    m_selectedEventIndex = 0;
                }
                EditorGUI.EndDisabledGroup();

                GUI.color = Color.red;
                EditorGUI.BeginDisabledGroup(!HasValidSelection);
                if (GUI.Button(new Rect(rect.x + rect.width - buttonsWidth * 2f, rect.y, buttonsWidth, rect.height), EditorGUIHelper.DeleteIcon))
                {
                    p_events.DeleteArrayElementAtIndex(m_selectedEventIndex);
                    m_selectedEventIndex = -1;
                }
                EditorGUI.EndDisabledGroup();
                GUI.color = guiColor;
            }

            // Events
            {
                if (m_element.Duration > 0f && HasValidSelection)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    Color guiColor = GUI.color;
                    GUI.color = Color.cyan;

                    SerializedProperty p_event = p_events.GetArrayElementAtIndex(m_selectedEventIndex);

                    SerializedProperty p_eventTime = p_event.FindPropertyRelative("normalizedTime");
                    SerializedProperty p_eventID = p_event.FindPropertyRelative("id");

                    EditorGUILayout.LabelField("Event " + p_eventID.intValue, EditorStyles.boldLabel);

                    EditorGUI.indentLevel++;
                    p_eventTime.floatValue = EditorGUILayout.Slider(p_eventTime.floatValue, 0f, 1f);
                    EditorGUILayout.PropertyField(p_eventID, true);
                    EditorGUI.indentLevel--;

                    GUI.color = guiColor;

                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void DrawEventHandle(Rect timeRect, Rect handleRect, int index, SerializedProperty p_event, bool selected)
        {
            Color guiColor = GUI.color;

            float eventNormalizedTime = p_event.FindPropertyRelative("normalizedTime").floatValue;
            float eventTime = eventNormalizedTime * m_element.Duration;
            float xPos = Mathf.Lerp(timeRect.x + m_handleDemiWidth, timeRect.x + timeRect.width - m_handleDemiWidth, eventNormalizedTime);

            EditorGUI.LabelField(new Rect(xPos - 10f, timeRect.y, 20f, timeRect.height), ((int)eventTime / 60) + ":" + (eventTime % 60), selected ? m_eventTimeSelectedStyle : m_eventTimeStyle);

            EditorGUI.DrawRect(new Rect(xPos - 2f, timeRect.y + timeRect.height, 4f, handleRect.y - timeRect.y - timeRect.height), m_timelineTimesColor);

            if (selected) GUI.color = Color.cyan;
            if (GUI.Button(new Rect(xPos - m_handleDemiWidth, handleRect.y, m_handleDemiWidth * 2f, handleRect.height), 
                new GUIContent(p_event.FindPropertyRelative("id").intValue.ToString())))
            {
                if (!selected)
                {
                    m_selectedEventIndex = index;
                }
                else
                {
                    m_selectedEventIndex = -1;
                }
            }
            GUI.color = guiColor;
        }

        #endregion
    }

#endif
}
