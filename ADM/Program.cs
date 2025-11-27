using Admin_Repository.Configuration;
using Admin_Repository.Injection;
using Admin_Service.Injection;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Shared.Classes.Model;
using Shared.Utils.Mapper;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

#region Basic Services

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

#endregion

#region MongoDB Configuration

var mongoDBSettings = builder.Configuration.GetSection("MongoDBSettings").Get<MongoDBSettings>();
if (mongoDBSettings == null)
    throw new InvalidOperationException("MongoDBSettings não configurado no appsettings.json");

if (string.IsNullOrEmpty(mongoDBSettings.ConnectionString))
    throw new InvalidOperationException("ConnectionString do MongoDB não configurada");

if (string.IsNullOrEmpty(mongoDBSettings.DatabaseNameAdmin))
    throw new InvalidOperationException("DatabaseNameAdmin não configurado");

builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDBSettings"));

builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = MongoClientSettings.FromConnectionString(mongoDBSettings.ConnectionString);
    settings.ConnectTimeout = TimeSpan.FromSeconds(30);
    settings.SocketTimeout = TimeSpan.FromSeconds(30);
    settings.ServerSelectionTimeout = TimeSpan.FromSeconds(30);
    settings.MaxConnectionPoolSize = 100;
    settings.MinConnectionPoolSize = 10;
    settings.WaitQueueTimeout = TimeSpan.FromSeconds(10);
    settings.RetryWrites = true;
    settings.RetryReads = true;
    settings.HeartbeatInterval = TimeSpan.FromSeconds(10);
    settings.HeartbeatTimeout = TimeSpan.FromSeconds(20);

    if (builder.Environment.IsDevelopment())
    {
        settings.ClusterConfigurator = cb =>
        {
            cb.Subscribe<MongoDB.Driver.Core.Events.CommandStartedEvent>(e =>
            {
                Console.WriteLine($"[MongoDB] Command: {e.CommandName} | Database: {e.DatabaseNamespace.DatabaseName}");
            });
            cb.Subscribe<MongoDB.Driver.Core.Events.CommandFailedEvent>(e =>
            {
                Console.WriteLine($"[MongoDB] Failed: {e.CommandName} | Error: {e.Failure.Message}");
            });
        };
    }

    return new MongoClient(settings);
});

builder.Services.AddScoped<IMongoDatabase>(serviceProvider =>
{
    var mongoClient = serviceProvider.GetRequiredService<IMongoClient>();
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return mongoClient.GetDatabase(settings.DatabaseNameAdmin);
});

#endregion

#region JWT Authentication

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettings == null)
    throw new InvalidOperationException("JwtSettings não configurado no appsettings.json");

