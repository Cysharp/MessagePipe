#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

using FluentAssertions;
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

                xs.Length.Should().Be(i);
                ys.Length.Should().Be(i + 1);
                ys.Length.Should().Be(xs.Length + 1);
                for (int j = 0; j < xs.Length; j++)
                {
                    xs[j].Should().Be(10 * j);
                }
                for (int j = 0; j < xs.Length; j++)
                {
                    ys[j].Should().Be(10 * j);
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

                ys.Length.Should().Be(0);
            }
            // one
            {
                var xs = new int[] { 10 };
                var ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == 999, (object)null);

                ys.Length.Should().Be(1);

                ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == 10, (object)null);
                ys.Length.Should().Be(0);
            }

            // two, fisrt remove
            {
                var xs = new[] { 10, 20 };
                var ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == 10, (object)null);
                ys.Should().Equal(new[] { 20 });
            }
            // two, last remove
            {
                var xs = new[] { 10, 20 };
                var ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == 20, (object)null);
                ys.Should().Equal(new[] { 10 });
            }

            // three, first remove
            {
                var xs = new[] { 10, 20, 30 };
                var ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == 10, (object)null);
                ys.Should().Equal(new[] { 20, 30 });
            }

            // three, middle remove
            {
                var xs = new[] { 10, 20, 30 };
                var ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == 20, (object)null);
                ys.Should().Equal(new[] { 10, 30 });
            }

            // three, last remove
            {
                var xs = new[] { 10, 20, 30 };
                var ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == 30, (object)null);
                ys.Should().Equal(new[] { 10, 20 });
            }

            // many patterns
            {
                var sequence = Enumerable.Range(1, 100).Select(x => Enumerable.Range(1, x).Select(y => y * 10).ToArray()).ToArray();

                foreach (var xs in sequence)
                {
                    for (int i = 1; i <= xs.Length; i++)
                    {
                        var ys = ArrayUtil.ImmutableRemove(xs, (x, _) => x == i * 10, (object)null);
                        ys.Should().Equal(Enumerable.Range(1, xs.Length).Select(x => x * 10).Where(x => x != i * 10));
                    }
                }
            }
        }
    }
}
