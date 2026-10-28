#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

using MessagePipe.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePipe.Tests
{
    public class ArrayUtilTest
    {
        [Fact]
        public void Add()
        {
            var xs = new int[0];
            for (int i = 0; i < 10; i++)
            {
                var ys = ArrayUtil.ImmutableAdd(xs, 10 * i);

                Assert.Equal(i, xs.Length);
                Assert.Equal(i + 1, ys.Length);
                Assert.Equal(xs.Length + 1, ys.Length);
                for (int j = 0; j < xs.Length; j++)
                {
                    Assert.Equal(10 * j, xs[j]);
                }
                for (int j = 0; j < xs.Length; j++)
                {
                    Assert.Equal(10 * j, ys[j]);
                }

                xs = ys;
            }
        }

        [Fact]
        public void Remove()
        {
            // empty, no match
            {
                var xs = new int[0] { };
                var ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == 999, (object)null);

                Assert.Empty(ys);
            }
            // one
            {
                var xs = new[] { 10 };
                var ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == 999, (object)null);

                Assert.Single(ys);

                ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == 10, (object)null);
                Assert.Empty(ys);
            }

            // two, fisrt remove
            {
                var xs = new[] { 10, 20 };
                var ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == 10, (object)null);
                Assert.Equal(new[] { 20 }, ys);
            }
            // two, last remove
            {
                var xs = new[] { 10, 20 };
                var ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == 20, (object)null);
                Assert.Equal(new[] { 10 }, ys);
            }

            // three, first remove
            {
                var xs = new[] { 10, 20, 30 };
                var ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == 10, (object)null);
                Assert.Equal(new[] { 20, 30 }, ys);
            }

            // three, middle remove
            {
                var xs = new[] { 10, 20, 30 };
                var ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == 20, (object)null);
                Assert.Equal(new[] { 10, 30 }, ys);
            }

            // three, last remove
            {
                var xs = new[] { 10, 20, 30 };
                var ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == 30, (object)null);
                Assert.Equal(new[] { 10, 20 }, ys);
            }

            // many patterns
            {
                var sequence = Enumerable.Range(1, 100).Select(x => Enumerable.Range(1, x).Select(y => y * 10).ToArray()).ToArray();

                foreach (var xs in sequence)
                {
                    for (int i = 1; i <= xs.Length; i++)
                    {
                        var ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == i * 10, (object)null);
                        Assert.Equal(Enumerable.Range(1, xs.Length).Select(x => x * 10).Where(x => x != i * 10), ys);
                    }
                }
            }
        }
    }
}
