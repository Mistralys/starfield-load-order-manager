using System;

namespace LoadOrderKeeper.Models
{
    public sealed class ModEntryModel : IEquatable<ModEntryModel>
    {
        public string FileName { get; }
        public bool IsEnabled { get; }

        public ModEntryModel(string line)
        {
            var trimmed = line?.Trim() ?? string.Empty;
            IsEnabled = trimmed.StartsWith("*", StringComparison.Ordinal);
            FileName = trimmed.TrimStart('*').Trim();
        }

        public string ToLine() => $"{(IsEnabled ? "*" : string.Empty)}{FileName}";

        public override string ToString() => ToLine();

        public bool Equals(ModEntryModel? other)
        {
            if (other is null) return false;
            return string.Equals(FileName, other.FileName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => Equals(obj as ModEntryModel);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(FileName);
    }
}
