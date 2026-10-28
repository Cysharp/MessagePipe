using MessagePipe.Internal;
using System.Linq;
using Xunit;

namespace MessagePipe.Tests
{
    public class FreeListTest
    {
        [Fact]
        public void AddRemoveTrim()
        {
            var list = new FreeList<string>();

            var a = list.Add("a");
            var b = list.Add("b");
            var c = list.Add("c");
            var d = list.Add("d");
            var e = list.Add("e");

            Assert.Equal(5, list.GetCount());

            var items = list.GetValues();
            Assert.Equal(8, items.Length);

            Assert.Equivalent(new[] { "a", "b", "c", "d", "e" }, items.Where(x => x != null), strict: true);

            list.Remove(c, true);
            list.Remove(e, true);

            Assert.Equal(3, list.GetCount());

            Assert.Equivalent(new[] { "a", "b", "d" }, list.GetValues().Where(x => x != null), strict: true);

            foreach (var i in Enumerable.Range(0, 30)) list.Add(i.ToString()); // 30
            list.Remove(a, true);
            list.Remove(b, true);
            list.Remove(d, true);

            list.Add("30");

            Assert.Equivalent(Enumerable.Range(0, 31).Select(x => x.ToString()), list.GetValues().Where(x => x != null), strict: true);


        }


        [Fact]
        public void ExpandAndShrink()
        {
            var freeList = new FreeList<string>();
            Assert.Equal(4, freeList.GetValues().Length);

            var a = freeList.Add("a");
            var b = freeList.Add("b");
            var c = freeList.Add("c");
            var d = freeList.Add("d");

            Assert.Equal(4, freeList.GetValues().Length);

            var e = freeList.Add("e");
            var f = freeList.Add("f");
            var g = freeList.Add("g");
            var h = freeList.Add("h");

            Assert.Equal(8, freeList.GetValues().Length);

            foreach (var subscription in new[] { a, b, c, d, e, f, g, h }) { freeList.Remove(subscription, true); }
            Assert.Equal(0, freeList.GetCount());
            Assert.Equal(8, freeList.GetValues().Length);

            var subscriptions = Enumerable.Range(100, 9).Select(x => freeList.Add(x.ToString())).ToArray();
            Assert.Equal(9, freeList.GetCount());
            Assert.Equal(16, freeList.GetValues().Length);

            foreach (var subscription in subscriptions) { freeList.Remove(subscription, true); }
            Assert.Equal(0, freeList.GetCount());
            Assert.Equal(4, freeList.GetValues().Length);
        }
    }
}
