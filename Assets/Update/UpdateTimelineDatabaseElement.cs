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
    public class UpdateTimelineDatabaseElement : BaseDataContainerScriptableElement
    {
        #region STRUCT Event

        [Serializable]
        public struct Event
        {
            public float time;
            public ushort id;
        }

        #endregion

        #region Members

        [SerializeField] private UpdatePicker m_update;
        [SerializeField] private int m_minutesDuration = 1;
        [SerializeField] private float m_secondsDuration;
        [SerializeField] private bool m_loop;
        [SerializeField] private float m_timescale = 1f;
        [SerializeField] private List<Event> m_events;

        #endregion

        #region Properties

        public float Duration => m_minutesDuration * 60f + m_secondsDuration;
        public bool Loop => m_loop;
        public float Timescale => m_timescale;

        #endregion

        #region Accessors

        public bool HasValidUpdate(out int updateKey)
        {
            return m_update.TryGetUpdateKey(out updateKey);
        }

        public IEnumerable<Event> GetSortedEvents()
        {
            List<Event> sortedEvents = new(m_events);
            sortedEvents.Sort((e1, e2) => e1.time.CompareTo(e2.time));
            return sortedEvents;
        }

        #endregion
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(UpdateTimelineDatabaseElement))]
    public class UpdateTimelineDatabaseElementEditor : BaseDataContainerScriptableElementEditor
    {
        #region Members

        protected UpdateTimelineDatabaseElement m_element;

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
        private float m_handleDemiWidth = 10f;

        #endregion

        #region Properties

        protected bool HasValidSelection => m_selectedEventIndex > -1 && m_selectedEventIndex < p_events.arraySize;

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            m_element = (UpdateTimelineDatabaseElement)target;

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

                EditorGUI.DrawRect(rect, Color.gray);

                var eventsTimeRect = new Rect(rect.x, rect.y, rect.width, 20f);
                var eventsHandleRect = new Rect(rect.x, rect.y + rect.height - 20f, rect.width, 20f);

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
                    p_events.GetArrayElementAtIndex(0).FindPropertyRelative("time").floatValue = m_element.Duration / 2f;
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
                    SerializedProperty p_event = p_events.GetArrayElementAtIndex(m_selectedEventIndex);

                    SerializedProperty p_eventTime = p_event.FindPropertyRelative("time");
                    SerializedProperty p_eventID = p_event.FindPropertyRelative("id");

                    EditorGUILayout.LabelField("Event " + p_eventID.intValue, EditorStyles.boldLabel);

                    EditorGUI.indentLevel++;
                    p_eventTime.floatValue = EditorGUILayout.Slider(p_eventTime.floatValue, 0f, m_element.Duration);
                    EditorGUILayout.PropertyField(p_eventID, true);
                    EditorGUI.indentLevel--;
                }
            }
        }

        private void DrawEventHandle(Rect timeRect, Rect handleRect, int index, SerializedProperty p_event, bool selected)
        {
            float eventTime;
            float xPos;

            Color guiColor = GUI.color;

            eventTime = p_event.FindPropertyRelative("time").floatValue;
            xPos = Mathf.Lerp(timeRect.x + m_handleDemiWidth, timeRect.x + timeRect.width - m_handleDemiWidth, eventTime / m_element.Duration);

            EditorGUI.LabelField(new Rect(xPos - 10f, timeRect.y, 20f, timeRect.height), eventTime.ToString(), selected ? m_eventTimeSelectedStyle : m_eventTimeStyle);

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
