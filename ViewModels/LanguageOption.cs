namespace LoadOrderKeeper.ViewModels
{
    /// <summary>
    /// Represents a language option for the settings dropdown.
    /// </summary>
    public sealed class LanguageOption
    {
        public string Code { get; }
        public string DisplayName { get; }

        public LanguageOption(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }
    }
}
