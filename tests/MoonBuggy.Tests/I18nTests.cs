namespace MoonBuggy.Tests;

public class I18nTests
{
    [Fact]
    public void Current_DefaultLcid_IsZero()
    {
        // Source locale = LCID 0
        var ctx = I18n.Current;
        Assert.Equal(0, ctx.LCID);
    }

    [Fact]
    public void Current_SetAndGet_RoundTrips()
    {
        var ctx = new I18nContext { LCID = 42 };
        I18n.Current = ctx;

        Assert.Same(ctx, I18n.Current);
        Assert.Equal(42, I18n.Current.LCID);

        // Reset for other tests
        I18n.Current = new I18nContext();
    }

    [Fact]
    public void Current_LazyInit_ReturnsSameInstance()
    {
        // Reading twice without setting should return the same lazily-created instance
        var a = I18n.Current;
        var b = I18n.Current;
        Assert.Same(a, b);
    }

    [Fact]
    public async Task Current_AsyncContextIsolation_IndependentLcids()
    {
        // Two concurrent async contexts should maintain independent LCID values
        var barrier = new TaskCompletionSource<bool>();
        int lcid1 = -1, lcid2 = -1;

        var task1 = Task.Run(async () =>
        {
            I18n.Current = new I18nContext { LCID = 100 };
            await barrier.Task; // wait for task2 to set its value
            lcid1 = I18n.Current.LCID;
        });

        var task2 = Task.Run(() =>
        {
            I18n.Current = new I18nContext { LCID = 200 };
            barrier.SetResult(true); // signal task1 to read
            lcid2 = I18n.Current.LCID;
        });

        await Task.WhenAll(task1, task2);

        Assert.Equal(100, lcid1);
        Assert.Equal(200, lcid2);
    }
}
