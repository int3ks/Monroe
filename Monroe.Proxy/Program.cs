using Monroe.Config;
using Monroe.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient("llama");
builder.Services.AddSingleton<LlamaProcessManager>();
builder.Services.AddSingleton<LlamaForwarder>();
builder.Services.AddSingleton<LlamaStreamingForwarder>();
builder.Services.AddSingleton<Classifier>();
builder.Services.AddSingleton<ModelRouter>();


var app = builder.Build();

// Start llama.cpp automatically
var llama = app.Services.GetRequiredService<LlamaProcessManager>();
llama.Start();

app.MapControllers();
app.Run($"http://*:{MonroeConfig.ProxyPort}");