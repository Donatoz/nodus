using System;
using System.Collections.Generic;
using System.Linq;
using Nodus.Core.Reactive;

namespace Nodus.Core.Extensions
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
            {
                action(item);
            }
        }

        public static void ReverseForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source.Reverse())
            {
                action(item);
            }
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
    }
}