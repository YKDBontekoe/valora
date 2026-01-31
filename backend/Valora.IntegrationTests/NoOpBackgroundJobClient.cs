using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace Valora.IntegrationTests;

public class NoOpBackgroundJobClient : IBackgroundJobClient
{
    public bool ChangeState(string jobId, IState state, string expectedState) => true;
    public string Create(Job job, IState state) => Guid.NewGuid().ToString();
}
