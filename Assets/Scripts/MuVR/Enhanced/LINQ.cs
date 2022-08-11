using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MuVR.Enhanced {
	
	// Additions to Linq
	public static class Linq {
		// Function that returns an iterator with the array index attached
		public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> data) {
			return data.Select((item, index) => (item, index));
		}

		// Function that flattens a container of containers into an array, returning the flattened array and another array indicating where each of the original arrays starts.
		// This allows very simple preparation of arrays of arrays of data for handoff to Unity Jobs
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