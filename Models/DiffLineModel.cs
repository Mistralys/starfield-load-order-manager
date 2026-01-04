using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.Models
{
    public enum DiffChangeType
    {
        Unchanged,
        Added,
        Removed,
        Moved,
        Replaced,
        Inserted
    }

    public sealed partial class DiffLineModel : ObservableObject
    {
        public DiffLineModel(string fileName, string text, DiffChangeType changeType, int? referenceNumber = null, int? currentNumber = null, string? replacementFileName = null)
        {
            FileName = fileName;
            Text = text;
            ChangeType = changeType;
            ReferenceNumber = referenceNumber;
            CurrentNumber = currentNumber;
            ReplacementFileName = replacementFileName;
            DependentChanges = new List<DiffLineModel>();
        }

        public string FileName { get; }

        public string Text { get; }

        public DiffChangeType ChangeType { get; }

        public int? ReferenceNumber { get; }

        public int? CurrentNumber { get; }

        public string? ReplacementFileName { get; }

        public List<DiffLineModel> DependentChanges { get; }

        public bool HasDependentChanges => DependentChanges.Count > 0;

        [ObservableProperty]
        private bool _isDependentChangesExpanded;

        public string Prefix => ChangeType switch
        {
            DiffChangeType.Added => "+",
            DiffChangeType.Removed => "-",
            DiffChangeType.Moved => "~",
            DiffChangeType.Replaced => ">",
            DiffChangeType.Inserted => "^",
            _ => string.Empty
        };
    }
}
