using MaxMind.Db;
using Microsoft.Extensions.Primitives;
using Npgsql;
using System.Diagnostics.Metrics;
using System.Net;
using System.Threading.Channels;
using UShort.SQLRepositories;

namespace UShort.Services
{
    public class TelemetryConsumerService : BackgroundService
    {
        private readonly ChannelReader<(HttpContext context, int linkId, char eventType)> _reader;
        private readonly Reader _mmdbReader;
        private readonly ILogger<TelemetryConsumerService> _logger;
        private readonly EventsRepository _eventsRepository;


        public TelemetryConsumerService(Channel<(HttpContext context, int linkId, char eventType)> channel, Reader mmdbReader, EventsRepository eventsRepository, ILogger<TelemetryConsumerService> logger)
        {
            _reader = channel.Reader;
            _logger = logger;
            _mmdbReader = mmdbReader;
            _eventsRepository = eventsRepository;   
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                if (_reader.CanPeek == false) await Task.Delay(10);

                await foreach (var payload in _reader.ReadAllAsync())
                {
                    try
                    {
                        SQLModels.EventModel ev = new SQLModels.EventModel()
                        {
                            EventType = payload.eventType,
                            LinkId = payload.linkId,
                            Timestamp = DateTime.UtcNow
                        };

                        var ipAddress = payload.context.Connection.RemoteIpAddress;

                        // Не забываем проверить на null
                        if (ipAddress != null)
                        {
                            //var data = _mmdbReader.Find<Dictionary<string, object>>(ipAddress);
                            var data = _mmdbReader.Find<Dictionary<string, object>>(IPAddress.Parse("91.79.128.12"));
                            if (data != null)
                            {
                                if (data.ContainsKey("continent"))
                                {
                                    if ((data["continent"] as Dictionary<string, object>).TryGetValue("geoname_id", out object continent_geonameId) == true)
                                    {
                                        ev.ContinentGeonameId = Convert.ToInt32(continent_geonameId);
                                    }
                                }
                                if (data.ContainsKey("country"))
                                {
                                    if ((data["country"] as Dictionary<string, object>).TryGetValue("geoname_id", out object country_geonameId) == true)
                                    {
                                        ev.CountryGeonameId = Convert.ToInt32(country_geonameId);
                                    }
                                }
                                if (data.ContainsKey("city"))
                                {
                                    if ((data["city"] as Dictionary<string, object>).TryGetValue("geoname_id", out object city_geonameId) == true)
                                    {
                                        ev.CityGeonameId = Convert.ToInt32(city_geonameId);
                                    }
                                }
                                if (data.ContainsKey("location"))
                                {
                                    var locationdDic = data["location"] as Dictionary<string, object>;
                                    if (locationdDic.TryGetValue("accuracy_radius", out object radius) == true)
                                    {
                                        ev.AccuracyRadius = Convert.ToInt32(radius);
                                    }
                                    if (locationdDic.TryGetValue("latitude", out object latitude) == true)
                                    {
                                        ev.Latitude = Convert.ToSingle(latitude);
                                    }
                                    if (locationdDic.TryGetValue("longitude", out object longitude) == true)
                                    {
                                        ev.Longitude = Convert.ToSingle(longitude);
                                    }
                                }
                            }
                        }

                        if (payload.context.Request.Headers.TryGetValue("User-Agent", out StringValues userAgent) == true)
                        {
                            ev.UserAgent = userAgent.ToString();
                        }

                        await _eventsRepository.CreateEventAsync(ev);
                    }
                    catch (PostgresException ex) when (ex.SqlState == "22003")
                    {
                        _logger.LogWarning($"Exception while processing telemetry:\nCan't create event: events limit exceeded");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Exception while processing telemetry:\n{ex}");
                    }
                }
            }
        }
    }
}
