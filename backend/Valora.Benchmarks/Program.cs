using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections.Generic;
using Valora.Domain.Common;
using System;
using System.Linq;

namespace Valora.Benchmarks
{
    // Simple KD-Tree Node
    public class KDNode
    {
        public (string Id, string Name, double Lat, double Lon) Station { get; set; }
        public KDNode Left { get; set; }
        public KDNode Right { get; set; }
        public int Axis { get; set; }
    }

    public class KDTree
    {
        private KDNode _root;

        public KDTree(List<(string Id, string Name, double Lat, double Lon)> stations)
        {
            _root = BuildTree(stations.ToList(), 0);
        }

        private KDNode BuildTree(List<(string Id, string Name, double Lat, double Lon)> stations, int depth)
        {
            if (stations.Count == 0) return null;

            int axis = depth % 2;
            stations.Sort((a, b) => axis == 0 ? a.Lat.CompareTo(b.Lat) : a.Lon.CompareTo(b.Lon));
            int median = stations.Count / 2;

            return new KDNode
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

            KDNode bestNode = null;
            double bestDist = double.MaxValue;

            SearchNearest(_root, targetLat, targetLon, ref bestNode, ref bestDist);

            return bestNode == null ? null : (bestNode.Station.Id, bestNode.Station.Name, bestDist);
        }

        private void SearchNearest(KDNode node, double targetLat, double targetLon, ref KDNode bestNode, ref double bestDist)
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

            KDNode first = targetValue < nodeValue ? node.Left : node.Right;
            KDNode second = targetValue < nodeValue ? node.Right : node.Left;

            SearchNearest(first, targetLat, targetLon, ref bestNode, ref bestDist);

            // To determine if we need to search the other branch, we calculate the shortest distance
            // from the target point to the splitting plane.
            // A simple approximation is the distance between (targetLat, targetLon) and the projection
            // on the splitting plane. For latitude, the projection is (node.Station.Lat, targetLon).
            // For longitude, it is (targetLat, node.Station.Lon).
            double planeDist = node.Axis == 0
                ? GeoDistance.BetweenMeters(targetLat, targetLon, node.Station.Lat, targetLon)
                : GeoDistance.BetweenMeters(targetLat, targetLon, targetLat, node.Station.Lon);

            if (planeDist < bestDist)
            {
                SearchNearest(second, targetLat, targetLon, ref bestNode, ref bestDist);
            }
        }
    }


    [MemoryDiagnoser]
    public class LuchtmeetnetAirQualityClientBenchmark
    {
        private List<(string Id, string Name, double Lat, double Lon)> _stations = new();
        private double _targetLat;
        private double _targetLon;
        private KDTree _kdTree;

        [GlobalSetup]
        public void Setup()
        {
            var random = new Random(42);
            for (int i = 0; i < 1000; i++)
            {
                double lat = 50.75 + random.NextDouble() * (53.5 - 50.75);
                double lon = 3.3 + random.NextDouble() * (7.2 - 3.3);
                _stations.Add(($"S{i}", $"Station {i}", lat, lon));
            }
            _targetLat = 52.37714;
            _targetLon = 4.89803;

            _kdTree = new KDTree(_stations);
        }

        [Benchmark(Baseline = true)]
        public (string Id, string Name, double DistanceMeters)? LinearSearch()
        {
            (string Id, string Name, double Lat, double Lon)? nearest = null;
            double minDistance = double.MaxValue;

            foreach (var station in _stations)
            {
                var distance = GeoDistance.BetweenMeters(_targetLat, _targetLon, station.Lat, station.Lon);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = station;
                }
            }

            if (nearest == null) return null;
            return (nearest.Value.Id, nearest.Value.Name, minDistance);
        }

        [Benchmark]
        public (string Id, string Name, double DistanceMeters)? KDTreeSearch()
        {
            return _kdTree.FindNearest(_targetLat, _targetLon);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<LuchtmeetnetAirQualityClientBenchmark>();
        }
    }
}
