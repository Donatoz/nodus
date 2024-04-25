using System;

namespace Nodus.Core.Extensions;

public static class ValidationExtensions
{
    public static T MustBe<T>(this T o, object val, string onFailure) where T : class
    {
        if (o != val)
        {
            throw new Exception(onFailure);
        }
        
        return o;
    }

    public static T MustNotBe<T>(this T o, object val, string onFailure) where T : class
    {
        if (o == val)
        {
            throw new Exception(onFailure);
        }

        return o;
    }

    public static T NotNull<T>(this T? o, string? onFailure = null) where T : class
    {
        if (o == null)
        {
            throw new Exception(onFailure);
        }

        return o;
    }
    
    public static T NotDefault<T>(this T? o, string? onFailure = null)
    {
        if (o?.Equals(default) ?? true)
        {
            throw new Exception(onFailure);
        }

        return o;
    }

    public static TType MustBeOfType<TType>(this object? o, string? onFailure = null)
    {
        if (o?.GetType() != typeof(TType))
        {
            throw new Exception(onFailure ?? $"The object must be of type {typeof(TType)}. Provided: {o?.GetType()}");
        }
        
        return (TType) o;
    }

    public static TType MustBe<TType>(this object? o, string? onFailure = null)
    {
        if (o is not TType t)
        {
            throw new Exception(onFailure ?? $"The object must be {typeof(TType)}. Provided: {o?.GetType()}");
        }

        return t;
    }

    public static float MustBeNumber(this object? o, string? onFailure = null)
    {
        if (o is float f) return f;
        
        if (!o.IsNumber())
        {
            throw new Exception(onFailure ?? $"The object must be a number. Provided: {o?.GetType()}");
        }

        return Convert.ToSingle(o);
    }
    
    public static T Default<T>(this object? o, Func<T> defaultValueFactory) where T : class
    {
        return o == null ? defaultValueFactory.Invoke() : o as T;
    }
}