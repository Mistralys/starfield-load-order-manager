namespace LoadOrderKeeper.Models
{
    public enum DiffChangeType
    {
        Unchanged,
        Added,
        Removed,
        Moved
    }

    public sealed class DiffLineModel
    {
        public DiffLineModel(string fileName, string text, DiffChangeType changeType, int? referenceNumber = null, int? currentNumber = null)
        {
            FileName = fileName;
            Text = text;
            ChangeType = changeType;
            ReferenceNumber = referenceNumber;
            CurrentNumber = currentNumber;
        }

        public string FileName { get; }

        public string Text { get; }

        public DiffChangeType ChangeType { get; }

        public int? ReferenceNumber { get; }

        public int? CurrentNumber { get; }

        public string Prefix => ChangeType switch
        {
            DiffChangeType.Added => "+",
            DiffChangeType.Removed => "-",
            DiffChangeType.Moved => "~",
            _ => string.Empty
        };
    }
}
