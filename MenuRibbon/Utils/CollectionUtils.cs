using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MenuRibbon.WPF.Utils
{
	public static class CollectionUtils
	{
		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			// REMARK: Do NOT use "yield" it will only enumerate what the callee explicitly enumerates
			foreach (var item in source)
				action(item);
			return source;
		}
	}
}
