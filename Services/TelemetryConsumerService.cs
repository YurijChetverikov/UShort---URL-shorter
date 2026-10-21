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
        public readonly int LinkId = -1;
        public readonly char EventType = (char)0;

        public readonly IPAddress? ClientAddress;
        public readonly string? UserAgent = null;

        public TelemetryPayload(int linkId, char eventType, HttpContext context)
        {
            LinkId = linkId;
            EventType = eventType;

            ClientAddress = context.Connection.RemoteIpAddress;
            if (context.Request.Headers.TryGetValue("User-Agent", out StringValues userAgent) == true)
            {
                UserAgent = userAgent.ToString();
            }
        }
    }



    public class TelemetryConsumerService : BackgroundService
    {
        private const int BATCH_SIZE = 100;
        private readonly TimeSpan BATCH_SEND_INTERVAL = TimeSpan.FromSeconds(1);


        private readonly ChannelReader<TelemetryPayload> _reader;
        private readonly Reader _mmdbReader;
        private readonly ILogger<TelemetryConsumerService> _logger;
        private readonly EventsRepository _eventsRepository;





        public TelemetryConsumerService(Channel<TelemetryPayload> channel, Reader mmdbReader, EventsRepository eventsRepository, ILogger<TelemetryConsumerService> logger)
        {
            _reader = channel.Reader;
            _logger = logger;
            _mmdbReader = mmdbReader;
            _eventsRepository = eventsRepository;   
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            List<SQLModels.EventModel> eventsList = new List<SQLModels.EventModel>(BATCH_SIZE);

            while (cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    // Wait until first payload arrives (non blocking wait)
                    await _reader.WaitToReadAsync(cancellationToken);

                    // Here first payload arrived. We creating cancellation token that cancels after BATCH_SEND_INTERVAL
                    // that forces us to send eventsList to eventsRepository, even eventsList's size < that BATCH_SIZE

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(BATCH_SEND_INTERVAL);
                    try
                    {
                        while (eventsList.Count < BATCH_SIZE)
                        {
                            eventsList.Add(processPayload(await _reader.ReadAsync(cts.Token)));
                        }
                        //cts.Cancel();
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested == false)
                    {
                        // Passed BATCH_SEND_INTERVAL. Need to send what we have in eventsList
                    }

                    await _eventsRepository.BulkCreateEventsAsync(eventsList, cancellationToken);

                    eventsList.Clear();
                        
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // CancellationToken cancel 
                }
                catch (Exception ex) when (ex is InvalidCastException || ex is FormatException)
                {
                    _logger.LogWarning($"Can't cast mmdb data:\n{ex}");
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

        private SQLModels.EventModel processPayload(TelemetryPayload payload)
        {
            SQLModels.EventModel ev = new SQLModels.EventModel()
            {
                EventType = payload.EventType,
                LinkId = payload.LinkId,
                Timestamp = DateTime.UtcNow
            };

            if (payload.ClientAddress != null)
            {
                var data = _mmdbReader.Find<Dictionary<string, object>>(payload.ClientAddress);
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


            ev.UserAgent = payload.UserAgent;

            return ev;
        }
    }
}
