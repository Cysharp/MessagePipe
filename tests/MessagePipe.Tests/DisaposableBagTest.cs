using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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

        [Fact]
        public void SingleAssignment()
        {
            {
                var si = DisposableBag.CreateSingleAssignment();
                var d = new Disposable(100);
                si.Disposable = d;
                d.DisposeCalled.Should().Be(0);
                si.Dispose();
                d.DisposeCalled.Should().Be(1);
            }
            {
                var si = DisposableBag.CreateSingleAssignment();
                var d = new Disposable(100);
                si.Disposable = d;
                Assert.Throws<InvalidOperationException>(() => si.Disposable = d);
            }
            {
                var si = DisposableBag.CreateSingleAssignment();
                si.Dispose();
                var d = new Disposable(100);
                si.Disposable = d;
                d.DisposeCalled.Should().Be(1);
            }

            {
                var bag = DisposableBag.CreateBuilder();
                var si = DisposableBag.CreateSingleAssignment();
                var d = new Disposable(100);
                d.SetTo(si).AddTo(bag);
                d.DisposeCalled.Should().Be(0);
                bag.Build().Dispose();
                d.DisposeCalled.Should().Be(1);
            }
        }

        [Fact]
        public void CallOnce()
        {
            var provider = TestHelper.BuildServiceProvider();

            var p = provider.GetRequiredService<IPublisher<int>>();
            var s = provider.GetRequiredService<ISubscriber<int>>();

            var d = DisposableBag.CreateSingleAssignment();

            var list = new List<int>();
            d.Disposable = s.Subscribe(x =>
            {
                list.Add(x);
                d.Dispose();
            });

            p.Publish(100);
            p.Publish(200);
            p.Publish(300);

            list.Should().Equal(100);
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
