using System.Collections.ObjectModel;

namespace ATI.Services.Common.Localization;

/// <summary>
/// Localization source for InCodeLocalizer.
/// </summary>
/// <remarks>Implement in each supported locale</remarks>
public interface IInCodeLocalization
{
    ReadOnlyDictionary<string, string> LocalizedStrings { get; }
    string Locale { get; }
}