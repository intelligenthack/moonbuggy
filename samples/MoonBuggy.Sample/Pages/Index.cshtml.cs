using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoonBuggy.Sample.Pages;

public class IndexModel : PageModel
{
    public string UserName => "Alice";
    public int MessageCount => 5;
    public int CartItems => 1;
}
