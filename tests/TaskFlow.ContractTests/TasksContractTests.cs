using System.Net;
using System.Text.Json;
using Xunit;

namespace TaskFlow.ContractTests;

/// <summary>
/// Testes de contrato dos endpoints de tarefas: criação, regras 2–5, filtros e
/// recursos inexistentes (404).
/// </summary>
public class TasksContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TasksContractTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> CreateProjectAsync()
    {
        var response = await _client.PostAsync("/projetos", Json.Body("""{"name":"Projeto"}"""));
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

    private Task<HttpResponseMessage> AdvanceAsync(string taskId, string status) =>
        _client.PatchAsync($"/tarefas/{taskId}", Json.Body($$"""{"status":"{{status}}"}"""));

    [Fact]
    public async Task Create_returns_201_pending_with_string_enums()
    {
        var projectId = await CreateProjectAsync();

        var response = await _client.PostAsync(
            $"/projetos/{projectId}/tarefas", Json.Body("""{"title":"Config","priority":"high"}"""));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal("pending", root.GetProperty("status").GetString());
        Assert.Equal("high", root.GetProperty("priority").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("completedAt").ValueKind);
    }

    [Fact]
    public async Task Create_without_priority_returns_400()
    {
        var projectId = await CreateProjectAsync();
        var response = await _client.PostAsync(
            $"/projetos/{projectId}/tarefas", Json.Body("""{"title":"Sem prioridade"}"""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_in_nonexistent_project_returns_404()
    {
        var response = await _client.PostAsync(
            $"/projetos/{Guid.NewGuid()}/tarefas", Json.Body("""{"title":"T","priority":"low"}"""));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_blank_title_returns_400()
    {
        var projectId = await CreateProjectAsync();
        var response = await _client.PostAsync(
            $"/projetos/{projectId}/tarefas", Json.Body("""{"title":"   ","priority":"low"}"""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_numeric_priority_returns_400()
    {
        var projectId = await CreateProjectAsync();
        var response = await _client.PostAsync(
            $"/projetos/{projectId}/tarefas", Json.Body("""{"title":"T","priority":1}"""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_out_of_range_priority_returns_400()
    {
        var projectId = await CreateProjectAsync();
        var response = await _client.PostAsync(
            $"/projetos/{projectId}/tarefas", Json.Body("""{"title":"T","priority":99}"""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patch_with_numeric_status_returns_400()
    {
        var projectId = await CreateProjectAsync();
        var taskId = await CreateTaskAsync(projectId);
        var response = await _client.PatchAsync($"/tarefas/{taskId}", Json.Body("""{"status":2}"""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_with_numeric_priority_filter_returns_400()
    {
        var projectId = await CreateProjectAsync();
        var response = await _client.GetAsync($"/projetos/{projectId}/tarefas?priority=99");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_title_too_long_returns_400()
    {
        var projectId = await CreateProjectAsync();
        var longTitle = new string('a', 201); // limite é 200
        var response = await _client.PostAsync(
            $"/projetos/{projectId}/tarefas", Json.Body($$"""{"title":"{{longTitle}}","priority":"low"}"""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_invalid_priority_string_returns_400()
    {
        var projectId = await CreateProjectAsync();
        var response = await _client.PostAsync(
            $"/projetos/{projectId}/tarefas", Json.Body("""{"title":"T","priority":"urgent"}"""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_wrong_type_title_returns_400()
    {
        var projectId = await CreateProjectAsync();
        var response = await _client.PostAsync(
            $"/projetos/{projectId}/tarefas", Json.Body("""{"title":123,"priority":"low"}"""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_over_posted_completedAt_returns_400()
    {
        // completedAt é read-only (D4); enviá-lo na criação é campo desconhecido.
        var projectId = await CreateProjectAsync();
        var response = await _client.PostAsync(
            $"/projetos/{projectId}/tarefas",
            Json.Body("""{"title":"T","priority":"low","completedAt":"2020-01-01T00:00:00Z"}"""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patch_with_over_posted_completedAt_returns_400()
    {
        var projectId = await CreateProjectAsync();
        var taskId = await CreateTaskAsync(projectId);
        var response = await _client.PatchAsync(
            $"/tarefas/{taskId}", Json.Body("""{"completedAt":"2020-01-01T00:00:00Z"}"""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patch_with_blank_title_returns_400()
    {
        var projectId = await CreateProjectAsync();
        var taskId = await CreateTaskAsync(projectId);
        var response = await _client.PatchAsync($"/tarefas/{taskId}", Json.Body("""{"title":"   "}"""));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_of_nonexistent_project_returns_404()
    {
        var response = await _client.GetAsync($"/projetos/{Guid.NewGuid()}/tarefas");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_invalid_status_enum_returns_400()
    {
        var projectId = await CreateProjectAsync();
        var taskId = await CreateTaskAsync(projectId);

        var response = await _client.PatchAsync($"/tarefas/{taskId}", Json.Body("""{"status":"bogus"}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patch_repeating_current_status_is_noop_returns_200()
    {
        var projectId = await CreateProjectAsync();
        var taskId = await CreateTaskAsync(projectId);

        var response = await AdvanceAsync(taskId, "pending"); // pending -> pending (no-op, D1)

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("pending", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("completedAt").ValueKind);
    }

    [Fact]
    public async Task Create_in_archived_project_returns_422()
    {
        var projectId = await CreateProjectAsync();
        await _client.PatchAsync($"/projetos/{projectId}", Json.Body("""{"status":"archived"}"""));

        var response = await _client.PostAsync(
            $"/projetos/{projectId}/tarefas", Json.Body("""{"title":"T","priority":"low"}"""));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Delete_pending_task_returns_204()
    {
        var projectId = await CreateProjectAsync();
        var taskId = await CreateTaskAsync(projectId);

        var response = await _client.DeleteAsync($"/tarefas/{taskId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_in_progress_task_returns_422()
    {
        var projectId = await CreateProjectAsync();
        var taskId = await CreateTaskAsync(projectId);
        await AdvanceAsync(taskId, "in_progress");

        var response = await _client.DeleteAsync($"/tarefas/{taskId}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Delete_nonexistent_task_returns_404()
    {
        var response = await _client.DeleteAsync($"/tarefas/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_skipping_status_returns_422()
    {
        var projectId = await CreateProjectAsync();
        var taskId = await CreateTaskAsync(projectId);

        var response = await AdvanceAsync(taskId, "done"); // pending -> done (pula in_progress)

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(doc.RootElement.GetProperty("detail").GetString()));
    }

    [Fact]
    public async Task Patch_reverting_status_returns_422()
    {
        var projectId = await CreateProjectAsync();
        var taskId = await CreateTaskAsync(projectId);
        await AdvanceAsync(taskId, "in_progress");

        var response = await AdvanceAsync(taskId, "pending"); // retrocede

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Completing_task_sets_completedAt()
    {
        var projectId = await CreateProjectAsync();
        var taskId = await CreateTaskAsync(projectId);
        await AdvanceAsync(taskId, "in_progress");

        var response = await AdvanceAsync(taskId, "done");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var completedAt = doc.RootElement.GetProperty("completedAt");
        Assert.Equal(JsonValueKind.String, completedAt.ValueKind);
        Assert.EndsWith("Z", completedAt.GetString());
    }

    [Fact]
    public async Task Patch_with_unknown_field_returns_400()
    {
        var projectId = await CreateProjectAsync();
        var taskId = await CreateTaskAsync(projectId);

        var response = await _client.PatchAsync($"/tarefas/{taskId}", Json.Body("""{"foo":"bar"}"""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patch_nonexistent_task_returns_404()
    {
        var response = await _client.PatchAsync(
            $"/tarefas/{Guid.NewGuid()}", Json.Body("""{"title":"x"}"""));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_filters_by_status()
    {
        var projectId = await CreateProjectAsync();
        var pendingTask = await CreateTaskAsync(projectId);
        var inProgressTask = await CreateTaskAsync(projectId);
        await AdvanceAsync(inProgressTask, "in_progress");

        var response = await _client.GetAsync($"/projetos/{projectId}/tarefas?status=in_progress");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = doc.RootElement.EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal(inProgressTask, items[0].GetProperty("id").GetString());
    }

    [Fact]
    public async Task List_with_invalid_filter_returns_400()
    {
        var projectId = await CreateProjectAsync();
        var response = await _client.GetAsync($"/projetos/{projectId}/tarefas?status=bogus");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
