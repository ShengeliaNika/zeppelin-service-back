using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Zeppelin.Common;
using Zeppelin;
using Zeppelin.Auth;
using Zeppelin.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer();

// Configured via IOptions<JwtOptions> (resolved from DI at first use, after
// the host is fully built) rather than reading builder.Configuration eagerly
// here - the eager read misses configuration providers appended after this
// point (e.g. WebApplicationFactory's ConfigureAppConfiguration in tests),
// which previously caused the JWT bearer handler to validate against a
// stale/empty signing key while TokenService (already DI-based) used the
// correct one.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.ClinicalStaff, p => p.RequireRole(Roles.Admin, Roles.Dentist, Roles.Hygienist))
    .AddPolicy(Policies.SchedulingStaff, p => p.RequireRole(Roles.Admin, Roles.Dentist, Roles.Hygienist, Roles.FrontDesk))
    .AddPolicy(Policies.AdminOnly, p => p.RequireRole(Roles.Admin))
    .AddPolicy(Policies.FinancialData, p => p.RequireRole(Roles.Admin));

const string DevCorsPolicy = "DevCors";
builder.Services.AddCors(options => options.AddPolicy(DevCorsPolicy, _ => { }));

// Same reasoning as the JWT options above: build the actual policy lazily
// (via PostConfigure, resolved with the final IConfiguration) instead of
// reading allowed origins from builder.Configuration at registration time.
builder.Services.PostConfigure<CorsOptions>(options =>
{
    var configuration = builder.Configuration;
    var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    options.AddPolicy(DevCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(DevCorsPolicy);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program;
