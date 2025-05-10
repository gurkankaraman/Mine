using ChatInsightGemini;
using ChatInsightGemini.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<GeminiClient>();
builder.Services.AddControllersWithViews(); // sadece AddControllers deðil!
builder.Services.AddScoped<ConversationProcessor>();


var app = builder.Build();

// Configure the HTTP request pipeline.


app.MapDefaultControllerRoute();

app.UseAuthorization();


app.MapControllers();

app.Run();

