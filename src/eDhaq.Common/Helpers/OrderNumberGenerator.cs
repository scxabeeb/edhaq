namespace eDhaq.Common.Helpers;

public static class OrderNumberGenerator
{
    public static string Generate(int sequence, int? year = null)
    {
        var y = year ?? DateTime.UtcNow.Year;
        return $"EDQ-{y}-{sequence:D6}";
    }
}
