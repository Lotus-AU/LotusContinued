using System;
using Newtonsoft.Json.Linq;

namespace Lotus.Extensions;

public static class NewtonsoftExtensions
{
    public static T Value<T>(this JToken token)
    {
        if (token == null || token.Type == JTokenType.Null)
            return default!;

        if (typeof(T) == typeof(string))
            return (T)(object)token.Cast<JValue>().Value.ToString();

        var jValue = token.Cast<JValue>();
        if (jValue == null) return default!;

        var raw = jValue.Value;

        if (typeof(T) == typeof(bool))   return (T)(object)raw.Unbox<bool>();
        if (typeof(T) == typeof(int))    return (T)(object)(int)raw.Unbox<long>();
        if (typeof(T) == typeof(long))   return (T)(object)raw.Unbox<long>();
        if (typeof(T) == typeof(float))  return (T)(object)(float)raw.Unbox<double>();
        if (typeof(T) == typeof(double)) return (T)(object)raw.Unbox<double>();

        throw new InvalidOperationException($"Unsupported type {typeof(T).Name} in Il2Cpp Value<T>");
    }
}