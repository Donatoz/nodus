using System;
using System.Collections.Generic;
using System.Linq;
using Nodus.Core.Reactive;

namespace Nodus.Core.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }

        return source;
    }

    public static IEnumerable<T> ReverseForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source.Reverse())
        {
            action(item);
        }

        return source;
    }

    public static void AddAndInvalidate<T>(this IReactiveProperty<ICollection<T>> col, T item)
    {
        col.Value.Add(item);
        col.Invalidate();
    }
        
    public static void RemoveAndInvalidate<T>(this IReactiveProperty<ICollection<T>> col, T item)
    {
        col.Value.Remove(item);
        col.Invalidate();
    }

    public static void ClearAndInvalidate<T>(this IReactiveProperty<ICollection<T>> col)
    {
        col.Value.Clear();
        col.Invalidate();
    }

    public static void DisposeAll(this IEnumerable<IDisposable> col)
    {
        col.ForEach(x => x.Dispose());
    }
}