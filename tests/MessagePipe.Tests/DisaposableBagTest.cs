using FluentAssertions;
using System;
using System.Linq;
using Xunit;

namespace MessagePipe.Tests
{
    public class DisaposableBagTest
    {
        [Fact]
        public void Static()
        {
            var d1 = new Disposable(1);
            var d2 = new Disposable(2);
            var d3 = new Disposable(3);
            var d4 = new Disposable(4);

            var bag = DisposableBag.Create(d1, d2, d3, d4);

            d1.DisposeCalled.Should().Be(0);
            d2.DisposeCalled.Should().Be(0);
            d3.DisposeCalled.Should().Be(0);
            d4.DisposeCalled.Should().Be(0);

            bag.Dispose();

            d1.DisposeCalled.Should().Be(1);
            d2.DisposeCalled.Should().Be(1);
            d3.DisposeCalled.Should().Be(1);
            d4.DisposeCalled.Should().Be(1);

            bag.Dispose();

            d1.DisposeCalled.Should().Be(1);
            d2.DisposeCalled.Should().Be(1);
            d3.DisposeCalled.Should().Be(1);
            d4.DisposeCalled.Should().Be(1);
        }

        [Fact]
        public void Nth()
        {
            var d = Enumerable.Range(1, 100).Select(x => new Disposable(x)).ToArray();

            var bag = DisposableBag.Create(d); // params IDisposable[]

            foreach (var item in d)
            {
                item.DisposeCalled.Should().Be(0);
            }

            bag.Dispose();

            foreach (var item in d)
            {
                item.DisposeCalled.Should().Be(1);
            }

            bag.Dispose();

            foreach (var item in d)
            {
                item.DisposeCalled.Should().Be(1);
            }
        }

        [Fact]
        public void Builder()
        {
            var builder = DisposableBag.CreateBuilder();
            builder.Build().Dispose(); // OK empty


            var d1 = new Disposable(1);
            var d2 = new Disposable(2);
            var d3 = new Disposable(3);
            var d4 = new Disposable(4);

            builder = DisposableBag.CreateBuilder();

            builder.Add(d1);
            builder.Add(d2);
            builder.Add(d3);
            builder.Add(d4);

            builder.Build().Dispose();

            d1.DisposeCalled.Should().Be(1);
            d2.DisposeCalled.Should().Be(1);
            d3.DisposeCalled.Should().Be(1);
            d4.DisposeCalled.Should().Be(1);

            var d = Enumerable.Range(1, 100).Select(x => new Disposable(x)).ToArray();

            builder = DisposableBag.CreateBuilder();
            foreach (var item in d)
            {
                builder.Add(item);
            }

            builder.Build().Dispose();
            foreach (var item in d)
            {
                item.DisposeCalled.Should().Be(1);
            }
        }

        public class Disposable : IDisposable
        {
            public int Number { get; }
            public int DisposeCalled { get; private set; }

            public Disposable(int number)
            {
                this.Number = number;
            }

            public void Dispose()
            {
                DisposeCalled++;
            }
        }
    }
}
