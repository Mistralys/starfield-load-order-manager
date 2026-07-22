using System;
using LoadOrderKeeper.ViewTexts;

namespace LoadOrderKeeper.Tests.Fixtures;

/// <summary>
/// xUnit test fixture that forces the <see cref="LocalizationService"/> singleton into en-US
/// for the duration of a test class, then restores the original culture on disposal.
/// </summary>
/// <remarks>
/// <para>
/// <b>What it does:</b> Snapshots <c>LocalizationService.Instance.CurrentCulture</c> on
/// construction, calls <c>SetCulture("en-US")</c> to guarantee English strings throughout the
/// test class, and restores the original culture in <see cref="Dispose"/>.
/// </para>
/// <para>
/// <b>Required usage pattern:</b> Every test class that consumes this fixture must combine
/// <em>two</em> xUnit mechanisms:
/// <list type="number">
///   <item>
///     <description>
///       <c>IClassFixture&lt;EnglishLocaleFixture&gt;</c> on the class declaration — gives each
///       class its own fixture instance (constructed once per class, disposed after the last test
///       in that class runs).
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>[Collection(LocaleSequentialCollection.Name)]</c> attribute — places the class inside
///       a <see cref="LocaleSequentialCollection"/> that has <c>DisableParallelization = true</c>.
///       This prevents concurrent culture mutations on the singleton when multiple locale-sensitive
///       test classes run simultaneously.
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Why both are needed:</b> <c>IClassFixture&lt;T&gt;</c> controls the fixture lifetime but
/// does not prevent xUnit from running test <em>classes</em> in parallel. Without the
/// <c>[Collection]</c> attribute, two classes can each have their own fixture instance and call
/// <c>SetCulture</c> concurrently, corrupting the shared singleton state and producing
/// intermittent test failures.
/// </para>
/// <para>
/// <b>Constructor injection idiom:</b> Accept the fixture as a constructor parameter and discard
/// it with <c>_ = localeFixture;</c>. The fixture's work is performed entirely in its own
/// constructor and <see cref="Dispose"/> — no stored reference is needed.
/// </para>
/// <example>
/// <code>
/// [Collection(LocaleSequentialCollection.Name)]
/// public sealed class MyCoordinatorTests : IClassFixture&lt;EnglishLocaleFixture&gt;, IDisposable
/// {
///     public MyCoordinatorTests(EnglishLocaleFixture localeFixture)
///     {
///         _ = localeFixture; // Ensures en-US culture is active for the lifetime of this test class
///         // ... remaining constructor setup
///     }
/// }
/// </code>
/// </example>
/// </remarks>
public class EnglishLocaleFixture : IDisposable
{
    private readonly string _originalCulture;

    public EnglishLocaleFixture()
    {
        _originalCulture = LocalizationService.Instance.CurrentCulture;
        LocalizationService.Instance.SetCulture("en-US");
    }

    public void Dispose()
    {
        LocalizationService.Instance.SetCulture(_originalCulture);
    }
}
