using Npgsql;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using UShort.Services;
using UShort.SQLModels;
using UShort.SQLRepositories;
using UShort.Utils;

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

        private static int _maxLinkLength = 64;
        private static string _geoipFilename = "";
        private static string _connectionString = "";
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            // Fixing CORS in localhost testing environment
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: "_allowLocalhostOrigins",
                    policy =>
                    {
                        policy.WithOrigins("http://127.0.0.1:5500");
                    });
            });

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

            builder.Services.AddSingleton<Channel<(TelemetryPayload payload, int linkId, char eventType)>>(sp =>
            {
                var options = new UnboundedChannelOptions()
                {
                    AllowSynchronousContinuations = false,
                    SingleWriter = true,
                    SingleReader = true
                };
                return Channel.CreateUnbounded<(TelemetryPayload payload, int linkId, char eventType)>(options);
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

            builder.Services.AddSingleton<UrlRepository>(sp =>
            {
                var u = new UrlRepository();

                if (u.Init(builder.Configuration.GetValue<string>("ConnectionString")) == false)
                {
                    throw new Exception("Can't connect to database");
                }

                return u;
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

            var app = builder.Build();

            app.UseCors("_allowLocalhostOrigins");

            app.UseStaticFiles();

            _configuration = app.Services.GetRequiredService<IConfiguration>();

            _maxLinkLength = _configuration.GetValue<int>("MaximumLinkLength");

            // Redirecting to original link
            app.MapGet("/r/{id}", (async Task<IResult> (string id, HttpContext context, Channel<(TelemetryPayload payload, int linkId, char eventType)> channel, UrlRepository urlRepository, CancellationToken cancellationToken) =>
            {
                int idInt = Base58.Decode(id);
                URLModel? link = await urlRepository.GetUrlAsync(idInt);

                if (link == null)
                {
                    await channel.Writer.WriteAsync((new TelemetryPayload(context), -1, 'R'));
                    return Results.LocalRedirect("/linkNotFound.html");
                }

                await channel.Writer.WriteAsync((new TelemetryPayload(context), idInt, 'R'));
                return Results.Redirect(link.Link);
            }));

            // Creating short link
            app.MapPost("/api/createLink", async (HttpContext context, Channel<(TelemetryPayload payload, int linkId, char eventType)> channel, LinksHashesRepository linksHashesRepository, UrlRepository urlRepository, CancellationToken cancellationToken) =>
            {
                var linkQuery = context.Request.Query.FirstOrDefault(x => x.Key == "link");

                if (linkQuery.Key == null)
                    return Results.Text("Requested parameter \"link\" is not provided", statusCode: 400);

                string link = linkQuery.Value;

                if (link == null)
                    return Results.Text("Requested parameter \"link\" is not provided", statusCode: 400);

                if (link.Length > _maxLinkLength)
                    return Results.Text("The length of parameter \"link\" exceeds the limit", statusCode: 400);

                if (isLinkValid(link) == false)
                    return Results.Text("Requested parameter \"link\" is not valid URL", statusCode: 400);

                // Checking if we already have this link

                int checkedLinkId = await linksHashesRepository.CheckLinkAsync(link);

                if (checkedLinkId != -1)
                    return Results.Text(checkedLinkId.ToString(), statusCode: 200);
               
                try
                {
                    int id = await urlRepository.CreateUrlAsync(link);
                    await linksHashesRepository.AddLinkAsync(link, id);
                    await channel.Writer.WriteAsync((new TelemetryPayload(context), id, 'C'));

                    return Results.Text(id.ToString(), statusCode: 200);
                }
                catch (PostgresException ex) when (ex.SqlState == "22003")
                {
                    // If inserting link causes us to exceed the maximum value of the integer type,
                    // postgres will return an error "numeric_value_out_of_range" with the code 22003
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
