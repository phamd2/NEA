using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace TransportDelayPredictor
{
    public class StationModel
    {
        public string StationId { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public string LineName { get; set; } = string.Empty;
    }

    public class RouteResult
    {
        public int StopsCount { get; set; }
        public double EstimatedMinutes { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public List<string> Directions { get; set; } = new List<string>();
    }

    public class RouteService
    {
        private readonly string _connectionString = "Data Source=SQL database/Stations Database.db;";

        public List<StationModel> GetAllStations()
        {
            var stations = new List<StationModel>();

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                string jubileeQuery = "SELECT StationId, StationName FROM Stations;";
                using (var cmd = new SqliteCommand(jubileeQuery, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stations.Add(new StationModel
                            {
                                StationId = reader.GetString(0),
                                StationName = reader.GetString(1),
                                LineName = "Jubilee Line"
                            });
                        }
                    }
                }

                string hmcQuery = "SELECT StationId, StationName FROM HammersmithandCityLine;";
                using (var cmd = new SqliteCommand(hmcQuery, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            stations.Add(new StationModel
                            {
                                StationId = reader.GetString(0),
                                StationName = reader.GetString(1),
                                LineName = "Hammersmith & City Line"
                            });
                        }
                    }
                }
            }

            return stations;
        }


        public RouteResult CalculateRoute(string originId, string destinationId)
        {
            if (string.IsNullOrWhiteSpace(originId) || string.IsNullOrWhiteSpace(destinationId))
            {
                return new RouteResult { IsSuccess = false, Message = "Please select both a current location and a destination." };
            }

            if (string.Equals(originId, destinationId, StringComparison.OrdinalIgnoreCase))
            {
                return new RouteResult { IsSuccess = false, Message = "Current location and destination cannot be the same." };
            }

            try
            {
                List<StationModel> stations = GetAllStations();
                StationModel? origin = stations.Find(station =>
                    string.Equals(station.StationId, originId.Trim(), StringComparison.OrdinalIgnoreCase));
                StationModel? destination = stations.Find(station =>
                    string.Equals(station.StationId, destinationId.Trim(), StringComparison.OrdinalIgnoreCase));

                if (origin == null || destination == null)
                {
                    return new RouteResult
                    {
                        IsSuccess = false,
                        Message = "The selected current location or destination was not found."
                    };
                }

                List<StationModel> originLine = GetOrderedLine(stations, origin.LineName);
                List<StationModel> destinationLine = GetOrderedLine(stations, destination.LineName);
                var directions = new List<string>();
                int stopsCount;
                bool requiresTransfer = !string.Equals(origin.LineName, destination.LineName, StringComparison.Ordinal);

                if (!requiresTransfer)
                {
                    stopsCount = AddSegmentDirections(origin, destination, originLine, directions);
                }
                else
                {
                    StationModel? bestOriginInterchange = null;
                    StationModel? bestDestinationInterchange = null;
                    int bestStopsCount = int.MaxValue;

                    foreach (StationModel originInterchange in originLine)
                    {
                        StationModel? destinationInterchange = destinationLine.Find(station =>
                            string.Equals(station.StationName, originInterchange.StationName, StringComparison.OrdinalIgnoreCase));

                        if (destinationInterchange == null)
                        {
                            continue;
                        }

                        int candidateStops = Math.Abs(
                            originLine.IndexOf(origin) - originLine.IndexOf(originInterchange)) +
                            Math.Abs(destinationLine.IndexOf(destinationInterchange) - destinationLine.IndexOf(destination));

                        if (candidateStops < bestStopsCount)
                        {
                            bestStopsCount = candidateStops;
                            bestOriginInterchange = originInterchange;
                            bestDestinationInterchange = destinationInterchange;
                        }
                    }

                    if (bestOriginInterchange == null || bestDestinationInterchange == null)
                    {
                        return new RouteResult
                        {
                            IsSuccess = false,
                            Message = "No interchange between these lines is available in the station data."
                        };
                    }

                    AddSegmentDirections(origin, bestOriginInterchange, originLine, directions);
                    directions.Add($"Change lines at {bestOriginInterchange.StationName} to the {destination.LineName}.");
                    AddSegmentDirections(bestDestinationInterchange, destination, destinationLine, directions);
                    stopsCount = bestStopsCount;
                }

                double estimatedMinutes = (stopsCount * 2.5) + (requiresTransfer ? 5 : 0);
                string message = $"Route found: {stopsCount} stop(s), approximately {estimatedMinutes:F1} minutes.";

                return new RouteResult
                {
                    IsSuccess = true,
                    StopsCount = stopsCount,
                    EstimatedMinutes = estimatedMinutes,
                    Message = message,
                    Directions = directions
                };
            }
            catch (SqliteException)
            {
                return new RouteResult
                {
                    IsSuccess = false,
                    Message = "Could not read the station database. Check that the database file is available."
                };
            }
        }

        private static List<StationModel> GetOrderedLine(List<StationModel> stations, string lineName)
        {
            return stations
                .Where(station => string.Equals(station.LineName, lineName, StringComparison.Ordinal))
                .OrderBy(station => int.Parse(station.StationId.Substring(1)))
                .ToList();
        }

        private static int AddSegmentDirections(
            StationModel start,
            StationModel end,
            List<StationModel> lineStations,
            List<string> directions)
        {
            int startIndex = lineStations.IndexOf(start);
            int endIndex = lineStations.IndexOf(end);
            int stops = Math.Abs(endIndex - startIndex);

            if (stops == 0)
            {
                return 0;
            }

            string direction = endIndex > startIndex
                ? lineStations[^1].StationName
                : lineStations[0].StationName;

            directions.Add(
                $"Take the {start.LineName} towards {direction} from {start.StationName} to {end.StationName} ({stops} stop(s)).");
            return stops;
        }
    }
}

