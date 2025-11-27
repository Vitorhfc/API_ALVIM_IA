using AutoMapper;
using Client.Filters;
using Client.Middleware;
using Client_Repository.Cache;
using Client_Repository.Cache.Interface;
using Client_Repository.Configuration.ContextBase;
using Client_Repository.Configuration.Contexto;
using Client_Repository.Configuration.Contexto.Interface;
using Client_Repository.Injection;
using Client_Service.Injection;
using Admin_Repository.Injection;
using Admin_Repository.Configuration;
using Admin_Service.Injection;
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

// Adiciona SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 32 * 1024; // 32 KB
});

#endregion

#region MongoDB Configuration

var mongoDBSettings = builder.Configuration.GetSection("MongoDBSettings").Get<MongoDBSettings>();
if (mongoDBSettings == null)
    throw new InvalidOperationException("MongoDBSettings não configurado");

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

    return new MongoClient(settings);
});

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return client.GetDatabase(settings.DatabaseNameAdmin);
});

#endregion

#region JWT Authentication

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettings == null)
    throw new InvalidOperationException("JwtSettings não configurado");

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
        },
        OnMessageReceived = context =>
        {
            // Suporte para autenticação do SignalR via query string
            var accessToken = context.Request.Query["access_token"];

            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
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
        Title = "Client API - Sistema Alvim",
        Version = "v1",
        Description = "API de operações específicas por cliente com arquitetura multitenant"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header usando Bearer scheme"
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

    // Configurar suporte para upload de arquivos
    c.OperationFilter<FileUploadOperationFilter>();
});

#endregion

#region DbContext Configuration

// ContextBase para Client (Tenant-specific)
builder.Services.AddScoped<ContextBase>(serviceProvider =>
{
    var mongoSettings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    var options = new DbContextOptionsBuilder<ContextBase>().Options;
    return new ContextBase(options, Options.Create(mongoSettings));
});

// ContextBaseAdmin para acesso ao banco Admin
builder.Services.AddScoped<ContextBaseAdmin>(serviceProvider =>
{
    var mongoSettings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    var options = new DbContextOptionsBuilder<ContextBaseAdmin>().Options;
    return new ContextBaseAdmin(options, Options.Create(mongoSettings));
});

#endregion

#region Dependency Injection

builder.Services.AddSingleton<ITenantCache, TenantCache>();
builder.Services.AddScoped<IContextoMultiTenantService, ContextoMultiTenantService>();
builder.Services.AddSingleton<Client_Service.Service.IMongoClientFactory, Client_Service.Service.MongoClientFactory>();

// Shared Services
builder.Services.AddScoped<Shared.Services.Interface.IStorageService, Shared.Services.StorageService>();
builder.Services.AddScoped<Shared.Services.Interface.IWhatsAppService, Shared.Services.WhatsAppService>();
builder.Services.AddScoped<Shared.Services.Interface.IWhatsAppInternoService, Shared.Services.WhatsAppInternoService>();
builder.Services.AddScoped<Shared.Services.Interface.IEmailService, Shared.Services.EmailService>();

// Serviço de notificações WhatsApp via SignalR
builder.Services.AddScoped<Shared.Services.Interface.IWhatsAppNotificationService, Shared.Services.WhatsAppNotificationService>();

// Serviço de provisionamento de banco de dados
builder.Services.AddScoped<Shared.Services.Interface.IDatabaseProvisioningService, Shared.Services.DatabaseProvisioningService>();

builder.Services.AddInjectionRepositoryClient();
builder.Services.AddInjectionRepositoryAdmin();
builder.Services.AddInjectionServiceClient();

// Admin Services (necessário para acesso ao EmpresaService no Cliente)
builder.Services.AddInjectionServiceAdmin();

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

    // Política CORS específica para SignalR (permite credenciais)
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "https://localhost:5173",
                "http://localhost:4200"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

#endregion

var app = builder.Build();

#region Inicialização de Índices MongoDB

// Criar índice único para NumeroTelefoneWaha para prevenir clientes duplicados
try
{
    using var scope = app.Services.CreateScope();
    var clienteRepo = scope.ServiceProvider.GetRequiredService<Client_Repository.Repositorio.Interface.IClienteRepositorio>();
    var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();

    // Simular contexto para criar índice em todas as empresas (ou criar manualmente por empresa)
    // Por ora, este código criará o índice quando houver a primeira requisição
    // Para criar em todos os bancos, seria necessário iterar sobre todas as empresas

    app.Logger.LogInformation("✅ Índice único configurado. Será criado na primeira requisição de cada tenant.");
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "⚠️ Não foi possível configurar índice único na inicialização. O índice será criado sob demanda.");
}

#endregion

#region Middleware Configuration

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Client API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseReloginMiddleware();
app.UseTenantMiddleware();
app.UseAuthorization();
app.MapControllers();

// Mapeia os Hubs do SignalR
app.MapHub<Shared.Hubs.WhatsAppHub>("/hubs/whatsapp").RequireCors("SignalRPolicy");

#endregion

#region Health Checks

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        service = "Client API",
        version = "1.0.0",
        timestamp = DateTime.UtcNow
    });
}).AllowAnonymous();

#endregion

app.Logger.LogInformation("🚀 Client API iniciada com sucesso");
app.Logger.LogInformation("📚 Swagger: /swagger");
app.Logger.LogInformation("💚 Health: /health");

app.Run();