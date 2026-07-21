using Npgsql;
using AmazonScraper.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Load .env file from backend/ (one level above the api/ project root)
var envFile = Path.Combine(builder.Environment.ContentRootPath, "..", ".env");
if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#')) continue;
        var eqIdx = line.IndexOf('=');
        if (eqIdx > 0)
            Environment.SetEnvironmentVariable(line[..eqIdx].Trim(), line[(eqIdx + 1)..].Trim());
    }
}

// Build Npgsql connection string from env vars
var dbHost     = Environment.GetEnvironmentVariable("HOST")          ?? throw new InvalidOperationException("Missing env var: HOST");
var dbUser     = Environment.GetEnvironmentVariable("USERNAME")      ?? throw new InvalidOperationException("Missing env var: USERNAME");
var dbPassword = Environment.GetEnvironmentVariable("PASSWORD")      ?? throw new InvalidOperationException("Missing env var: PASSWORD");
var dbName     = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? throw new InvalidOperationException("Missing env var: DATABASE_NAME");

var connectionString = $"Host={dbHost};Username={dbUser};Password={dbPassword};Database={dbName};SSL Mode=Require;Trust Server Certificate=true";
var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();
builder.Services.AddSingleton(dataSource);
builder.Services.AddScoped<InventoryRepository>();
builder.Services.AddScoped<UsersRepository>();

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("frontend");
app.MapControllers();
app.Run();
