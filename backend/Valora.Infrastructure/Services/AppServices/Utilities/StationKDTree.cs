using System.Collections.Generic;
using System.Linq;
using Valora.Domain.Common;

namespace Valora.Infrastructure.Services.AppServices.Utilities;

public class StationKDNode
{
    public (string Id, string Name, double Lat, double Lon) Station { get; set; }
    public StationKDNode? Left { get; set; }
    public StationKDNode? Right { get; set; }
    public int Axis { get; set; }
}

public class StationKDTree
{
    private readonly StationKDNode? _root;

    public StationKDTree(IEnumerable<(string Id, string Name, double Lat, double Lon)> stations)
    {
        _root = BuildTree(stations.ToList(), 0);
    }

    private StationKDNode? BuildTree(List<(string Id, string Name, double Lat, double Lon)> stations, int depth)
    {
        if (stations.Count == 0) return null;

        int axis = depth % 2;
        stations.Sort((a, b) => axis == 0 ? a.Lat.CompareTo(b.Lat) : a.Lon.CompareTo(b.Lon));
        int median = stations.Count / 2;

        return new StationKDNode
        {
            Station = stations[median],
            Axis = axis,
            Left = BuildTree(stations.GetRange(0, median), depth + 1),
            Right = BuildTree(stations.GetRange(median + 1, stations.Count - (median + 1)), depth + 1)
        };
    }

    public (string Id, string Name, double DistanceMeters)? FindNearest(double targetLat, double targetLon)
    {
        if (_root == null) return null;

        StationKDNode? bestNode = null;
        double bestDist = double.MaxValue;

        SearchNearest(_root, targetLat, targetLon, ref bestNode, ref bestDist);

        return bestNode == null ? null : (bestNode.Station.Id, bestNode.Station.Name, bestDist);
    }

    private void SearchNearest(StationKDNode? node, double targetLat, double targetLon, ref StationKDNode? bestNode, ref double bestDist)
    {
        if (node == null) return;

        double d = GeoDistance.BetweenMeters(targetLat, targetLon, node.Station.Lat, node.Station.Lon);
        if (d < bestDist)
        {
            bestDist = d;
            bestNode = node;
        }

        double targetValue = node.Axis == 0 ? targetLat : targetLon;
        double nodeValue = node.Axis == 0 ? node.Station.Lat : node.Station.Lon;

        StationKDNode? first = targetValue < nodeValue ? node.Left : node.Right;
        StationKDNode? second = targetValue < nodeValue ? node.Right : node.Left;

        SearchNearest(first, targetLat, targetLon, ref bestNode, ref bestDist);

        // Simple approximation for short distances
        double planeDist = node.Axis == 0
            ? GeoDistance.BetweenMeters(targetLat, targetLon, node.Station.Lat, targetLon)
            : GeoDistance.BetweenMeters(targetLat, targetLon, targetLat, node.Station.Lon);

        if (planeDist < bestDist)
        {
            SearchNearest(second, targetLat, targetLon, ref bestNode, ref bestDist);
        }
    }
}
