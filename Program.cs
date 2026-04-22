LoadDotEnv(Directory.GetCurrentDirectory());

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

static void LoadDotEnv(string contentRootPath)
{
    var envPath = Path.Combine(contentRootPath, ".env");

    if (!File.Exists(envPath))
    {
        return;
    }

    foreach (var line in File.ReadAllLines(envPath))
    {
        var trimmedLine = line.Trim();

        if (
            string.IsNullOrWhiteSpace(trimmedLine)
            || trimmedLine.StartsWith('#')
            || !trimmedLine.Contains('=')
        )
        {
            continue;
        }

        var separatorIndex = trimmedLine.IndexOf('=');
        var key = trimmedLine[..separatorIndex].Trim();
        var value = trimmedLine[(separatorIndex + 1)..].Trim();

        if (string.IsNullOrWhiteSpace(key) || Environment.GetEnvironmentVariable(key) is not null)
        {
            continue;
        }

        Environment.SetEnvironmentVariable(key, Unquote(value));
    }
}

static string Unquote(string value)
{
    if (value.Length >= 2)
    {
        var first = value[0];
        var last = value[^1];

        if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
        {
            return value[1..^1];
        }
    }

    return value;
}
