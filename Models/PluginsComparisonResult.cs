namespace LoadOrderKeeper.Models
{
    public readonly record struct PluginsComparisonResult(bool HasDifferences, string PluginsSignature);
}
