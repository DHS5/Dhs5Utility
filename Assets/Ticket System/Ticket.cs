using System;
using UnityEngine;
using System.Runtime.CompilerServices;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.Utility.TicketSystem
{
    [Serializable]
    public struct Ticket
    {
        [SerializeField] private ulong m_uid;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ulong GetUID() { return m_uid; }

        public Ticket(ulong uid) { m_uid = uid; }
    }

    #region Drawer

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(Ticket))]
    public class TicketDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label.text += " : " + property.FindPropertyRelative("m_uid").ulongValue.ToString();
            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.LabelField(position, label);

            EditorGUI.EndProperty();
        }
    }

#endif

    #endregion
}
