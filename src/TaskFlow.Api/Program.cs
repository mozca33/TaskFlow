using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Binding;
using TaskFlow.Api.Errors;
using TaskFlow.Api.Persistence;
using TaskFlow.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
    {
        // Aceita enums em snake_case na query string (?status=in_progress).
        options.ModelBinderProviders.Insert(0, new WireEnumModelBinderProvider());
    })
    .AddJsonOptions(options =>
    {
        // Enums no wire como string minúscula/snake_case: active, in_progress, low… (D9).
        // allowIntegerValues: false rejeita enum numérico (ex.: priority:1/99) com 400,
        // impedindo entrada e persistência de valores fora do domínio do contrato (D17).
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false));
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<TaskFlowDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("TaskFlow")));

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();

// Erros de domínio → ProblemDetails (RFC 7807) num ponto central. O traceId é
// injetado em TODA resposta de erro (400, 404, 422), para correlação de logs
// uniforme — sem isso, só o 400 automático do framework o traria (D16).
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

// Aplica migrations pendentes na inicialização (SQLite local).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Expõe a classe Program (top-level statements) para os testes de contrato
// via WebApplicationFactory<Program>.
public partial class Program { }
