using System.Net;
using System.Text.Json;
using NSwag;
using Xunit;

namespace TaskFlow.ContractTests;

/// <summary>
/// Valida que as respostas reais da API batem com os schemas declarados no
/// contrato (openapi.yaml). Usa NSwag/NJsonSchema para carregar o contrato e
/// validar o corpo contra os componentes Project, Task e ProblemDetails.
/// </summary>
public class OpenApiSchemaTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly Lazy<Task<OpenApiDocument>> Contract = new(LoadContractAsync);

    private static async Task<OpenApiDocument> LoadContractAsync()
    {
        var yaml = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "openapi.yaml"));
        return await OpenApiYamlDocument.FromYamlAsync(yaml);
    }

    private readonly HttpClient _client;

    public OpenApiSchemaTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static async Task AssertMatchesSchema(string schemaName, string json)
    {
        var document = await Contract.Value;
        var schema = document.Components.Schemas[schemaName];
        var errors = schema.Validate(json);

        Assert.True(
            errors.Count == 0,
            $"Response não conforma ao schema '{schemaName}': {string.Join("; ", errors)}");
    }

    private async Task<string> CreateProjectAsync()
    {
        var response = await _client.PostAsync("/projetos", Json.Body("""{"name":"P"}"""));
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Project_response_conforms_to_contract()
    {
        var response = await _client.PostAsync(
            "/projetos", Json.Body("""{"name":"Contrato","description":"com descricao"}"""));

        await AssertMatchesSchema("Project", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Task_response_conforms_to_contract()
    {
        var projectId = await CreateProjectAsync();

        var response = await _client.PostAsync(
            $"/projetos/{projectId}/tarefas", Json.Body("""{"title":"T","priority":"high"}"""));

        await AssertMatchesSchema("Task", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BusinessRule_error_conforms_to_ProblemDetails()
    {
        var projectId = await CreateProjectAsync();
        var taskResponse = await _client.PostAsync(
            $"/projetos/{projectId}/tarefas", Json.Body("""{"title":"T","priority":"low"}"""));
        using var taskDoc = JsonDocument.Parse(await taskResponse.Content.ReadAsStringAsync());
        var taskId = taskDoc.RootElement.GetProperty("id").GetString();
        await _client.PatchAsync($"/tarefas/{taskId}", Json.Body("""{"status":"in_progress"}"""));

        var response = await _client.PatchAsync(
            $"/projetos/{projectId}", Json.Body("""{"status":"archived"}"""));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        await AssertMatchesSchema("ProblemDetails", await response.Content.ReadAsStringAsync());
    }
}