if (string.IsNullOrEmpty(jwtSettings.Secret))
    throw new InvalidOperationException("JWT Secret não configurado");

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Emissor,
        ValidAudience = jwtSettings.Audiencia,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.FromMinutes(5)
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var httpContext = context.HttpContext;
            var claims = context.Principal?.Claims;

            if (claims != null)
            {
                var usuarioId = claims.FirstOrDefault(c => c.Type == "UsuarioId")?.Value;
                var empresaId = claims.FirstOrDefault(c => c.Type == "EmpresaId")?.Value;

                if (!string.IsNullOrEmpty(usuarioId))
                    httpContext.Items["UsuarioId"] = usuarioId;

                if (!string.IsNullOrEmpty(empresaId))
                    httpContext.Items["EmpresaId"] = empresaId;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

#endregion

#region Swagger Configuration

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Admin API - Sistema Alvim",
        Version = "v1",
        Description = "API administrativa para gerenciamento de empresas, módulos e configurações",
        Contact = new OpenApiContact
        {
            Name = "Suporte Técnico",
            Email = "suporte@alvim.com.br"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header usando Bearer scheme.\n\n" +
                     "Digite 'Bearer' [espaço] e depois seu token.\n\n" +
                     "Exemplo: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...\""
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    c.UseInlineDefinitionsForEnums();
    c.OrderActionsBy(apiDesc => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}");

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

#endregion

#region DbContext Configuration

builder.Services.AddScoped<ContextBaseAdmin>(serviceProvider =>
{
    var mongoSettings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    var options = new DbContextOptionsBuilder<ContextBaseAdmin>().Options;
    return new ContextBaseAdmin(options, Options.Create(mongoSettings));
});

#endregion

#region Dependency Injection

// Serviços de comunicação (Email e WhatsApp) - Compartilhados
builder.Services.AddScoped<Shared.Services.Interface.IEmailService, Shared.Services.EmailService>();
builder.Services.AddScoped<Shared.Services.Interface.IWhatsAppService, Shared.Services.WhatsAppService>();

// Serviço de provisionamento de banco de dados
builder.Services.AddScoped<Shared.Services.Interface.IDatabaseProvisioningService, Shared.Services.DatabaseProvisioningService>();

builder.Services.AddInjectionServiceAdmin();
builder.Services.AddInjectionRepositoryAdmin();

#endregion

#region AutoMapper Configuration

var mappingConfig = new MapperConfiguration(mc => mc.AddProfile(new MappingProfile()));
builder.Services.AddSingleton(mappingConfig.CreateMapper());

#endregion

#region CORS Configuration

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

#endregion

var app = builder.Build();

#region MongoDB Startup Validation

try
{
    using var scope = app.Services.CreateScope();
    var mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();
    var settings = scope.ServiceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;

    var adminDatabase = mongoClient.GetDatabase(settings.DatabaseNameAdmin);
    await adminDatabase.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}");

    app.Logger.LogInformation("✓ Conexão com MongoDB Admin ({Database}) estabelecida", settings.DatabaseNameAdmin);

    var empresaCollection = adminDatabase.GetCollection<MongoDB.Bson.BsonDocument>("Empresa");
    var empresaCount = await empresaCollection.CountDocumentsAsync(MongoDB.Bson.BsonDocument.Parse("{}"));
    app.Logger.LogInformation("✓ Empresas cadastradas: {Count}", empresaCount);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "✗ Falha crítica ao conectar com MongoDB");
    throw;
}

#endregion

#region Middleware Configuration

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Admin API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Admin API - Documentação";
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        c.DefaultModelsExpandDepth(2);
        c.EnableDeepLinking();
        c.DisplayRequestDuration();
        c.EnableFilter();
        c.ShowExtensions();
    });
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

#endregion

#region Health Checks

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        service = "ADM API",
        version = "1.0.0",
        environment = app.Environment.EnvironmentName,
        timestamp = DateTime.UtcNow
    });
}).AllowAnonymous().WithTags("Health");

app.MapGet("/health/mongodb/admin", async (
    IMongoClient mongoClient,
    IOptions<MongoDBSettings> settings) =>
{
    try
    {
        var database = mongoClient.GetDatabase(settings.Value.DatabaseNameAdmin);
        var startTime = DateTime.UtcNow;
        await database.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}");
        var responseTime = DateTime.UtcNow - startTime;

        var empresaCollection = database.GetCollection<MongoDB.Bson.BsonDocument>("Empresa");
        var empresaCount = await empresaCollection.CountDocumentsAsync(MongoDB.Bson.BsonDocument.Parse("{}"));

        return Results.Ok(new
        {
            status = "healthy",
            database = settings.Value.DatabaseNameAdmin,
            type = "admin",
            responseTime = $"{responseTime.TotalMilliseconds:F2}ms",
            empresasCount = empresaCount,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            title: "MongoDB Admin Health Check Failed",
            statusCode: 503
        );
    }
}).AllowAnonymous().WithTags("Health");

#endregion

app.Logger.LogInformation("🚀 Admin API iniciada com sucesso");
app.Logger.LogInformation("📚 Swagger: /swagger");
app.Logger.LogInformation("💚 Health: /health");

app.Run();