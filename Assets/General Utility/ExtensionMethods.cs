using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

public static class ExtensionMethods
{
    #region Vectors

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 ToVector2(this Vector3 vector3)
    {
        return vector3;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 ToVector2XZ(this Vector3 vector3)
    {
        return new Vector2(vector3.x, vector3.z);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 ToVector2YZ(this Vector3 vector3)
    {
        return new Vector2(vector3.y, vector3.z);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ToVector3(this Vector2 vector2)
    {
        return vector2;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ToVector3XZ(this Vector2 vector2)
    {
        return new Vector3(vector2.x, 0f, vector2.y);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ToVector3YZ(this Vector2 vector2)
    {
        return new Vector3(0f, vector2.x, vector2.y);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this Vector2 vector, float value)
    {
        return value >= Mathf.Min(vector.x, vector.y) && value <= Mathf.Max(vector.x, vector.y);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this Vector2Int vector, float value)
    {
        return value >= Mathf.Min(vector.x, vector.y) && value <= Mathf.Max(vector.x, vector.y);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Contains(this Vector2Int vector, int value)
    {
        return value >= Mathf.Min(vector.x, vector.y) && value <= Mathf.Max(vector.x, vector.y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsApproximatelyZero(this Vector3 vector)
    {
        return Mathf.Approximately(vector.x, 0.0f) && Mathf.Approximately(vector.y, 0.0f) && Mathf.Approximately(vector.z, 0.0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Max(this Vector2 vector)
    {
        return Mathf.Max(vector.x, vector.y);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Min(this Vector2 vector)
    {
        return Mathf.Min(vector.x, vector.y);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Max(this Vector3 vector)
    {
        return Mathf.Max(vector.x, vector.y, vector.z);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Min(this Vector3 vector)
    {
        return Mathf.Min(vector.x, vector.y, vector.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 SetX(this Vector3 vector, float x)
    {
        return new Vector3(x, vector.y, vector.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 SetY(this Vector3 vector, float y)
    {
        return new Vector3(vector.x, y, vector.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 SetZ(this Vector3 vector, float z)
    {
        return new Vector3(vector.x, vector.y, z);
    }

    #endregion

    #region Boolean

    public static int ToInt(this bool value)
    {
        return value ? 1 : 0;
    }

    #endregion

    #region Integers

    public static bool ToBool(this int value)
    {
        return value != 0;
    }

    #endregion

    #region Collections

    public static bool IsValid<T>(this ICollection<T> collection)
    {
        return collection != null && collection.Count > 0;
    }

    public static bool IsIndexValid<T>(this ICollection<T> collection, int index)
    {
        return collection.IsValid() && index >= 0 && index < collection.Count;
    }
    public static bool IsIndexValid<T>(this IList<T> collection, int index, out T value)
    {
        if (collection.IsValid() && index >= 0 && index < collection.Count)
        {
            value = collection[index];
            return true;
        }
        value = default(T);
        return false;
    }

    public static void Swap<T>(this IList<T> list, int indexA, int indexB)
    {
        if (IsIndexValid(list, indexA, out var valueA) && IsIndexValid(list, indexB, out var valueB)) 
        {
            list[indexA] = valueB;
            list[indexB] = valueA;
        }
    }

    public static List<T> Copy<T>(this List<T> list)
    {
        return new(list);
    }
    public static Dictionary<T, U> Copy<T, U>(this Dictionary<T, U> dico)
    {
        return new(dico);
    }

    public static void InitWithConstantValue<T>(this T[] array, T value)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = value;
        }
    }
    public static void InitWithConstantValue<T>(this ICollection<T> collection, int count, T value)
    {
        for (int i = 0; i < count; i++)
        {
            collection.Add(value);
        }
    }

    public static string ToString<T>(this ICollection<T> collection, string separator = "\n", bool showIndex = true)
    {
        StringBuilder sb = new();

        int i = 0;
        foreach (var item in collection)
        {
            if (showIndex)
            {
                sb.Append(i);
                sb.Append(". ");
            }
            sb.Append(item.ToString());
            sb.Append(separator);
            i++;
        }

        return sb.ToString();
    }

    #endregion

    #region Numerical Collections

    public static float Sum(this ICollection<float> collection)
    {
        float sum = 0f;

        foreach (var item in collection)
        {
            sum += item;
        }

        return sum;
    }
    public static int Sum(this ICollection<int> collection)
    {
        int sum = 0;

        foreach (var item in collection)
        {
            sum += item;
        }

        return sum;
    }
    public static Vector2 Sum(this ICollection<Vector2> collection)
    {
        Vector2 sum = Vector2.zero;

        foreach (var item in collection)
        {
            sum += item;
        }

        return sum;
    }
    public static Vector3 Sum(this ICollection<Vector3> collection)
    {
        Vector3 sum = Vector3.zero;

        foreach (var item in collection)
        {
            sum += item;
        }

        return sum;
    }

    public static float Average(this ICollection<float> collection)
    {
        return collection.Sum() / collection.Count;
    }
    public static float Average(this ICollection<int> collection)
    {
        return (float)collection.Sum() / collection.Count;
    }
    public static Vector2 Average(this ICollection<Vector2> collection)
    {
        return collection.Sum() / collection.Count;
    }
    public static Vector3 Average(this ICollection<Vector3> collection)
    {
        return collection.Sum() / collection.Count;
    }

    public static void InitWithRange(this ICollection<int> collection, int start, int count, int step)
    {
        for (int i = 0; i < count; i++)
        {
            collection.Add(start + i * step);
        }
    }
    public static void InitWithRange(this ICollection<float> collection, float start, int count, float step)
    {
        for (int i = 0; i < count; i++)
        {
            collection.Add(start + i * step);
        }
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

    #region String

    public static string PreParseFloat(this string str)
    {
        string result = "";
        foreach (var c in str)
        {
            if (char.IsDigit(c)) result += c;
            else if (c == '.') result += c;
            else if (c == ',') result += '.';
        }
        return result.Trim();
    }
    public static bool TryParseFloat(this string str, out float f)
    {
        string result = "";
        foreach (var c in str)
        {
            if (char.IsDigit(c)) result += c;
            else if (c == '.') result += c;
            else if (c == ',') result += '.';
        }
        return float.TryParse(result.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out f);
    }

    #endregion

    #region Color

    public static Color SetAlpha(this Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
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

    #region Random

    public static float GetRandomInRange(this Vector2 vector)
    {
        return UnityEngine.Random.Range(Mathf.Min(vector.x), Mathf.Max(vector.y));
    }
    public static int GetRandomInRange(this Vector2Int vector, bool maxInclusive)
    {
        return UnityEngine.Random.Range(Mathf.Min(vector.x), Mathf.Max(vector.y) + (maxInclusive ? 1 : 0));
    }

    public static T GetRandom<T>(this IList<T> collection)
    {
        return collection[UnityEngine.Random.Range(0, collection.Count)];
    }
    public static T GetRandom<T>(this T[] collection)
    {
        return collection[UnityEngine.Random.Range(0, collection.Length)];
    }

    public static bool TryGetRandomWithPredicate<T>(this ICollection<T> list, Predicate<T> predicate, out T value)
    {
        var filteredList = list.Where(item => predicate(item)).ToList();

        if (filteredList.Count <= 0)
        {
            value = default;
            return false;
        }

        value = filteredList[UnityEngine.Random.Range(0, filteredList.Count)];
        return true;
    }

    public static T GetRandomWithExceptions<T>(this IList<T> list, IList<T> exceptions)
    {
        IList<T> availableItem = list.Except(exceptions).ToList();

        if (availableItem.Count > 0) return availableItem[UnityEngine.Random.Range(0, availableItem.Count)];

        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    #endregion
}
