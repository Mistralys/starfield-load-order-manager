using Xunit;

namespace LoadOrderKeeper.Tests.Fixtures;

/// <summary>
/// xUnit collection definition that groups all locale-sensitive test classes into a
/// single non-parallel collection. Classes within a collection run sequentially,
/// preventing concurrent culture changes on the LocalizationService singleton from
/// interfering with each other. Each class in this collection uses its own
/// IClassFixture&lt;EnglishLocaleFixture&gt; instance for per-class culture setup/teardown.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class LocaleSequentialCollection
{
    public const string Name = "LocaleSequentialCollection";
}
