using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.Models
{
    public enum DiffChangeType
    {
        Unchanged,
        Added,
        Removed
    }

    public sealed class DiffLineModel
    {
        public DiffLineModel(string text, DiffChangeType changeType)
        {
            Text = text;
            ChangeType = changeType;
        }

        public string Text { get; }

        public DiffChangeType ChangeType { get; }

        public string Prefix => ChangeType switch
        {
            DiffChangeType.Added => "+",
            DiffChangeType.Removed => "-",
            _ => ""
        };
    }
}
