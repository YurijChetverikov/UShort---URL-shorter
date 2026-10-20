using MaxMind.Db;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using System.Net;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using UShort.Services;
using UShort.SQLRepositories;

namespace UShort
{
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }

    public class Program
    {

        private static IConfiguration? _configuration;
        private static UrlRepository _urlRepository = new UrlRepository();


        private static int _maxLinkLength = 64;
        private static string _geoipFilename = "";
        private static string _connectionString = "";


        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private static Channel<Channel<(HttpRequest req, char eventType)>, Channel<SQLModels.EventModel>> _telemetryChannel;
        private static MaxMind.Db.Reader _mmdbReader;
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            using var startupLoggerFactory = LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddConsole();
            });

            ILogger startupLogger = startupLoggerFactory.CreateLogger<Program>();

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            });

            builder.Services.AddSingleton<Channel<(HttpContext context, int linkId, char eventType)>>(sp =>
            {
                var options = new UnboundedChannelOptions()
                {
                    AllowSynchronousContinuations = false,
                    SingleWriter = true,
                    SingleReader = true
                };
                return Channel.CreateUnbounded<(HttpContext context, int linkId, char eventType)>(options);
            });

            _geoipFilename = builder.Configuration.GetValue<string>("GeoIP2FileName");
            if (File.Exists(_geoipFilename) == false)
            {
                startupLogger.LogError($"Unable to locate GeoIP2 .mmdb file at \"{_geoipFilename}\"");
                return;
            }
            _connectionString = builder.Configuration.GetValue<string>("ConnectionString");
            if (_connectionString == null)
            {
                startupLogger.LogError($"ConnectionString value is null");
                return;
            }

            builder.Services.AddSingleton<MaxMind.Db.Reader>(sp =>
            {
                return MaxMind.Db.Reader.CreateAsync(_geoipFilename).GetAwaiter().GetResult();
            });

            builder.Services.AddSingleton<EventsRepository>(sp =>
            {
                var e = new EventsRepository();

                if (e.Init(builder.Configuration.GetValue<string>("ConnectionString")) == false)
                {
                    throw new Exception("Can't connect to database");
                }

                return e;
            });
            builder.Services.AddSingleton<LinksHashesRepository>(sp =>
            {
                var l = new LinksHashesRepository();

                if (l.Init(builder.Configuration.GetValue<string>("ConnectionString")) == false)
                {
                    throw new Exception("Can't connect to database");
                }

                return l;
            });

            builder.Services.AddHostedService<TelemetryConsumerService>();
            //builder.Services.AddHostedService<MyConsumer>();

            var app = builder.Build();

            _configuration = app.Services.GetRequiredService<IConfiguration>();

            if (_urlRepository.Init(_configuration.GetValue<string>("ConnectionString")) == false)
            {
                app.Logger.LogError("Can't connect to database");
                return;
            }




            _mmdbReader = new Reader(_geoipFilename);

            _maxLinkLength = _configuration.GetValue<int>("MaximumLinkLength");

            _mmdbReader = new Reader("C:\\Users\\Admin\\Downloads\\GeoLite2-City.mmdb");

            //var channelOptions = new UnboundedChannelOptions()
            //{
            //    AllowSynchronousContinuations = false,
            //    SingleWriter = true,
            //    SingleReader = true
            //};

            //Channel<(HttpRequest request, char eventType)> channel = Channel.CreateUnbounded<(HttpRequest request, char eventType)>(channelOptions);

            //Task consumerTask = consumeTelemetryDataAsync(channel.Reader, _cts.Token);

            app.MapGet("/createLink", async (HttpContext context, Channel<(HttpContext context, int linkId, char eventType)> channel, LinksHashesRepository linksHashesRepository, CancellationToken cancellationToken) =>
            {
                var linkQuery = context.Request.Query.FirstOrDefault(x => x.Key == "link");

                if (linkQuery.Key == null)
                    return Results.BadRequest("Requested parameter \"link\" is not provided");

                string link = linkQuery.Value;

                if (link == null)
                    return Results.BadRequest("Requested parameter \"link\" is not provided");

                if (link.Length > _maxLinkLength)
                    return Results.BadRequest("The length of parameter \"link\" exceeds the limit");

                if (isLinkValid(link) == false)
                    return Results.BadRequest("Requested parameter \"link\" is not valid URL");

                int checkedLinkId = await linksHashesRepository.CheckLinkAsync(link);

                if (checkedLinkId != -1)
                    return Results.Ok(checkedLinkId);

                // If inserting link causes us to exceed the maximum value of the integer type,
                // postgres will return an error "numeric_value_out_of_range" with the code 22003

                try
                {
                    int id = await _urlRepository.CreateUrlAsync(link);
                    await linksHashesRepository.AddLinkAsync(link, id);
                    await channel.Writer.WriteAsync((context, id, 'C'));

                    return Results.Ok(id);
                }
                catch (PostgresException ex) when (ex.SqlState == "22003")
                {
                    app.Logger.LogError($"Can't create link: links limit exceeded");
                }
                catch (Exception ex)
                {
                    app.Logger.LogError(ex, "While creating link");
                }

                return Results.InternalServerError();
            });

            app.Run();
        }


        private static bool isLinkValid(string link)
        {
            if (!Uri.TryCreate(link, UriKind.Absolute, out Uri? uriResult))
                return false;

            if (uriResult.IsWellFormedOriginalString() == false)
                return false;

            return true;
        }
    }
}
