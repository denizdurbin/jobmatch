using JobMatch.Infrastructure.Ai;
using JobMatch.Infrastructure.Extraction;
using JobMatch.Tests.Fakes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JobMatch.Tests;

public class PostingExtractorTests
{
    private static readonly string Posting = File.ReadAllText("TestData/posting-dotnet-junior.txt");
    private static string Json(string name) => File.ReadAllText($"TestData/{name}.json");

    private static PostingExtractor CreateExtractor(FakeChatClient chat)
    {
        return new PostingExtractor(chat, new PromptLoader(), Options.Create(new GroqOptions()), NullLogger<PostingExtractor>.Instance);
    }

    [Fact]
    public async Task Valid_response_succeeds_first_try()
    {
        var chat = new FakeChatClient(Json("extraction-valid"));

        var attempt = await CreateExtractor(chat).ExtractAsync(Posting);

        attempt.Success.ShouldBeTrue();
        attempt.Result!.ContractType.ShouldBe("CDI");
        attempt.Result.Skills.Count.ShouldBe(4);
        attempt.PromptVersion.ShouldBe("extraction.v1");
        chat.Calls.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Sends_prompt_and_posting()
    {
        var chat = new FakeChatClient(Json("extraction-valid"));

        await CreateExtractor(chat).ExtractAsync(Posting);

        var messages = chat.Calls[0];
        messages[0].Role.ShouldBe(ChatRole.System);
        messages[0].Text.ShouldContain("Never guess");
        messages[1].Text.ShouldContain("Développeur .NET Junior");
    }

    [Fact]
    public async Task Skill_with_fake_evidence_is_dropped()
    {
        var chat = new FakeChatClient(Json("extraction-fake-evidence"));

        var attempt = await CreateExtractor(chat).ExtractAsync(Posting);

        attempt.Success.ShouldBeTrue();
        attempt.Result!.Skills.ShouldNotContain(s => s.Name == "Kubernetes");
    }

    [Fact]
    public async Task Invalid_then_valid_retries_with_errors()
    {
        var chat = new FakeChatClient(Json("extraction-bad-enum"), Json("extraction-valid"));

        var attempt = await CreateExtractor(chat).ExtractAsync(Posting);

        attempt.Success.ShouldBeTrue();
        chat.Calls.Count.ShouldBe(2);
        chat.Calls[1].Last().Text.ShouldContain("contract_type");
    }

    [Fact]
    public async Task Invalid_twice_fails()
    {
        var chat = new FakeChatClient(Json("extraction-bad-enum"), Json("extraction-bad-enum"));

        var attempt = await CreateExtractor(chat).ExtractAsync(Posting);

        attempt.Success.ShouldBeFalse();
        attempt.Error!.ShouldContain("contract_type");
    }

    [Fact]
    public async Task Not_json_counts_as_invalid()
    {
        var chat = new FakeChatClient("sorry I can't do that", Json("extraction-valid"));

        var attempt = await CreateExtractor(chat).ExtractAsync(Posting);

        attempt.Success.ShouldBeTrue();
        chat.Calls.Count.ShouldBe(2);
    }
}
