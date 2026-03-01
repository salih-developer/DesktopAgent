using DesktopAgent.Services.Tools;

namespace DesktopAgent.Tests;

public class BasicToolsTests
{
    [Fact]
    public async Task ReadWriteFileTools_WorkOnRelativePath()
    {
        using var scope = new WorkspaceScope();
        var write = new WriteFileTool();
        var read = new ReadFileTool();

        var writeResult = await write.RunAsync(
            TestInfrastructure.Args(new { path = @"src\personel.txt", content = "personel-icerik" }),
            CancellationToken.None);

        var readResult = await read.RunAsync(
            TestInfrastructure.Args(new { path = @"src\personel.txt" }),
            CancellationToken.None);

        Assert.True(writeResult.Success);
        Assert.True(readResult.Success);
        Assert.Equal("personel-icerik", readResult.Output);
        Assert.True(File.Exists(Path.Combine(scope.RootPath, @"src\personel.txt")));
    }

    [Fact]
    public async Task ReadFileTool_ReturnsError_WhenFileMissing()
    {
        using var scope = new WorkspaceScope();
        var read = new ReadFileTool();

        var result = await read.RunAsync(
            TestInfrastructure.Args(new { path = "missing.txt" }),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("File not found", result.Error);
    }

    [Fact]
    public async Task EditFileTool_ReplacesText()
    {
        using var scope = new WorkspaceScope();
        var write = new WriteFileTool();
        var edit = new EditFileTool();
        var read = new ReadFileTool();

        await write.RunAsync(
            TestInfrastructure.Args(new { path = "app.txt", content = "hello personel" }),
            CancellationToken.None);

        var editResult = await edit.RunAsync(
            TestInfrastructure.Args(new { path = "app.txt", search = "personel", replace = "personel-proje" }),
            CancellationToken.None);

        var readResult = await read.RunAsync(
            TestInfrastructure.Args(new { path = "app.txt" }),
            CancellationToken.None);

        Assert.True(editResult.Success);
        Assert.Equal("Modified", editResult.Output);
        Assert.Equal("hello personel-proje", readResult.Output);
    }

    [Fact]
    public async Task EditFileTool_ReturnsError_WhenSearchNotFound()
    {
        using var scope = new WorkspaceScope();
        var write = new WriteFileTool();
        var edit = new EditFileTool();

        await write.RunAsync(
            TestInfrastructure.Args(new { path = "app.txt", content = "hello world" }),
            CancellationToken.None);

        var result = await edit.RunAsync(
            TestInfrastructure.Args(new { path = "app.txt", search = "missing", replace = "x" }),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Text not found", result.Error);
    }

    [Fact]
    public async Task CreateDirectoryTool_CreatesDirectory()
    {
        using var scope = new WorkspaceScope();
        var tool = new CreateDirectoryTool();

        var result = await tool.RunAsync(
            TestInfrastructure.Args(new { path = @"backend\api" }),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Created", result.Output);
        Assert.True(Directory.Exists(Path.Combine(scope.RootPath, @"backend\api")));
    }

    [Fact]
    public async Task ListFilesTool_ReturnsEmptyDirectoryMarker()
    {
        using var scope = new WorkspaceScope();
        var tool = new ListFilesTool();

        var result = await tool.RunAsync(
            TestInfrastructure.Args(new { path = "." }),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("(empty directory:", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListFilesTool_ReturnsEntriesWhenRecursive()
    {
        using var scope = new WorkspaceScope();
        Directory.CreateDirectory(Path.Combine(scope.RootPath, "nested"));
        await File.WriteAllTextAsync(Path.Combine(scope.RootPath, "root.txt"), "r");
        await File.WriteAllTextAsync(Path.Combine(scope.RootPath, @"nested\child.txt"), "c");

        var tool = new ListFilesTool();
        var result = await tool.RunAsync(
            TestInfrastructure.Args(new { path = ".", recursive = true }),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("root.txt", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("child.txt", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListFilesTool_ReturnsError_WhenDirectoryMissing()
    {
        using var scope = new WorkspaceScope();
        var tool = new ListFilesTool();

        var result = await tool.RunAsync(
            TestInfrastructure.Args(new { path = "does-not-exist" }),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Directory not found:", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchInFilesTool_FindsMatchingFile()
    {
        using var scope = new WorkspaceScope();
        await File.WriteAllTextAsync(Path.Combine(scope.RootPath, "a.txt"), "personel kaydi");
        await File.WriteAllTextAsync(Path.Combine(scope.RootPath, "b.txt"), "farkli icerik");

        var tool = new SearchInFilesTool();
        var result = await tool.RunAsync(
            TestInfrastructure.Args(new { query = "personel", include = "*.txt" }),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("a.txt", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("b.txt", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchInFilesTool_ReturnsNoMatchesMarker()
    {
        using var scope = new WorkspaceScope();
        await File.WriteAllTextAsync(Path.Combine(scope.RootPath, "a.txt"), "personel kaydi");

        var tool = new SearchInFilesTool();
        var result = await tool.RunAsync(
            TestInfrastructure.Args(new { query = "olmayan", include = "*.txt" }),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("(no matches)", result.Output);
    }

    [Fact]
    public async Task SearchInFilesTool_ReturnsError_WhenQueryEmpty()
    {
        using var scope = new WorkspaceScope();
        var tool = new SearchInFilesTool();

        var result = await tool.RunAsync(
            TestInfrastructure.Args(new { query = "", include = "*.txt" }),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Query cannot be empty.", result.Error);
    }

    [Fact]
    public async Task RunTerminalTool_ExecutesCommand()
    {
        using var scope = new WorkspaceScope();
        var tool = new RunTerminalTool();

        var result = await tool.RunAsync(
            TestInfrastructure.Args(new { command = "echo personel-test" }),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("personel-test", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunTerminalTool_ReturnsError_WhenCommandEmpty()
    {
        using var scope = new WorkspaceScope();
        var tool = new RunTerminalTool();

        var result = await tool.RunAsync(
            TestInfrastructure.Args(new { command = "" }),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Command cannot be empty.", result.Error);
    }

    [Fact]
    public async Task RunTerminalTool_FallsBackToWorkspace_WhenCwdMissing()
    {
        using var scope = new WorkspaceScope();
        var tool = new RunTerminalTool();

        var result = await tool.RunAsync(
            TestInfrastructure.Args(new { command = "cd", cwd = "missing-folder" }),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(scope.RootPath, result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StubTools_ReturnExpectedMessages()
    {
        using var scope = new WorkspaceScope();
        var diagnostics = new GetDiagnosticsTool();
        var openFile = new GetOpenFileInfoTool();
        var insertCode = new InsertCodeTool();

        var emptyArgs = TestInfrastructure.Args(new { });

        var diagnosticsResult = await diagnostics.RunAsync(emptyArgs, CancellationToken.None);
        var openFileResult = await openFile.RunAsync(emptyArgs, CancellationToken.None);
        var insertCodeResult = await insertCode.RunAsync(emptyArgs, CancellationToken.None);

        Assert.True(diagnosticsResult.Success);
        Assert.True(openFileResult.Success);
        Assert.True(insertCodeResult.Success);
        Assert.Contains("not available", diagnosticsResult.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No active editor information", openFileResult.Output, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not supported", insertCodeResult.Output, StringComparison.OrdinalIgnoreCase);
    }
}
