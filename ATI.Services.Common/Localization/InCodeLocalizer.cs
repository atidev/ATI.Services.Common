using System;
using System.Collections.Generic;
using System.Linq;
using ATI.Services.Common.Variables;
using JetBrains.Annotations;
using NLog;

namespace ATI.Services.Common.Localization;

/// <summary>
/// Simple localizer, allows to store translations in hardcoded ILocalization classes.
/// Useful when count of literals to translate is rather small and they inlined in code.
/// </summary>
/// <remarks>Add at startup <c>app.UseAcceptLanguageLocalization()</c> and receive using DI</remarks>
[PublicAPI]
public class InCodeLocalizer
{
    private readonly Dictionary<string, IInCodeLocalization> _localizations;
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public InCodeLocalizer(IEnumerable<IInCodeLocalization> localizations)
    {
        _localizations = localizations.Where(x => ServiceVariables.SupportedLocales.Contains(x.Locale))
                                      .ToDictionary(x => x.Locale);
    }

    /// <summary>
    /// Returns localized string in current locale. Requires usage of <c>app.UseAcceptLanguageLocalization()</c>.
    /// <example>
    /// <code>
    ///      _inCodeLocalizer["StringId", false] //requires to provide default localization
    ///     or
    ///     _inCodeLocalizer["String itself in default locale"]
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="name">Could be key of the string to localize.
    /// Or default locale string itself, if <c>nameAsKey is true</c>.</param>
    /// <param name="nameAsKey">Allows to omit default locale ILocalization file</param>
    /// <exception cref="ArgumentNullException">If name parameter is null</exception>
    public string this[string name, bool nameAsKey = true]
    {
        get
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            
            var locale = LocaleHelper.GetLocale();
            if (nameAsKey && locale == ServiceVariables.DefaultLocale)
            {
                return name;
            }

            if (_localizations.TryGetValue(locale, out var localization)
                && localization.LocalizedStrings.TryGetValue(name, out var localized))
            {
                return localized;
            }

            Logger.Error($"Missing translation for {name} in locale {locale}");
            return string.Empty;
        }
    }
}