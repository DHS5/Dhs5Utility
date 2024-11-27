using Dhs5.Utility.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Settings("Editor/OnScreen Console", Scope.User)]
public class OnScreenConsoleSettings : CustomSettings<OnScreenConsoleSettings>
{
    #region Members

    [Header("Inputs")]

    [SerializeField] private InputActionReference m_openInputRef;
    [SerializeField] private InputActionReference m_closeInputRef;

    #endregion

    #region Inputs Access

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
}
