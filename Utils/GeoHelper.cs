namespace MissaoBackend.Utils;

public static class GeoHelper
{
    // Distância em linha reta entre duas coordenadas, em quilómetros (fórmula de Haversine).
    public static double DistanciaKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double raioTerraKm = 6371.0;

        double dLat = ParaRadianos(lat2 - lat1);
        double dLon = ParaRadianos(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ParaRadianos(lat1)) * Math.Cos(ParaRadianos(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return raioTerraKm * c;
    }

    private static double ParaRadianos(double graus) => graus * Math.PI / 180.0;
}
