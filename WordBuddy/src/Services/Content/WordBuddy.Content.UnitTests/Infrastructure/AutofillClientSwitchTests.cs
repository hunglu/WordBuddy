using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Autofill;
using WordBuddy.Content.Infrastructure.Extensions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Infrastructure;

/// <summary><c>Autofill:UseFakeClients</c>: fakes only in Development with the flag; real clients otherwise.</summary>
public class AutofillClientSwitchTests
{
    private static ServiceProvider Build(string? flag, bool isDevelopment)
    {
        Dictionary<string, string?> values = [];
        if (flag is not null)
        {
            values["Autofill:UseFakeClients"] = flag;
        }

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddAutofillClients(configuration, isDevelopment);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddAutofillClients_DevelopmentWithFlag_RegistersFakes()
    {
        using ServiceProvider provider = Build("true", isDevelopment: true);

        provider.GetRequiredService<IDictionaryClient>().Should().BeOfType<DevelopmentFakeAutofillClients>();
        provider.GetRequiredService<ISenseGenerator>().Should().BeOfType<DevelopmentFakeAutofillClients>();
        provider.GetRequiredService<IAudioDownloader>().Should().BeOfType<DevelopmentFakeAutofillClients>();
    }

    [Theory]
    [InlineData("true", false)]
    [InlineData("false", true)]
    [InlineData(null, true)]
    [InlineData(null, false)]
    public void AddAutofillClients_OtherwiseRegistersRealClients(string? flag, bool isDevelopment)
    {
        using ServiceProvider provider = Build(flag, isDevelopment);

        provider.GetRequiredService<IDictionaryClient>().Should().BeOfType<FreeDictionaryClient>();
        provider.GetRequiredService<ISenseGenerator>().Should().BeOfType<ClaudeSenseGenerator>();
        provider.GetRequiredService<IAudioDownloader>().Should().BeOfType<HttpAudioDownloader>();
    }

    [Theory]
    [InlineData("serendipity")]
    [InlineData("Apple")]
    [InlineData("happy")]
    [InlineData("run")]
    public async Task DevelopmentFakeAutofillClients_KnownWord_ReturnsFixedEntryAndSenses(string word)
    {
        DevelopmentFakeAutofillClients fakes = new();

        Result<DictionaryEntry> entry = await fakes.LookupAsync(word);
        Result<GeneratedWord> generated = await fakes.GenerateAsync(new SenseGenerationRequest(word, entry.Value.PartsOfSpeech, ["vi"], []));

        entry.IsSuccess.Should().BeTrue();
        generated.Value.Lexemes.Should().NotBeEmpty();
        generated.Value.Lexemes.SelectMany(l => l.Senses).Should().OnlyContain(s => s.Translations.ContainsKey("vi"));
    }

    [Fact]
    public async Task DevelopmentFakeAutofillClients_UnknownWord_ReturnsNotFound()
    {
        Result<DictionaryEntry> entry = await new DevelopmentFakeAutofillClients().LookupAsync("zzqxautofill");

        entry.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task DevelopmentFakeAutofillClients_Run_HasVerbAndNoun()
    {
        Result<GeneratedWord> generated = await new DevelopmentFakeAutofillClients()
            .GenerateAsync(new SenseGenerationRequest("run", [], ["vi"], []));

        generated.Value.Lexemes.Select(l => l.PartOfSpeech).Should().Equal(PartOfSpeech.Verb, PartOfSpeech.Noun);
    }
}
