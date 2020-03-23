using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Turkey
{
    static class IEnumerableExtensions
    {
        public static async Task ForEachAsync<T>(this IEnumerable<T> items, Func<T, Task> task)
        {
            foreach (T item in items)
            {
                await task(item);
            }
        }
    }
}
