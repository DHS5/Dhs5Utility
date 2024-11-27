using Dhs5.Utility.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Settings("Editor/OnScreen Console", Scope.User)]
public class OnScreenConsoleSettings : CustomSettings<OnScreenConsoleSettings>
{
    #region Inputs

    [Header("Inputs")]

    [SerializeField] private InputActionReference m_openInputRef;
    [SerializeField] private InputActionReference m_closeInputRef;

    public static bool HasOpenConsoleInput(out InputAction action)
    {
        if (I != null)
        {
            action = I.m_openInputRef.ToInputAction();
            return action != null;
        }
        action = null;
        return false;
    }
    public static bool HasCloseConsoleInput(out InputAction action)
    {
        if (I != null)
        {
            action = I.m_closeInputRef.ToInputAction();
            return action != null;
        }
        action = null;
        return false;
    }

    #endregion

    #region GUI

    [Header("GUI")]

    // INPUT
    [Tooltip("Width of the input rect in percent of the game view")]
    [SerializeField, Range(0.2f, 1f)] private float m_inputRectWidth = 0.8f;
    [Tooltip("Height of the input rect in pixels")]
    [SerializeField, Min(0f)] private float m_inputRectHeight = 50f;
    [SerializeField, Min(0f)] private int m_inputStyleFontSize = 40;
    [SerializeField, ColorUsage(false)] private Color m_inputStyleTextColor = Color.white;

    public static float InputRectWidthPercent => I != null ? I.m_inputRectWidth : 0.8f;
    public static float InputRectHeight => I != null ? I.m_inputRectHeight : 50f;
    public static int InputStyleFontSize => I != null ? I.m_inputStyleFontSize : 40;
    public static Color InputStyleTextColor => I != null ? I.m_inputStyleTextColor : Color.white;

    [Space(15f)]

    [Tooltip("Height of an option rect in pixels")]
    [SerializeField] private float m_optionRectHeight = 30f;
    [Tooltip("Max number of options displayed at the same time")]
    [SerializeField] private int m_maxOptionsDisplayed = 10;

    public static float OptionRectHeight => I != null ? I.m_optionRectHeight : 30f;
    public static float MaxOptionsDisplayed => I != null ? I.m_maxOptionsDisplayed : 10;

    #endregion
}
