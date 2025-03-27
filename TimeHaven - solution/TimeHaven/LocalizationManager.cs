using System.Globalization;
using System.Windows;
using TimeHaven;

public static class LocalizationManager
{
    private static CultureInfo currentCulture = new CultureInfo("en-US");
    public static event Action ?LanguageChanged;
    public static CultureInfo CurrentCulture => currentCulture;

    public static void SetLanguage(string langCode)
    {
        currentCulture = new CultureInfo(langCode);
        Thread.CurrentThread.CurrentUICulture = currentCulture;
        Thread.CurrentThread.CurrentCulture = currentCulture;

        UpdateResourceDictionary(langCode);

        SettingsManager.UpdateSetting(langCode, val => SettingsManager.Settings.Language = val);

        LanguageChanged?.Invoke();
    }

    private static void UpdateResourceDictionary(string langCode)
    {
        string langFile = $"Resources/Lang.{langCode}.xaml";
        var dict = new ResourceDictionary { Source = new Uri(langFile, UriKind.Relative) };

        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(dict);
    }

    public static string GetTimeSuffix()
    {
        return CurrentCulture.Name == "ru-RU" ? " мин" : " min";
    }

}
