namespace ReaderMonad;

// ---------- Domain: small examples of environment composition ----------

public interface IHasTaxInfo
{
    decimal VatRate { get; }
}

public interface IHasUser
{
    string CurrentUserRole { get; }
}

public interface IHasCurrency
{
    string Currency { get; }
}

public sealed record AppEnv(
    decimal VatRate,
    string CurrentUserRole,
    string Currency
) : IHasTaxInfo, IHasUser, IHasCurrency;


