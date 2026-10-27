using ControleRpa.Api.Data;
using ControleRpa.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Controle RPA - Notas Fiscais de Grãos",
        Version = "v1",
        Description = "API para controle de idempotência e observabilidade para o robô de conciliação de notas fiscais. " +
                      "Consumida via Steps Advanced REST Client no AutomationEdge."
    });
});

var connectionString = builder.Configuration.GetConnectionString("RpaControle")
    ?? throw new InvalidOperationException("Connection string 'RpaControle' não configurada.");

builder.Services.AddDbContext<RpaDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IControleService, ControleService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Local"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Controle RPA v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    utc = DateTime.UtcNow,
    service = "ControleRpa.Api"
}));

app.Run();
