using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MenuRibbon.WPF.Utils;
using System.Runtime;

namespace MenuRibbon.WPF.Tests
{
	[TestClass]
	public class TestCollections
	{
		[TestMethod]
		public void TestWeakList()
		{
			var wl = new WeakList<object>();

			AddObject(wl);
			Assert.AreEqual(1, wl.Count);

			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
			AddObject(wl);
			Assert.AreEqual(1, wl.Count);

			var o = new object();
			wl.Add(o);
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
			Assert.AreEqual(2, wl.Count);

			int iC = 0;
			foreach (var item in wl) iC++;
			Assert.AreEqual(1, iC);
			Assert.AreEqual(2, wl.Count);

			GC.KeepAlive(o);
		}
		void AddObject(WeakList<object> wl) { wl.Add(new Object()); }

		[TestMethod]
		public void TestWeakSet()
		{
			var ws = new WeakSet<object>();

			var o = new Object();
			ws.Add(o);
			ws.Add(o);
			Assert.AreEqual(1, ws.Count);
			foreach (var item in ws) Assert.AreEqual(item, o);

			ws.Remove(o);
			Assert.AreEqual(0, ws.Count);

			AddObject(ws);
			AddObject(ws);
			Assert.AreEqual(2, ws.Count);
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

			int iC = 0;
			foreach (var item in ws) iC++;
			Assert.AreEqual(0, iC);
			Assert.AreEqual(2, ws.Count);

			ws.WeakCleanup();
			Assert.AreEqual(0, ws.Count);
		}
		void AddObject(WeakSet<object> ws) { ws.Add(new Object()); }

		[TestMethod]
		public void TestForEach()
		{
			// make sure it enumerates
			var l = new WeakList<string> { "hello" };
			var count = 0;
			l.ForEach(x => count++);
			Assert.AreEqual(1, count);
		}
	}
}
