namespace LoadOrderKeeper.Models
{
    public enum DiffChangeType
    {
        Unchanged,
        Added,
        Removed,
        Moved,
        Replaced
    }

    public sealed class DiffLineModel
    {
        public DiffLineModel(string fileName, string text, DiffChangeType changeType, int? referenceNumber = null, int? currentNumber = null, string? replacementFileName = null)
        {
            FileName = fileName;
            Text = text;
            ChangeType = changeType;
            ReferenceNumber = referenceNumber;
            CurrentNumber = currentNumber;
            ReplacementFileName = replacementFileName;
        }

        public string FileName { get; }

        public string Text { get; }

        public DiffChangeType ChangeType { get; }

        public int? ReferenceNumber { get; }

        public int? CurrentNumber { get; }

        public string? ReplacementFileName { get; }

        public string Prefix => ChangeType switch
        {
            DiffChangeType.Added => "+",
            DiffChangeType.Removed => "-",
            DiffChangeType.Moved => "~",
            DiffChangeType.Replaced => ">",
            _ => string.Empty
        };
    }
}
