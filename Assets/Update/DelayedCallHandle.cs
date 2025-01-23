using Dhs5.Utility.Updates;
using UnityEngine;

public struct DelayedCallHandle
{
    #region Constructors

    internal DelayedCallHandle(ulong key)
    {
        this.key = key;
    }

    public static DelayedCallHandle Empty = new(0);

    #endregion

    #region Members

    public readonly ulong key;

    #endregion


    #region Methods

    public void Kill()
    {
        if (key > 0) Updater.Instance.UnregisterDelayedCall(key);
    }

    #endregion

    #region Accessors

    public bool IsValid()
    {
        return key > 0 && Updater.Instance.DoesDelayedCallExist(key);
    }

    public float GetTimeLeft()
    {
        if (key > 0 && Updater.Instance.GetDelayedCallTimeLeft(key, out var timeLeft))
        {
            return timeLeft;
        }
        return -1f;
    }
    public int GetFramesLeft()
    {
        if (key > 0 && Updater.Instance.GetDelayedCallFramesLeft(key, out var framesLeft))
        {
            return framesLeft;
        }
        return -1;
    }

    #endregion
}
