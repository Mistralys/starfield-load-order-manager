namespace LoadOrderKeeper.Models
{
    public sealed class ModDiffModel
    {
        public string FileName { get; init; } = string.Empty;
        public int? ReferenceNumber { get; init; }
        public int? CurrentNumber { get; init; }

        public bool IsNew => ReferenceNumber is null && CurrentNumber is not null;
        public bool IsRemoved => ReferenceNumber is not null && CurrentNumber is null;
        public bool IsMoved => ReferenceNumber is not null && CurrentNumber is not null && ReferenceNumber != CurrentNumber;
    }
}
