#region

using System.Globalization;
using JagexAccountSwitcher.Languages;
using Jeek.Avalonia.Localization;

#endregion

namespace JagexAccountSwitcher.Converters;

public class ResXLocalizer : BaseLocalizer
{
    public override void Reload()
    {
        if (_languages.Count == 0)
        {
            _languages.Add("en");
            _languages.Add("es");
            _languages.Add("pt");
        }

        ValidateLanguage();

        Strings.Culture = new CultureInfo(_language);

        _hasLoaded = true;

        UpdateDisplayLanguages();
    }

    protected override void OnLanguageChanged()
    {
        Reload();
    }

    public override string Get(string key)
    {
        if (!_hasLoaded)
            Reload();

        var langString = Strings.ResourceManager.GetString(key, Strings.Culture);
        return langString != null
            ? langString.Replace("\\n", "\n")
            : $"{Language}:{key}";
    }
}