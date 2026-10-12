namespace RideMatching.Application.Common;

public static class GeoCalculator
{
    private const double EarthRadiusKm = 6371.0;

    /// <summary>
    /// Calculates the great-circle distance between two points in kilometers using Haversine formula.
    /// </summary>
    public static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        if (Math.Abs(lat1 - lat2) < 0.000001 && Math.Abs(lon1 - lon2) < 0.000001)
            return 0.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2)) +
                (Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2));

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(EarthRadiusKm * c, 2);
    }

    /// <summary>
    /// Calculates initial compass bearing from point 1 to point 2 in degrees (0 - 360).
    /// </summary>
    public static double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
    {
        var dLon = DegreesToRadians(lon2 - lon1);
        var rLat1 = DegreesToRadians(lat1);
        var rLat2 = DegreesToRadians(lat2);

        var y = Math.Sin(dLon) * Math.Cos(rLat2);
        var x = (Math.Cos(rLat1) * Math.Sin(rLat2)) -
                (Math.Sin(rLat1) * Math.Cos(rLat2) * Math.Cos(dLon));

        var brng = Math.Atan2(y, x);
        var degrees = RadiansToDegrees(brng);
        return (degrees + 360.0) % 360.0;
    }

    /// <summary>
    /// Estimates driving time in minutes assuming average urban traffic speed (30 km/h) plus baseline.
    /// </summary>
    public static double EstimateDurationMinutes(double distanceKm, double avgSpeedKmh = 30.0)
    {
        if (distanceKm <= 0) return 1.0;
        var hours = distanceKm / Math.Max(avgSpeedKmh, 10.0);
        var minutes = (hours * 60.0) + 2.0; // 2 min buffer for traffic/signals
        return Math.Round(Math.Max(minutes, 2.0), 1);
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
    private static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;
}
