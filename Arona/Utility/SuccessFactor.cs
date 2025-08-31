namespace Arona.Utility;

public static class SuccessFactor
{
    
    public static double Calculate(int rating, int battlesCount, double leagueExponent)
    {
        // Subtrahera 1000 för att ta bort överskott med rating som läggs till i public rating
        int normalizedRating = rating - 1000;
        int battles = Math.Max(battlesCount, 1);

        return Math.Round((Math.Pow(normalizedRating, leagueExponent) / 15) * ((double)normalizedRating / battles) / 10, 2);
    }
}