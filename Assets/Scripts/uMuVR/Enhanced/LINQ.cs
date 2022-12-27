using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace uMuVR.Enhanced {
	
	/// <summary>
	/// Additions to Linq
	/// </summary>
	public static class Linq {
		/// <summary>
		///		Extension function that returns an iterator with the array index attached
		/// </summary>
		/// <param name="data">The iterator to index</param>
		/// <returns>A new iterator where every entry is a tuple of the form (index, data).</returns>
		public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> data) {
			return data.Select((item, index) => (item, index));
		}
		
		/// <summary>
		///		Extension function that flattens a container of containers into an array, returning the flattened array and another array indicating where each of the original arrays starts.
		///		This allows very simple preparation of arrays of arrays of data for handoff to Unity Jobs
		/// </summary>
		/// <param name="data">The array of arrays to be flattened</param>
		/// <param name="concatenatedData">Output: The flattened array</param>
		/// <param name="starts">Output: Indices in the flattened array where each of the original sub arrays began</param>
		public static void ConcatenateWithStartIndices<T>([NotNull] this IEnumerable<IEnumerable<T>> data, out T[] concatenatedData, out long[] starts) {
			starts = new long[data.LongCount()];
			starts[0] = 0;

			var concat = data.First();
			foreach (var (enumerable, i) in data.WithIndex().Skip(1)) {
				starts[i] = concat.LongCount();
				concat = concat.Concat(enumerable);
			}

			concatenatedData = concat.ToArray();
		}
	}
}