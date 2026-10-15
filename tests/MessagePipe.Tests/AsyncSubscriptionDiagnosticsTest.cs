using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace MessagePipe.Tests
{
    public class AsyncSubscriptionDiagnosticsTest
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task DisposeSubscriptionAfterScope(bool buffered, bool captureStackTrace)
        {
            var services = new ServiceCollection();
            services.AddMessagePipe(options =>
            {
                options.InstanceLifetime = InstanceLifetime.Scoped;
                options.EnableCaptureStackTrace = captureStackTrace;
            });
            using var provider = services.BuildServiceProvider();
            var diagnostics = provider.GetRequiredService<MessagePipeDiagnosticsInfo>();
            var scope = provider.CreateScope();
            var subscription = buffered
                ? await scope.ServiceProvider.GetRequiredService<IBufferedAsyncSubscriber<int>>()
                    .SubscribeAsync((_, _) => default)
                : scope.ServiceProvider.GetRequiredService<IAsyncSubscriber<int>>()
                    .Subscribe((_, _) => default);

            diagnostics.SubscribeCount.Should().Be(1);
            scope.Dispose();
            diagnostics.SubscribeCount.Should().Be(0);
            diagnostics.GetCapturedStackTraces().Should().BeEmpty();

            subscription.Dispose();
            diagnostics.SubscribeCount.Should().Be(0);
            subscription.Dispose();
            diagnostics.SubscribeCount.Should().Be(0);
        }
    }
}
