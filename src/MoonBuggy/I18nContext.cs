namespace MoonBuggy;

/// <summary>
/// Holds i18n state for the current async context.
/// Stored in AsyncLocal via I18n.Current.
/// </summary>
public class I18nContext
{
    /// <summary>
    /// LCID for the current locale.
    /// 0 = source locale (fallback/default).
    /// </summary>
    public int LCID { get; set; }
}
