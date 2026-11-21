using Xunit;

// All tests share one temp config.json (see TestEnvironment), because SettingsService is static
// and persists to a single file. Test classes that reach it, directly or via ProfileService,
// would race on that file when xUnit runs collections in parallel, which fails with an
// IOException. The suite is fast, so running it sequentially costs almost nothing.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
