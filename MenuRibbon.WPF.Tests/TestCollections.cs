using MenuRibbon.WPF.Utils;
using System;
using System.Runtime;
using Xunit;

namespace MenuRibbon.WPF.Tests
{
	public class TestCollections
	{
		[Fact]
		public void TestWeakList()
		{
			{
				var wo = new WeakReference(new object());
				GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
				Assert.False(wo.IsAlive);
			}
            {
				var wo = new WeakReference<object>(new object());
				GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
				Assert.False(wo.TryGetTarget(out var _));
			}




			var wl = new WeakList<object>();

			AddObject(wl);
			Assert.Single(wl);

			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
			AddObject(wl);
			Assert.Single(wl);

			var o = new object();
			wl.Add(o);
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
			Assert.Equal(2, wl.Count);

			int iC = 0;
			foreach (var item in wl) iC++;
			Assert.Equal(1, iC);
			Assert.Equal(2, wl.Count);

			GC.KeepAlive(o);
		}
		void AddObject(WeakList<object> wl) { wl.Add(new Object()); }

		[Fact]
		public void TestWeakSet()
		{
			var ws = new WeakSet<object>();

			var o = new Object();
			ws.Add(o);
			ws.Add(o);
			Assert.Single(ws);
			foreach (var item in ws) Assert.Equal(item, o);

			ws.Remove(o);
			Assert.Empty(ws);

			AddObject(ws);
			AddObject(ws);
			Assert.Equal(2, ws.Count);
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

			int iC = 0;
			foreach (var item in ws) iC++;
			Assert.Equal(0, iC);
			Assert.Equal(2, ws.Count);

			ws.WeakCleanup();
			Assert.Empty(ws);
		}
		void AddObject(WeakSet<object> ws) { ws.Add(new Object()); }

		[Fact]
		public void TestForEach()
		{
			// make sure it enumerates
			var l = new WeakList<string> { "hello" };
			var count = 0;
			l.ForEach(x => count++);
			Assert.Equal(1, count);
		}
	}
}
