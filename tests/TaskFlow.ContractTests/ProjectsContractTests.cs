using System.Net;
using System.Text.Json;
using Xunit;

namespace TaskFlow.ContractTests;

/// <summary>
/// Testes de contrato dos endpoints de projetos: criação, listagem, busca,
/// atualização, regra 1 (arquivamento) e recursos inexistentes (404).
/// </summary>
public class ProjectsContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProjectsContractTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> CreateProjectAsync(string name = "Projeto")
    {
        var response = await _client.PostAsync("/projetos", Json.Body($$"""{"name":"{{name}}"}"""));
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    private async Task<string> CreateTaskAsync(string projectId, string priority = "low")
    {
        var response = await _client.PostAsync(
            $"/projetos/{projectId}/tarefas", Json.Body($$"""{"title":"T","priority":"{{priority}}"}"""));
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task Create_returns_201_with_location_and_active_status()
    {
        var response = await _client.PostAsync("/projetos", Json.Body("""{"name":"Novo projeto"}"""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal("active", root.GetProperty("status").GetString());
        Assert.Equal("Novo projeto", root.GetProperty("name").GetString());
        Assert.EndsWith("Z", root.GetProperty("createdAt").GetString()); // timestamp em UTC
    }

    [Fact]
    public async Task Create_without_name_returns_400()
    {
        var response = await _client.PostAsync("/projetos", Json.Body("""{"description":"sem nome"}"""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_unknown_field_returns_400()
    {
        var response = await _client.PostAsync("/projetos", Json.Body("""{"name":"x","foo":"bar"}"""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_id_returns_200()
    {
        var id = await CreateProjectAsync();
        var response = await _client.GetAsync($"/projetos/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(id, doc.RootElement.GetProperty("id").GetString());
    }

    [Fact]
    public async Task Get_nonexistent_returns_404()
    {
        var response = await _client.GetAsync($"/projetos/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_with_malformed_id_returns_404()
    {
        var response = await _client.GetAsync("/projetos/nao-e-guid");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_returns_200_json_array()
    {
        await CreateProjectAsync();
        var response = await _client.GetAsync("/projetos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task Patch_name_returns_200()
    {
        var id = await CreateProjectAsync();
        var response = await _client.PatchAsync($"/projetos/{id}", Json.Body("""{"name":"Renomeado"}"""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Renomeado", doc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Patch_empty_body_returns_400()
    {
        var id = await CreateProjectAsync();
        var response = await _client.PatchAsync($"/projetos/{id}", Json.Body("{}"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Archive_with_in_progress_task_returns_422()
    {
        var projectId = await CreateProjectAsync();
        var taskId = await CreateTaskAsync(projectId);
        await _client.PatchAsync($"/tarefas/{taskId}", Json.Body("""{"status":"in_progress"}"""));

        var response = await _client.PatchAsync($"/projetos/{projectId}", Json.Body("""{"status":"archived"}"""));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("detail").GetString()));
    }

    [Fact]
    public async Task Archive_without_in_progress_returns_200()
    {
        var projectId = await CreateProjectAsync();

        var response = await _client.PatchAsync($"/projetos/{projectId}", Json.Body("""{"status":"archived"}"""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("archived", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Unarchive_is_allowed()
    {
        var projectId = await CreateProjectAsync();
        await _client.PatchAsync($"/projetos/{projectId}", Json.Body("""{"status":"archived"}"""));

        var response = await _client.PatchAsync($"/projetos/{projectId}", Json.Body("""{"status":"active"}"""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("active", doc.RootElement.GetProperty("status").GetString());
    }
}
