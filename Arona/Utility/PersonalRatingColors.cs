namespace Arona.Utility;

internal static class PersonalRatingColors
{
    private const string Superunicum = "A00DC5";
    private const string Unicum = "D042F3";
    private const string Great = "02C9B3";
    private const string VeryGood = "318000";
    private const string Good = "44B300";
    private const string Average = "FFC71F";
    private const string BelowAverage = "FE7903";
    private const string SuperPotatium = "FE0E00";

    public static string GetColor(int personalRating) => personalRating switch
    {
        >= 0 and <= 750 => SuperPotatium,
        > 750 and <= 1100 => BelowAverage,
        > 1100 and <= 1350 => Average,
        > 1350 and <= 1550 => Good,
        > 1550 and <= 1750 => VeryGood,
        > 1750 and <= 2100 => Great,
        > 2100 and <= 2450 => Unicum,
        _ => Superunicum,
    };

    public static string GetColor(double winRate) => winRate switch
    {
        >= 0 and < 46 => SuperPotatium,
        >= 47 and < 49 => BelowAverage,
        >= 49 and < 52 => Average,
        >= 52 and < 54 => Good,
        >= 54 and < 56 => VeryGood,
        >= 56 and < 60 => Great,
        >= 60 and < 64 => Unicum,
        _ => Superunicum,
    };
}