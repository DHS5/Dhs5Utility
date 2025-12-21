using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct SerializableDate
{
    #region Constructors

    public SerializableDate(DateTime dateTime)
    {
        this.m_day = dateTime.Day;
        this.m_month = dateTime.Month;
        this.m_year = dateTime.Year;
        
        this.m_hour = dateTime.Hour;
        this.m_minute = dateTime.Minute;
        this.m_second = dateTime.Second;
        this.m_millisecond = dateTime.Millisecond;
    }
    public SerializableDate(int day, int month, int year)
    {
        this.m_day = day; 
        this.m_month = month; 
        this.m_year = year;

        this.m_hour = 0;
        this.m_minute = 0;
        this.m_second = 0;
        this.m_millisecond = 0;
    }
    public SerializableDate(int day, int month, int year, int hour)
    {
        this.m_day = day; 
        this.m_month = month; 
        this.m_year = year;

        this.m_hour = hour;
        this.m_minute = 0;
        this.m_second = 0;
        this.m_millisecond = 0;
    }
    public SerializableDate(int day, int month, int year, int hour, int minute)
    {
        this.m_day = day; 
        this.m_month = month; 
        this.m_year = year;

        this.m_hour = hour;
        this.m_minute = minute;
        this.m_second = 0;
        this.m_millisecond = 0;
    }
    public SerializableDate(int day, int month, int year, int hour, int minute, int second)
    {
        this.m_day = day; 
        this.m_month = month; 
        this.m_year = year;

        this.m_hour = hour;
        this.m_minute = minute;
        this.m_second = second;
        this.m_millisecond = 0;
    }
    public SerializableDate(int day, int month, int year, int hour, int minute, int second, int millisecond)
    {
        this.m_day = day; 
        this.m_month = month; 
        this.m_year = year;

        this.m_hour = hour;
        this.m_minute = minute;
        this.m_second = second;
        this.m_millisecond = millisecond;
    }

    public static SerializableDate Now => new SerializableDate(DateTime.Now);

    #endregion

    #region Members

    // Date
    [SerializeField] private int m_year;
    [SerializeField] private int m_month;
    [SerializeField] private int m_day;
    
    // Time
    [SerializeField] private int m_hour;
    [SerializeField] private int m_minute;
    [SerializeField] private int m_second;
    [SerializeField] private int m_millisecond;

    #endregion

    #region Properties

    public int Day => m_day;
    public int Month => m_month;
    public int Year => m_year;

    public int Hour => m_hour;
    public int Minute => m_minute;
    public int Second => m_second;
    public int Millisecond => m_millisecond;

    #endregion

    #region To String

    public string ToStringDate(bool us = false)
    {
        if (us) return $"{Month:00}/{Day:00}/{Year:0000}";

        return $"{Day:00}/{Month:00}/{Year:0000}";
    }

    public string ToFullStringNoSeparator(bool includeSeconds, bool includeMilliseconds)
    {
        if (includeMilliseconds) return $"{Year:0000}{Month:00}{Day:00}{Hour:00}{Minute:00}{Second:00}{Millisecond:000}";
        if (includeSeconds) return $"{Year:0000}{Month:00}{Day:00}{Hour:00}{Minute:00}{Second:00}";
        return $"{Year:0000}{Month:00}{Day:00}{Hour:00}{Minute:00}";
    }

    #endregion
}

[AttributeUsage(AttributeTargets.Field)]
public class DateReadOnlyAttribute : PropertyAttribute
{

}

#region Attribute Drawer

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(DateReadOnlyAttribute))]
public class DateReadOnlyAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var p_year = property.FindPropertyRelative("m_year");
        var p_month = property.FindPropertyRelative("m_month");
        var p_day = property.FindPropertyRelative("m_day");
        var p_hour = property.FindPropertyRelative("m_hour");
        var p_minute = property.FindPropertyRelative("m_minute");
        var p_second = property.FindPropertyRelative("m_second");
        var p_millisecond = property.FindPropertyRelative("m_millisecond");

        EditorGUI.BeginProperty(position, label, property);

        EditorGUI.LabelField(position, $"{p_day.intValue:00}/{p_month.intValue:00}/{p_year.intValue:0000}   {p_hour.intValue:00}h{p_minute.intValue:00}:{p_second.intValue:00}:{p_millisecond.intValue:000}");

        EditorGUI.EndProperty();
    }
}

#endif

#endregion
