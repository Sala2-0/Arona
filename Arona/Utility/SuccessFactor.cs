namespace Arona.Utility;

public static class SuccessFactor
{
    
    public static double Calculate(int publicRating, int battlesCount, double leagueExponent)
    {
        if (battlesCount == 0) return 0.0;
        
        // Subtrahera 1000 för att ta bort överskott med rating som läggs till i public rating
        int normalizedRating = publicRating - 1000;
        int battles = Math.Max(battlesCount, 1);

        return Math.Round((Math.Pow(normalizedRating, leagueExponent) / 15) * ((double)normalizedRating / battles) / 10, 2);
    }
}