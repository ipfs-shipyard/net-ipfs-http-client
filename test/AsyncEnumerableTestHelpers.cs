using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    internal static class AsyncEnumerableTestHelpers
    {
        public static IEnumerable<T> ToEnumerable<T>(this IAsyncEnumerable<T> source)
        {
            return source.ToArrayAsync().GetAwaiter().GetResult();
        }

        public static async Task<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> source)
        {
            var list = new List<T>();
            await foreach (var item in source)
            {
                list.Add(item);
            }
            return list.ToArray();
        }
    }
}
