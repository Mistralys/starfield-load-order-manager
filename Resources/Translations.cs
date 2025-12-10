using System.Globalization;

namespace StarfieldLoadOrderManager.Resources
{
    public class Translations
    {
        public string this[string key] => Strings.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? string.Empty;
    }
}
