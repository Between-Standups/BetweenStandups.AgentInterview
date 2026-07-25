namespace OrderDiscountRules;

public sealed class DiscountCalculator
{
    public decimal CalculateTotal(decimal subtotal, bool isLoyalCustomer)
    {
        if (subtotal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(subtotal));
        }

        var discount = subtotal switch
        {
            >= 500 => 0.15m,
            >= 100 => 0.10m,
            _ => 0m
        };

        if (isLoyalCustomer)
        {
            discount += 0.05m;
        }

        return decimal.Round(subtotal * (1 - discount), 2, MidpointRounding.AwayFromZero);
    }
}
