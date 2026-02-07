using System.Globalization;
using MoonBuggy;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();

var app = builder.Build();

// Locale middleware: reads "lang" query parameter or cookie
app.Use(async (context, next) =>
{
    var lang = context.Request.Query["lang"].FirstOrDefault();

    if (!string.IsNullOrEmpty(lang))
    {
        // Persist choice in a cookie
        context.Response.Cookies.Append("lang", lang, new CookieOptions
        {
            MaxAge = TimeSpan.FromDays(365),
            IsEssential = true
        });
    }
    else
    {
        lang = context.Request.Cookies["lang"];
    }

    if (!string.IsNullOrEmpty(lang))
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo(lang);
            I18n.Current = new I18nContext { LCID = culture.LCID };
        }
        catch (CultureNotFoundException)
        {
            // Unknown locale — fall through to source locale (LCID 0)
        }
    }

    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.Run();
