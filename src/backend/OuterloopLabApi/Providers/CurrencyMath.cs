namespace OuterloopLabApi;

public static class CurrencyMath
{
    public static decimal RoundTwo(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
