using System.Text.Json;
using System.IO;


namespace Swifter.Core.UI;

public sealed class WorkspaceManager
{
    private static WorkspaceManager? _instance;
    private readonly List<Workspace> _workspaces = new();
    private readonly string _workspacesDir;

    public static WorkspaceManager Instance => _instance ??= new WorkspaceManager();

    public IReadOnlyList<Workspace> Workspaces => _workspaces.AsReadOnly();
    public Workspace? ActiveWorkspace { get; private set; }

    public event EventHandler<Workspace>? WorkspaceActivated;

    private WorkspaceManager()
    {
        _workspacesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Swifter", "Workspaces");
        Directory.CreateDirectory(_workspacesDir);
        LoadWorkspaces();
        if (_workspaces.Count == 0)
        {
            _workspaces.Add(new Workspace { Name = "Default", Icon = "🌐", Color = "#0078D4" });
        }
    }

    private void LoadWorkspaces()
    {
        var path = Path.Combine(_workspacesDir, "workspaces.json");
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            var ws = JsonSerializer.Deserialize<List<Workspace>>(json);
            if (ws != null) _workspaces.AddRange(ws);
        }
        catch { }
    }

    public Workspace CreateWorkspace(string name, string icon = "📂", string color = "#0078D4")
    {
        var ws = new Workspace { Name = name, Icon = icon, Color = color, CreatedAt = DateTime.UtcNow };
        _workspaces.Add(ws);
        Save();
        return ws;
    }

    public void ActivateWorkspace(int index)
    {
        if (index < 0 || index >= _workspaces.Count) return;
        var currentTabs = TabEngine.TabManager.Instance.GetTabSnapshot();
        if (ActiveWorkspace != null) ActiveWorkspace.SavedTabs = currentTabs;
        ActiveWorkspace = _workspaces[index];
        TabEngine.TabManager.Instance.RestoreTabs(ActiveWorkspace.SavedTabs ?? new());
        WorkspaceActivated?.Invoke(this, ActiveWorkspace);
    }

    public void DeleteWorkspace(int index)
    {
        if (index < 0 || index >= _workspaces.Count) return;
        if (_workspaces[index] == ActiveWorkspace) return;
        _workspaces.RemoveAt(index);
        Save();
    }

    private void Save()
    {
        var path = Path.Combine(_workspacesDir, "workspaces.json");
        var json = JsonSerializer.Serialize(_workspaces, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}

public sealed class Workspace
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "🌐";
    public string Color { get; set; } = "#0078D4";
    public DateTime CreatedAt { get; set; }
    public List<TabEngine.TabData>? SavedTabs { get; set; }
}