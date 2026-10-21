using MaxMind.Db;
using Microsoft.Extensions.Primitives;
using Npgsql;
using System.Net;
using System.Threading.Channels;
using UShort.SQLRepositories;
namespace UShort.Services
{
    public class TelemetryPayload
    {
        public IPAddress? ClientAddress;
        public string? UserAgent = null;

        public TelemetryPayload(HttpContext context)
        {
            ClientAddress = context.Connection.RemoteIpAddress;
            if (context.Request.Headers.TryGetValue("User-Agent", out StringValues userAgent) == true)
            {
                UserAgent = userAgent.ToString();
            }
        }
    }



    public class TelemetryConsumerService : BackgroundService
    {
        private readonly ChannelReader<(TelemetryPayload payload, int linkId, char eventType)> _reader;
        private readonly Reader _mmdbReader;
        private readonly ILogger<TelemetryConsumerService> _logger;
        private readonly EventsRepository _eventsRepository;


        public TelemetryConsumerService(Channel<(TelemetryPayload payload, int linkId, char eventType)> channel, Reader mmdbReader, EventsRepository eventsRepository, ILogger<TelemetryConsumerService> logger)
        {
            _reader = channel.Reader;
            _logger = logger;
            _mmdbReader = mmdbReader;
            _eventsRepository = eventsRepository;   
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var payload in _reader.ReadAllAsync(stoppingToken))
                {

                    SQLModels.EventModel ev = new SQLModels.EventModel()
                    {
                        EventType = payload.eventType,
                        LinkId = payload.linkId,
                        Timestamp = DateTime.UtcNow
                    };

                    if (payload.payload.ClientAddress != null)
                    {
                        var data = _mmdbReader.Find<Dictionary<string, object>>(payload.payload.ClientAddress);
                        //var data = _mmdbReader.Find<Dictionary<string, object>>(IPAddress.Parse("91.79.128.12"));

                        // Getting geo data from mmdb

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


                    ev.UserAgent = payload.payload.UserAgent;


                    await _eventsRepository.CreateEventAsync(ev);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // CancellationToken cancel 
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
