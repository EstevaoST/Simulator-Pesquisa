using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Extensions
{
    public static float StdDev<E>(this IEnumerable<E> values, Func<E, float> selector)
    {
        float ret = 0;
        int count = values.Count();
        if (count > 1)
        {
            //Compute the Average
            float avg = values.Average(selector);

            //Perform the Sum of (value-avg)^2
            float sum = values.Sum(x => {
                float v = selector(x);
                return (v - avg) * (v - avg);
            });

            //Put it all together
            ret = Mathf.Sqrt(sum / count);
        }
        return ret;
    }

    public static Vector2 ToVec2XZ(this Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }
    public static Vector3 ToVec3XZ(this Vector2 v, float y = 0)
    {
        return new Vector3(v.x, y, v.y);
    }
}
