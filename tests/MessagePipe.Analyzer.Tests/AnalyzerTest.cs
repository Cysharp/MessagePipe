using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace MessagePipe.Analyzer.Tests
{
    public class AnalyzerTest
    {
        static async Task VerifyAsync(string testCode, int startLine, int startColumn, int endLine, int endColumn)
        {

            await new CSharpAnalyzerTest<MessagePipeAnalyzer, XUnitVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Default.WithPackages(ImmutableArray.Create(new PackageIdentity("MessagePipe", "1.4.0"))),
                ExpectedDiagnostics = { new DiagnosticResult("MPA001", DiagnosticSeverity.Error).WithSpan(startLine, startColumn, endLine, endColumn) },
                TestCode = testCode
            }.RunAsync();
        }

        static async Task VerifyNoErrorAsync(string testCode)
        {

            await new CSharpAnalyzerTest<MessagePipeAnalyzer, XUnitVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Default.WithPackages(ImmutableArray.Create(new PackageIdentity("MessagePipe", "1.4.0"))),
                ExpectedDiagnostics = { },
                TestCode = testCode
            }.RunAsync();
        }

        [Fact]
        public async Task SimpleTest()
        {
            var testCode = @"using MessagePipe;

class C
{
    public void M(ISubscriber<int> subscriber)
    {
        subscriber.Subscribe(x => { });
    }
}";

            await VerifyAsync(testCode, 7, 9, 7, 39);
        }

        [Fact]
        public async Task NoErrorReport()
        {
            var testCode = @"using MessagePipe;

class C
{
    public void M(ISubscriber<int> subscriber)
    {
        var d = subscriber.Subscribe(x => { });
    }
}";

            await VerifyNoErrorAsync(testCode);
        }
    }

}
