using Monroe.Config;
using Monroe.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();


builder.Services.AddControllers();
builder.Services.AddHttpClient("llama");

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddSingleton<LlamaProcessManager>();
builder.Services.AddSingleton<LlamaForwarder>();
builder.Services.AddSingleton<LlamaStreamingForwarder>();
builder.Services.AddSingleton<Classifier>();
builder.Services.AddSingleton<ModelRouter>();
var models = builder.Configuration
    .GetSection("Models")
    .Get<List<ModelConfig>>() ?? [];

builder.Services.AddSingleton(models);


var app = builder.Build();

// Start llama.cpp automatically
var llama = app.Services.GetRequiredService<LlamaProcessManager>();
llama.Start();
llama.StartClassifier();


app.MapControllers();

var ProxyPort = builder.Configuration.GetValue<int>("ProxyPort", 8080);
app.Run($"http://*:{ProxyPort}");