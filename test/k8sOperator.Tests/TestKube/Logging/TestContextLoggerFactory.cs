namespace k8sOperator.Tests.TestKube.Logging;

public sealed class TestContextLoggerFactory(TestContext testContext)
        : ILoggerFactory
{
    private readonly TestContext _testContext = testContext ?? throw new ArgumentNullException(nameof(testContext));

    public void Dispose()
    {
    }
    public ILogger CreateLogger(string categoryName)
        => new TestContextLogger(_testContext, categoryName, LogLevel.Debug);

    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotImplementedException();
    }
}
