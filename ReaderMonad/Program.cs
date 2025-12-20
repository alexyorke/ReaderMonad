namespace ReaderMonad;

// ---------- Demo program: examples of Reader usage ----------

internal class Program
{
    static void Main(string[] args)
    {
        var cartItem = new CartItem(2.99m, "Apple");
        var cart = new Cart([cartItem]);

        Console.WriteLine(HandleCheckout(cart));
    }

    internal static string HandleCheckout(Cart cart)
    {
        // In a real app these come from the outside world:
        DateTime now = DateTime.UtcNow;
        bool isVip = false; // e.g., from the authenticated user
        string cultureCode = "en-US"; // e.g., from headers / user settings
        ILogger logger = new ConsoleLogger();
        string correlationId = Guid.NewGuid().ToString("N");

        var env = new PricingEnv(
            Now: now,
            IsVip: isVip,
            CultureCode: cultureCode,
            Logger: logger,
            CorrelationId: correlationId
        );

        // Build the computation (still just a value)
        Reader<PricingEnv, string> pipeline = GenerateCheckoutSummary(cart);

        // Run it once to get a plain result
        string summary = pipeline.Run(env);

        return summary;
    }

    internal static Reader<PricingEnv, string> GenerateCheckoutSummary(Cart cart) =>
        from total in CalculateCartTotal(cart)
        from text in FormatPrice(total)
        select $"Final Amount: {text}";

    internal static Reader<PricingEnv, string> GenerateUpsellMessage(Cart cart) =>
        from currentTotal in CalculateCartTotal(cart)
        // Run the *same* calculation, but with a modified environment (VIP = true)
        from potentialTotal in CalculateCartTotal(cart).Local(env => env with { IsVip = true })
        select currentTotal == potentialTotal
            ? "You are getting the best price!"
            : $"Upgrade to VIP to save {(currentTotal - potentialTotal):C}!";

    internal static Reader<PricingEnv, decimal> CalculateCartTotal(Cart cart)
    {
        // Start with a Reader that always produces 0 (ignores env for now)
        Reader<PricingEnv, decimal> acc = Reader<PricingEnv, decimal>.Pure(0m);

        foreach (var item in cart.Items)
        {
            CartItem currentItem = item;
            acc = acc.Bind(
                totalSoFar =>
                {
                    return CalculateItemPrice(currentItem).Map(
                        price =>
                        {
                            return totalSoFar + price;
                        }
                    );
                }
            );
        }

        return acc;
    }

    internal static Reader<PricingEnv, decimal> CalculateItemPrice(CartItem item)
    {
        return Reader.From<PricingEnv, decimal>(env =>
        {
            const decimal VipOrFlashSaleDiscountRate = 0.10m;
            const decimal NoDiscountRate = 0.00m;
            const decimal FullPriceMultiplier = 1.00m;

            bool isFlashSale = env.Now.Hour >= 17 && env.Now.Hour < 19;

            decimal discountRate = (env.IsVip || isFlashSale)
                ? VipOrFlashSaleDiscountRate
                : NoDiscountRate;

            decimal finalPrice = item.BasePrice * (FullPriceMultiplier - discountRate);

            env.Logger.Log(
                $"Item={item.Name} Base={item.BasePrice} DiscountRate={discountRate:P0} Final={finalPrice} Request={env.CorrelationId}"
            );

            return finalPrice;
        });
    }

    internal static Reader<PricingEnv, string> FormatPrice(decimal amount) =>
        Reader.From<PricingEnv, string>(env =>
            amount.ToString("C", new System.Globalization.CultureInfo(env.CultureCode))
        );

    internal static Reader<IHasTaxInfo, decimal> CalculateTax(decimal amount) =>
        Reader.From<IHasTaxInfo, decimal>(ctx => amount * ctx.VatRate);

    internal static Reader<IHasUser, bool> IsVip() =>
        Reader.From<IHasUser, bool>(ctx => ctx.CurrentUserRole == "VIP");

    internal static Reader<IHasCurrency, string> FormatMoney(decimal amount) =>
        Reader.From<IHasCurrency, string>(ctx => $"{ctx.Currency} {amount:F2}");
}
