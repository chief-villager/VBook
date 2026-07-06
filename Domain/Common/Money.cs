namespace Bookkeeping.Domain.Common;

// Value object. Persistence in this scaffold stores plain decimals with NGN assumed;
// Money is used at the service and domain boundary. See README for the tradeoff.
public readonly record struct Money(decimal Amount, string Currency = "NGN")
{
    public static Money Zero(string currency = "NGN") => new(0m, currency);
    public static Money Naira(decimal amount) => new(amount, "NGN");

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount + other.Amount };
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount - other.Amount };
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Currency mismatch: {Currency} vs {other.Currency}.");
    }

    public override string ToString() => $"{Currency} {Amount:N2}";
}
