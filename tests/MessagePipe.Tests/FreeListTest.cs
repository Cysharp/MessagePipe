using FluentAssertions;
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
            //var list = new FreeList<int, string>();

            //list.Add(100, "a");
            //list.Add(200, "b");
            //list.Add(300, "c");
            //list.Add(400, "d");
            //list.Add(500, "e");

            //list.Count.Should().Be(5);

            //var items = list.GetUnsafeRawItems();
            //items.Count().Should().Be(8); // inital count

            //items.Where(x => x != null).Should().BeEquivalentTo(new[] { "a", "b", "c", "d", "e" });

            //list.Remove(300);
            //list.Remove(500);

            //list.Count.Should().Be(3);

            //items = list.GetUnsafeRawItems();
            //items.Where(x => x != null).Should().BeEquivalentTo(new[] { "a", "b", "d" });

            //list.Add(600, "f");
            //list.Add(700, "g");
            //list.Add(800, "h");
            //list.Add(900, "i");
            //list.Add(999, "j");

            //list.Count.Should().Be(list.GetUnsafeRawItems().Length);

            //// grow

            //list.Add(1000, "a2");
            //list.Count.Should().Be(9);
            //list.GetUnsafeRawItems().Length.Should().Be(16);
            //list.GetUnsafeRawItems().Where(x => x != null).Should().BeEquivalentTo(new[] { "a", "b", "d", "f", "g", "h", "i", "j", "a2" });

            //list.Add(1001, "b2");
            //list.Add(1002, "c2");
            //list.GetUnsafeRawItems().Where(x => x != null).Should().BeEquivalentTo(new[] { "a", "b", "d", "f", "g", "h", "i", "j", "a2", "b2", "c2" });

            //list.Remove(600);
            //list.Remove(800);
            //list.Remove(900);
            //list.Remove(1002);

            //list.GetUnsafeRawItems().Where(x => x != null).Should().BeEquivalentTo(new[] { "a", "b", "d", "g", "j", "a2", "b2" }); // 7

            //foreach (var i in Enumerable.Range(0, 30)) list.Add(i, i.ToString()); // +30 = 37

            //list.GetUnsafeRawItems().Where(x => x != null).Should().BeEquivalentTo(new[] { "a", "b", "d", "g", "j", "a2", "b2" }.Concat(Enumerable.Range(0, 30).Select(x => x.ToString())));


            //list.Remove(200); // 36

            //foreach (var i in Enumerable.Range(0, 24)) list.Remove(i);

            //list.GetUnsafeRawItems().Where(x => x != null).Should().BeEquivalentTo(new[] { "a", "d", "g", "j", "a2", "b2" }.Concat(Enumerable.Range(24, 6).Select(x => x.ToString())));
        }


        [Fact]
        public void TrimExcess()
        {
            //var freeList = new FreeList<int, string>();

            //// initial 8
            //foreach (var i in Enumerable.Range(10, 8)) freeList.Add(i, i.ToString());

            //// next 16
            //foreach (var i in Enumerable.Range(100, 8)) freeList.Add(i, i.ToString());

            //// morenext 32
            //foreach (var i in Enumerable.Range(1000, 16)) freeList.Add(i, i.ToString());

            //freeList.GetUnsafeRawItems().Length.Should().Be(32);

            //// first shrink = 8
            //foreach (var i in Enumerable.Range(1000, 16)) freeList.Remove(i);
            //foreach (var i in Enumerable.Range(100, 8)) freeList.Remove(i);

            //freeList.GetUnsafeRawItems().Length.Should().Be(16);
            //freeList.GetUnsafeRawItems().Where(x => x != null).Count().Should().Be(8);
        }
    }
}
