using System.Data;
using System.Text;
using Api.Common;
using Api.Common.Middlewares;
using Api.Domain.Abstractions.Infrastructure.Messaging;
using Api.Domain.Abstractions.UseCases;
using Api.Domain.Entities.Enums;
using Api.Features.Ingestion;
using Api.Infrastructure.Messaging;
using Api.Infrastructure.Persistence.Contexts;
using Api.Infrastructure.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Api;

public static class DependencyInjectionExtension
{
    public static IServiceCollection AddDependecyInjection(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructure(configuration);
        services.AddApplication();
        services.AddPresentation(configuration);
        return services;
    }

    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddOpenApi();
        services.AddJwtAuthenticationAndAuthorization(configuration);
        services.AddHealthChecks();

        return services;
    }

    private static IServiceCollection AddJwtAuthenticationAndAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services
            .AddAuthorizationBuilder()
            .AddPolicy(Policies.UserOnly, policy => policy.RequireRole(nameof(Roles.User)))
            .AddPolicy(Policies.AdministratorOnly, policy => policy.RequireRole(nameof(Roles.Admin)));

        return services;
    }

    public static IHostBuilder AddSerilog(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

        return hostBuilder;
    }

    public static void ApplyMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();

        using var context = scope.ServiceProvider.GetRequiredService<SensorDbContext>();

        context.Database.Migrate();
    }

    private static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddContexts(configuration);
        services.AddSettings(configuration);
        services.AddMessaging();

        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        services.AddScoped<IMessagePublisher, RabbitMQPublisher>();

        return services;
    }

    private static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidators();
        services.AddUseCases();

        return services;
    }

    private static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        services.AddScoped<IUseCase<IngestSensorDataRequest>, IngestSensorDataUseCase>();

        return services;
    }

    private static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjectionExtension).Assembly, ServiceLifetime.Scoped, includeInternalTypes: true);

        return services;
    }

    private static IServiceCollection AddContexts(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionString not configured.");

        services.AddDbContext<SensorDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }

    private static IServiceCollection AddSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSetting>(configuration.GetSection(JwtSetting.SectionName));

        services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<JwtSetting>>().Value);

        services.Configure<RabbitMQSettings>(configuration.GetSection(RabbitMQSettings.SectionName));

        services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<RabbitMQSettings>>().Value);

        return services;
    }
}
