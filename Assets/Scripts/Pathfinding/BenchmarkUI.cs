using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public class BenchmarkUI : MonoBehaviour
{
    [SerializeField] private PathfindingBenchmark benchmark;
    [SerializeField] private NavMeshAgent playerAgent;
    [SerializeField] private RouteDatabase routeDatabase;

    [Header("Destinations (set by generator)")]
    [SerializeField] private Vector3[] destinationPositions;
    [SerializeField] private string[] destinationNames;

    [Header("Path Colors")]
    [SerializeField] private Color navMeshColor = Color.cyan;
    [SerializeField] private Color aStarColor = Color.green;
    [SerializeField] private Color dijkstraColor = new(1f, 0.5f, 0f);

    int selectedDestination;
    int selectedAlgorithm;
    readonly string[] algoNames = { "NavMesh", "A*", "Dijkstra" };
    NavigationGraph graph;
    readonly List<GameObject> pathObjects = new();

    Button[] destButtons;
    Button[] algoButtons;
    TextMeshProUGUI metricsText;
    TextMeshProUGUI statusText;

    void Start()
    {
        BuildUI();
        BuildGraph();
    }

    void BuildGraph()
    {
        if (routeDatabase == null || routeDatabase.allPoints.Count == 0) return;
        graph = new NavigationGraph();
        graph.BuildFromRoutePoints(routeDatabase.allPoints, 25f);
    }

    void BuildUI()
    {
        // ===== LEFT PANEL (compact) =====
        var left = MakePanel("Controls", 220, 380);
        Anchor(left, 0, 0.5f, 0, 0.5f, 0, 0.5f);
        left.anchoredPosition = new Vector2(8, 0);

        float y = -6;
        MakeLabel(left, "BENCHMARK", y, 16, FontStyles.Bold, 200);
        y -= 26;

        // Destination buttons (small)
        int dc = destinationNames != null ? destinationNames.Length : 0;
        destButtons = new Button[dc];
        Color[] dCols = { new(0.9f, 0.3f, 0.3f), new(0.3f, 0.9f, 0.3f), new(0.3f, 0.3f, 0.9f) };
        for (int i = 0; i < dc; i++)
        {
            int idx = i;
            destButtons[i] = MakeBtn(left, destinationNames[i], y, 200, 24,
                i < dCols.Length ? dCols[i] : Color.gray, () => SelectDest(idx));
            y -= 28;
        }
        y -= 4;

        // Algorithm buttons (row of 3)
        algoButtons = new Button[3];
        Color[] aCols = { navMeshColor, aStarColor, dijkstraColor };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            algoButtons[i] = MakeBtn(left, algoNames[i], y, 200, 24, aCols[i],
                () => SelectAlgo(idx));
            y -= 28;
        }
        y -= 4;

        // Action buttons
        MakeBtn(left, "NAVIGATE", y, 200, 28, new Color(0.1f, 0.55f, 0.2f), OnNavigate);
        y -= 32;
        MakeBtn(left, "COMPARE ALL 3", y, 200, 28, new Color(0.5f, 0.3f, 0.75f), OnCompareAll);
        y -= 32;
        MakeBtn(left, "FULL BENCHMARK", y, 200, 28, new Color(0.75f, 0.4f, 0.1f), OnFullBenchmark);
        y -= 30;

        statusText = MakeLabel(left, "Ready", y, 10, FontStyles.Italic, 200);
        statusText.alignment = TextAlignmentOptions.Left;

        // ===== RIGHT PANEL (metrics) =====
        var right = MakePanel("Metrics", 300, 320);
        Anchor(right, 1, 0.5f, 1, 0.5f, 1, 0.5f);
        right.anchoredPosition = new Vector2(-8, 0);

        MakeLabel(right, "RESULTS", -6, 14, FontStyles.Bold, 280);
        metricsText = MakeLabel(right, "Press COMPARE or BENCHMARK", -26, 10, FontStyles.Normal, 280);
        metricsText.alignment = TextAlignmentOptions.TopLeft;
        metricsText.rectTransform.sizeDelta = new Vector2(280, 280);
        metricsText.enableWordWrapping = false;
        metricsText.overflowMode = TextOverflowModes.Truncate;
        metricsText.richText = true;
        metricsText.font = TMP_Settings.defaultFontAsset;
        metricsText.fontStyle = FontStyles.Normal;

        // ===== BOTTOM LEGEND =====
        var legend = MakePanel("Legend", 320, 24);
        Anchor(legend, 0.5f, 0, 0.5f, 0, 0.5f, 0);
        legend.anchoredPosition = new Vector2(0, 6);
        var lt = MakeLabel(legend, "<color=#00FFFF>\u2588 NavMesh</color>  <color=#00FF00>\u2588 A*</color>  <color=#FF8800>\u2588 Dijkstra</color>",
            -2, 12, FontStyles.Normal, 310);
        lt.richText = true;

        HighlightGroup(destButtons, 0);
        HighlightGroup(algoButtons, 0);
    }

    void SelectDest(int i) { selectedDestination = i; HighlightGroup(destButtons, i); }
    void SelectAlgo(int i) { selectedAlgorithm = i; HighlightGroup(algoButtons, i); }

    void HighlightGroup(Button[] btns, int active)
    {
        if (btns == null) return;
        for (int i = 0; i < btns.Length; i++)
        {
            if (btns[i] == null) continue;
            btns[i].GetComponent<Image>().color =
                i == active ? new Color(0.32f, 0.32f, 0.40f) : new Color(0.15f, 0.15f, 0.19f);
            var t = btns[i].GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.fontStyle = i == active ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    // ===== ACTIONS =====

    void OnNavigate()
    {
        if (!ValidateInput()) return;
        ClearPaths();

        Vector3 start = playerAgent.transform.position;
        Vector3 end = destinationPositions[selectedDestination];
        var result = RunAlgorithm(selectedAlgorithm, start, end);

        playerAgent.SetDestination(end);

        if (result.Found)
        {
            Color c = AlgoColor(selectedAlgorithm);
            DrawPath(result.Waypoints, c, 0f);
            DrawWaypoints(result.Waypoints, c, 0f);
            statusText.text = $"{result.AlgorithmName}: {result.TotalDistance:F1}m {result.ElapsedMs:F3}ms";
        }
        else
            statusText.text = "PATH NOT FOUND";
    }

    void OnCompareAll()
    {
        if (!ValidateInput()) return;
        if (graph == null) BuildGraph();
        ClearPaths();

        Vector3 start = playerAgent.transform.position;
        Vector3 end = destinationPositions[selectedDestination];

        var results = new List<PathResult>();
        for (int i = 0; i < 3; i++)
        {
            var r = RunAlgorithm(i, start, end);
            results.Add(r);
            if (r.Found)
            {
                float yOff = i * 0.5f;
                Color c = AlgoColor(i);
                DrawPath(r.Waypoints, c, yOff);
                DrawWaypoints(r.Waypoints, c, yOff);
            }
        }

        ShowMetrics(results, destinationNames[selectedDestination]);
        statusText.text = "Compared!";
    }

    void OnFullBenchmark()
    {
        if (benchmark == null) return;
        if (graph == null) BuildGraph();
        benchmark.RebuildGraph();
        benchmark.RunFullBenchmark(15);

        var log = benchmark.BenchmarkLog;
        if (log.Count == 0) return;

        float[] times = new float[3], dists = new float[3];
        int[] found = new int[3], nodes = new int[3];

        foreach (var e in log)
        {
            var rs = new[] { e.NavMeshResult, e.AStarResult, e.DijkstraResult };
            for (int i = 0; i < 3; i++)
            {
                if (!rs[i].Found) continue;
                times[i] += (float)rs[i].ElapsedMs;
                dists[i] += rs[i].TotalDistance;
                if (rs[i].NodesExplored >= 0) nodes[i] += rs[i].NodesExplored;
                found[i]++;
            }
        }

        int t = log.Count;
        metricsText.text =
            $"<b>BENCHMARK ({t} pairs)</b>\n\n" +
            FmtRow("NavMesh", found[0], t, times[0], dists[0], -1) +
            FmtRow("A*", found[1], t, times[1], dists[1], nodes[1]) +
            FmtRow("Dijkstra", found[2], t, times[2], dists[2], nodes[2]);
        statusText.text = $"Done: {t} pairs";
    }

    PathResult RunAlgorithm(int algo, Vector3 start, Vector3 end)
    {
        if (algo == 0) return new NavMeshPathfinder().FindPath(start, end);
        if (graph == null) BuildGraph();
        if (graph == null) return PathResult.Failed(algoNames[algo]);
        IPathfinder pf = algo == 1
            ? new AStarPathfinder(graph, true)
            : new DijkstraPathfinder(graph, true);
        return pf.FindPath(start, end);
    }

    bool ValidateInput()
    {
        if (playerAgent == null || destinationPositions == null ||
            selectedDestination >= destinationPositions.Length)
        {
            statusText.text = "Invalid input";
            return false;
        }
        return true;
    }

    Color AlgoColor(int i) => i switch { 0 => navMeshColor, 1 => aStarColor, _ => dijkstraColor };

    // ===== VISUALIZATION =====

    void DrawPath(List<Vector3> pts, Color color, float yOff)
    {
        if (pts.Count < 2) return;
        var obj = new GameObject("Path");
        obj.transform.SetParent(transform.root);
        pathObjects.Add(obj);

        var lr = obj.AddComponent<LineRenderer>();
        lr.positionCount = pts.Count;
        for (int i = 0; i < pts.Count; i++)
            lr.SetPosition(i, pts[i] + Vector3.up * (0.4f + yOff));

        lr.startWidth = 0.35f;
        lr.endWidth = 0.35f;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", color);
        lr.material = mat;
        lr.startColor = color;
        lr.endColor = color;
        lr.useWorldSpace = true;
    }

    void DrawWaypoints(List<Vector3> pts, Color color, float yOff)
    {
        for (int i = 0; i < pts.Count; i++)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "WP";
            sphere.transform.SetParent(transform.root);
            sphere.transform.position = pts[i] + Vector3.up * (0.4f + yOff);
            sphere.transform.localScale = Vector3.one * (i == 0 || i == pts.Count - 1 ? 0.7f : 0.35f);

            var r = sphere.GetComponent<Renderer>();
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", color);
            r.material = m;

            // Remove collider so it doesn't interfere with NavMesh/clicks
            Object.Destroy(sphere.GetComponent<Collider>());
            pathObjects.Add(sphere);
        }
    }

    void ClearPaths()
    {
        foreach (var o in pathObjects)
            if (o != null) Destroy(o);
        pathObjects.Clear();
    }

    void ShowMetrics(List<PathResult> results, string dest)
    {
        string s = $"<b>To: {dest}</b>\n\n";

        PathResult? fastest = null, shortest = null;
        foreach (var r in results)
        {
            string status = r.Found ? "<color=#88FF88>OK</color>" : "<color=#FF8888>FAIL</color>";
            s += $"<b>{r.AlgorithmName}</b> {status}\n";
            if (r.Found)
            {
                string nd = r.NodesExplored >= 0 ? $"{r.NodesExplored}" : "n/a";
                s += $"  {r.ElapsedMs:F3}ms  {r.TotalDistance:F1}m  {nd} nodes  {r.Waypoints.Count}wp\n";
                if (fastest == null || r.ElapsedMs < fastest.Value.ElapsedMs) fastest = r;
                if (shortest == null || r.TotalDistance < shortest.Value.TotalDistance) shortest = r;
            }
        }
        if (fastest != null) s += $"\n<color=#00FF00>Fastest: {fastest.Value.AlgorithmName}</color>";
        if (shortest != null) s += $"\n<color=#FFFF00>Shortest: {shortest.Value.AlgorithmName}</color>";
        metricsText.text = s;
    }

    static string FmtRow(string n, int f, int t, float time, float dist, int nodes)
    {
        string at = f > 0 ? $"{time / f:F3}" : "-";
        string ad = f > 0 ? $"{dist / f:F1}" : "-";
        string an = nodes >= 0 && f > 0 ? $"{nodes / f}" : "n/a";
        return $"<b>{n}</b> {f}/{t} {at}ms {ad}m {an}\n";
    }

    // ===== UI FACTORY =====

    RectTransform MakePanel(string name, float w, float h)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(transform, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(w, h);
        var img = obj.AddComponent<Image>();
        img.color = new Color(0.06f, 0.06f, 0.09f, 0.92f);
        return rect;
    }

    void Anchor(RectTransform r, float amx, float amy, float aMx, float aMy, float px, float py)
    {
        r.anchorMin = new Vector2(amx, amy);
        r.anchorMax = new Vector2(aMx, aMy);
        r.pivot = new Vector2(px, py);
    }

    TextMeshProUGUI MakeLabel(RectTransform parent, string text, float yPos, float size,
        FontStyles style, float width)
    {
        var obj = new GameObject("L");
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, yPos);
        rect.sizeDelta = new Vector2(-10, 18);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    Button MakeBtn(RectTransform parent, string label, float yPos, float w, float h,
        Color accent, UnityEngine.Events.UnityAction click)
    {
        var obj = new GameObject(label);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, yPos);
        rect.sizeDelta = new Vector2(w, h);

        var img = obj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.19f);
        img.raycastTarget = true;

        var btn = obj.AddComponent<Button>();
        var c = btn.colors;
        c.normalColor = new Color(0.20f, 0.20f, 0.25f);
        c.highlightedColor = accent * 0.6f;
        c.pressedColor = accent;
        c.fadeDuration = 0.05f;
        btn.colors = c;
        btn.onClick.AddListener(click);

        var lo = new GameObject("T");
        lo.transform.SetParent(obj.transform, false);
        var lr = lo.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(4, 1);
        lr.offsetMax = new Vector2(-4, -1);
        var t = lo.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = 11;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        return btn;
    }
}
