using Microsoft.Extensions.AI;

namespace JobMatch.Tests.Fakes;

// returns canned answers in order instead of calling a real LLM
public class FakeChatClient(params string[] responses) : IChatClient
{
    private readonly Queue<string> _responses = new(responses);

    public List<List<ChatMessage>> Calls { get; } = new();

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        Calls.Add(messages.ToList());
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("FakeChatClient has no more responses");
        }
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _responses.Dequeue())));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
