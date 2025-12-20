namespace ReaderMonad;

// ---------- Domain: checkout/cart pricing ----------

public sealed record Cart(IEnumerable<CartItem> Items);

public sealed record CartItem(decimal BasePrice, string Name);

public interface IUserContext
{
    bool IsVip { get; }
}

public interface ILocalization
{
    string CultureCode { get; }
}

public interface ILogger
{
    void Log(string msg);
}

public interface IRequestContext
{
    string CorrelationId { get; }
}

public sealed record PricingEnv(
    DateTime Now,
    bool IsVip,
    string CultureCode,
    ILogger Logger,
    string CorrelationId
) : IUserContext, ILocalization, IRequestContext;

internal sealed class ConsoleLogger : ILogger
{
    public void Log(string msg)
    {
        Console.WriteLine(msg);
    }
}


