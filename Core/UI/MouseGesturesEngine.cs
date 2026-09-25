using System.Windows;
using System.Windows.Input;
using System.IO;

namespace Swifter.Core.UI;

public sealed class MouseGesturesEngine
{
    private static MouseGesturesEngine? _instance;
    private readonly List<GesturePoint> _points = new();
    private bool _isRecording;
    private Point _startPoint;
    private readonly Dictionary<string, Action> _gestureActions = new();
    private string _lastGesture = "";

    public static MouseGesturesEngine Instance => _instance ??= new MouseGesturesEngine();

    public event EventHandler<string>? GestureRecognized;
    public bool Enabled { get; set; } = true;
    public int MinPoints { get; set; } = 5;
    public double Threshold { get; set; } = 30;

    private MouseGesturesEngine()
    {
        RegisterDefaults();
    }

    private void RegisterDefaults()
    {
        _gestureActions["U"] = () => TabManager.Instance.CreateTab();
        _gestureActions["D"] = () => { var a = TabManager.Instance.ActiveTab; if (a != null) TabManager.Instance.CloseTab(a.Id); };
        _gestureActions["L"] = () => { };
        _gestureActions["R"] = () => { };
        _gestureActions["UR"] = () => { };
        _gestureActions["UL"] = () => { };
        _gestureActions["DR"] = () => { };
        _gestureActions["DL"] = () => { };
    }

    public void RegisterGesture(string gesture, Action action)
    {
        _gestureActions[gesture] = action;
    }

    public void StartRecording(Point screenPoint)
    {
        if (!Enabled) return;
        _isRecording = true;
        _startPoint = screenPoint;
        _points.Clear();
        _points.Add(new GesturePoint(screenPoint.X, screenPoint.Y));
    }

    public void TrackMovement(Point screenPoint)
    {
        if (!_isRecording) return;
        _points.Add(new GesturePoint(screenPoint.X, screenPoint.Y));
    }

    public string StopRecording()
    {
        if (!_isRecording) return "";
        _isRecording = false;
        if (_points.Count < MinPoints) return "";
        var direction = AnalyzeDirection();
        _lastGesture = direction;
        if (_gestureActions.TryGetValue(direction, out var action))
        {
            action();
            GestureRecognized?.Invoke(this, direction);
        }
        return direction;
    }

    public void CancelRecording()
    {
        _isRecording = false;
        _points.Clear();
    }

    private string AnalyzeDirection()
    {
        if (_points.Count < 2) return "";
        var segments = new List<string>();
        int segLen = Math.Max(1, _points.Count / 8);
        for (int i = 0; i < _points.Count - segLen; i += segLen)
        {
            var p1 = _points[i];
            var p2 = _points[Math.Min(i + segLen, _points.Count - 1)];
            var dx = p2.X - p1.X;
            var dy = p2.Y - p1.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < Threshold) continue;
            string dir;
            if (Math.Abs(dx) > Math.Abs(dy))
                dir = dx > 0 ? "R" : "L";
            else
                dir = dy > 0 ? "D" : "U";
            if (segments.Count == 0 || segments[^1] != dir)
                segments.Add(dir);
        }
        return string.Join("", segments);
    }

    public void ShowGestureOverlay()
    {
    }

    public void LoadGestures(string configPath)
    {
        if (!File.Exists(configPath)) return;
        try
        {
            var json = File.ReadAllText(configPath);
            var gestures = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (gestures != null)
            {
                foreach (var g in gestures)
                {
                    if (_gestureActions.ContainsKey(g.Key)) continue;
                }
            }
        }
        catch { }
    }

    public void SaveGestures(string configPath)
    {
        var data = _gestureActions.ToDictionary(kv => kv.Key, kv => kv.Value.Method.Name);
        var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }

    private sealed class GesturePoint
    {
        public double X { get; }
        public double Y { get; }
        public GesturePoint(double x, double y) { X = x; Y = y; }
    }
}