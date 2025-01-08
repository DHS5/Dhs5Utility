using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtensionMethods
{
    #region Vectors

    public static Vector2 ToVector2(this Vector3 vector3)
    {
        return new Vector2(vector3.x, vector3.y);
    }

    public static Vector2 BrutMoveTowards(this Vector2 current, Vector2 target, float speed)
    {
        float num = target.x - current.x;
        float num2 = target.y - current.y;
        float num3 = num * num + num2 * num2;
        if (num3 == 0f)
        {
            return target;
        }

        float num4 = (float)Math.Sqrt(num3);
        return new Vector2(current.x + num / num4 * speed, current.y + num2 / num4 * speed);
    }

    public static float GetRandomInRange(this Vector2 vector)
    {
        return UnityEngine.Random.Range(Mathf.Min(vector.x), Mathf.Max(vector.y));
    }
    public static int GetRandomInRange(this Vector2Int vector, bool maxInclusive)
    {
        return UnityEngine.Random.Range(Mathf.Min(vector.x), Mathf.Max(vector.y) + (maxInclusive ? 1 : 0));
    }

    #endregion

    #region Collections

    //public static bool IsValid(this ICollection collection)
    //{
    //    return collection != null && collection.Count > 0;
    //}
    public static bool IsValid<T>(this ICollection<T> collection)
    {
        return collection != null && collection.Count > 0;
    }

    public static bool IsIndexValid<T>(this ICollection<T> collection, int index)
    {
        return collection.IsValid() && index >= 0 && index < collection.Count;
    }

    public static void Swap<T>(this IList<T> list, int indexA, int indexB)
    {
        T tmp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = tmp;
    }

    public static List<T> Copy<T>(this List<T> list)
    {
        return new(list);
    }
    public static Dictionary<T, U> Copy<T, U>(this Dictionary<T, U> dico)
    {
        return new(dico);
    }

    #endregion

    #region Enums

    public static IEnumerable<T> GetFlags<T>(this Enum input) where T : Enum
    {
        foreach (Enum value in Enum.GetValues(input.GetType()))
            if (input.HasFlag(value))
                yield return (T)value;
    }
    public static IEnumerable<int> GetFlagsIndex(this Enum input)
    {
        foreach (Enum value in Enum.GetValues(input.GetType()))
            if (input.HasFlag(value))
                yield return 1 >> Convert.ToInt32(value);
    }

    #endregion

    #region Layer Mask

    public static bool Contains(this LayerMask mask, int layer)
    {
        return (mask & (1 << layer)) != 0;
    }

    #endregion

    #region Clipboard

    public static void CopyToClipboard(this string str)
    {
        GUIUtility.systemCopyBuffer = str;
    }

    #endregion
}
