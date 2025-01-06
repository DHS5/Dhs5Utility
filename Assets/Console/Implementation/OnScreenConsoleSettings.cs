using Dhs5.Utility.Console;
using Dhs5.Utility.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Settings("Editor/OnScreen Console", Scope.User)]
public class OnScreenConsoleSettings : CustomSettings<OnScreenConsoleSettings>
{
    #region Predefined Commands

    [Header("Predefined Commands")]

    [SerializeField] private List<PredefinedConsoleCommand> m_predefinedCommands;

    public static IEnumerable<PredefinedConsoleCommand> PredefinedCommands => I != null ? I.m_predefinedCommands : null;

    #endregion

    #region Inputs

    [Header("Inputs")]

    [SerializeField] private InputActionReference m_openInputRef;
    [SerializeField] private InputActionReference m_closeInputRef;

    public static bool HasOpenConsoleInput(out InputAction action)
    {
        if (I != null && I.m_openInputRef != null)
        {
            action = I.m_openInputRef.ToInputAction();
            return action != null;
        }
        action = null;
        return false;
    }
    public static bool HasCloseConsoleInput(out InputAction action)
    {
        if (I != null && I.m_closeInputRef != null)
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
    [SerializeField, Min(10f)] private float m_inputRectHeight = 50f;
    [SerializeField, Min(10)] private int m_inputFontSize = 40;
    [SerializeField, ColorUsage(false)] private Color m_inputTextColor = Color.white;
    [SerializeField, ColorUsage(false)] private Color m_inputValidTextColor = Color.green;

    public static float InputRectWidthPercent => I != null ? I.m_inputRectWidth : 0.8f;
    public static float InputRectHeight => I != null ? I.m_inputRectHeight : 50f;
    public static int InputFontSize => I != null ? I.m_inputFontSize : 40;
    public static Color InputTextColor => I != null ? I.m_inputTextColor : Color.white;
    public static Color InputValidTextColor => I != null ? I.m_inputValidTextColor : Color.green;

    [Space(15f)]

    [Tooltip("Height of an option rect in pixels")]
    [SerializeField] private float m_optionRectHeight = 30f;
    [Tooltip("Max number of options displayed at the same time")]
    [SerializeField, Min(1)] private int m_maxOptionsDisplayed = 10;
    [SerializeField, Min(10f)] private int m_optionFontSize = 30;
    [SerializeField, ColorUsage(false)] private Color m_optionTextColor = Color.white;

    public static float OptionRectHeight => I != null ? I.m_optionRectHeight : 30f;
    public static int MaxOptionsDisplayed => I != null ? I.m_maxOptionsDisplayed : 10;
    public static int OptionFontSize => I != null ? I.m_optionFontSize : 30;
    public static Color OptionTextColor => I != null ? I.m_optionTextColor : Color.white;

    #endregion
}
