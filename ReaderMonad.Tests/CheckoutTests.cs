using System.Globalization;
using ReaderMonad;

namespace ReaderMonad.Tests;

public sealed class CheckoutTests
{
    private sealed class CapturingLogger : ILogger
    {
        public List<string> Messages { get; } = new();
        public void Log(string msg) => Messages.Add(msg);
    }

    private static PricingEnv EnvAt(
        DateTime now,
        bool isVip = false,
        string cultureCode = "en-US",
        ILogger? logger = null,
        string correlationId = "test-corr"
    )
        => new(
            Now: now,
            IsVip: isVip,
            CultureCode: cultureCode,
            Logger: logger ?? new CapturingLogger(),
            CorrelationId: correlationId
        );

    [Theory]
    [InlineData(16, 59, false)] // before flash sale window
    [InlineData(17, 00, true)]  // start inclusive
    [InlineData(18, 59, true)]  // within
    [InlineData(19, 00, false)] // end exclusive
    public void CalculateItemPrice_applies_flash_sale_discount_only_between_17_and_19_utc(int hour, int minute, bool shouldDiscount)
    {
        var logger = new CapturingLogger();
        var env = EnvAt(new DateTime(2025, 12, 19, hour, minute, 0, DateTimeKind.Utc), isVip: false, logger: logger);
        var item = new CartItem(BasePrice: 100m, Name: "Widget");

        var price = Program.CalculateItemPrice(item).Run(env);

        Assert.Equal(shouldDiscount ? 90m : 100m, price);
        Assert.Single(logger.Messages); // one item computed => one log line
        Assert.Contains("Item=Widget", logger.Messages[0]);
        Assert.Contains("Request=test-corr", logger.Messages[0]);
    }

    [Fact]
    public void CalculateItemPrice_applies_vip_discount_outside_flash_sale()
    {
        var env = EnvAt(new DateTime(2025, 12, 19, 12, 0, 0, DateTimeKind.Utc), isVip: true);
        var item = new CartItem(BasePrice: 80m, Name: "Apple");

        var price = Program.CalculateItemPrice(item).Run(env);

        Assert.Equal(72m, price);
    }

    [Fact]
    public void CalculateCartTotal_sums_item_prices_under_environment_and_logs_once_per_item()
    {
        var logger = new CapturingLogger();
        var env = EnvAt(new DateTime(2025, 12, 19, 12, 0, 0, DateTimeKind.Utc), isVip: true, logger: logger);

        var cart = new Cart(
            new[]
            {
                new CartItem(BasePrice: 10m, Name: "A"),
                new CartItem(BasePrice: 5m, Name: "B"),
                new CartItem(BasePrice: 2m, Name: "C")
            }
        );

        var total = Program.CalculateCartTotal(cart).Run(env);

        Assert.Equal((10m + 5m + 2m) * 0.9m, total);
        Assert.Equal(3, logger.Messages.Count);
    }

    [Fact]
    public void FormatPrice_uses_environment_culture_code()
    {
        var envUs = EnvAt(new DateTime(2025, 12, 19, 12, 0, 0, DateTimeKind.Utc), cultureCode: "en-US");
        var envFr = EnvAt(new DateTime(2025, 12, 19, 12, 0, 0, DateTimeKind.Utc), cultureCode: "fr-FR");

        var us = Program.FormatPrice(12.34m).Run(envUs);
        var fr = Program.FormatPrice(12.34m).Run(envFr);

        Assert.Contains("$", us);
        Assert.Contains("€", fr);
    }

    [Fact]
    public void GenerateCheckoutSummary_formats_final_amount_from_cart_total()
    {
        var env = EnvAt(new DateTime(2025, 12, 19, 12, 0, 0, DateTimeKind.Utc), isVip: false, cultureCode: "en-US");
        var cart = new Cart(new[] { new CartItem(10m, "A"), new CartItem(1.50m, "B") });

        var summary = Program.GenerateCheckoutSummary(cart).Run(env);

        Assert.Equal("Final Amount: $11.50", summary);
    }

    [Fact]
    public void GenerateUpsellMessage_when_not_vip_and_not_flash_sale_suggests_upgrade_with_savings()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            var env = EnvAt(new DateTime(2025, 12, 19, 12, 0, 0, DateTimeKind.Utc), isVip: false);
            var cart = new Cart(new[] { new CartItem(100m, "A") });

            var msg = Program.GenerateUpsellMessage(cart).Run(env);

            Assert.Contains("Upgrade to VIP to save", msg);
            Assert.Contains("$10.00", msg);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void GenerateUpsellMessage_when_flash_sale_already_applies_returns_best_price_message()
    {
        var env = EnvAt(new DateTime(2025, 12, 19, 17, 30, 0, DateTimeKind.Utc), isVip: false);
        var cart = new Cart(new[] { new CartItem(100m, "A") });

        var msg = Program.GenerateUpsellMessage(cart).Run(env);

        Assert.Equal("You are getting the best price!", msg);
    }

    [Fact]
    public void Interface_based_helpers_work_with_AppEnv()
    {
        var env = new AppEnv(VatRate: 0.2m, CurrentUserRole: "VIP", Currency: "USD");

        var tax = Program.CalculateTax(50m).Run(env);
        var isVip = Program.IsVip().Run(env);
        var formatted = Program.FormatMoney(12.3m).Run(env);

        Assert.Equal(10m, tax);
        Assert.True(isVip);
        Assert.Equal("USD 12.30", formatted);
    }
}


