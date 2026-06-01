using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using TMPro;

public class NavigationScreen : MonoBehaviour
{
    AppFlowManager flow;
    bool uiBuilt;
    RectTransform panel;

    // UI elements that update
    TextMeshProUGUI destLabel;
    TextMeshProUGUI distanceLabel;
    TextMeshProUGUI etaLabel;
    TextMeshProUGUI floorLabel;
    TextMeshProUGUI modeLabel;
    TextMeshProUGUI statusLabel;
    Image progressBar;
    float totalRouteDistance;
    NavMeshAgent playerAgent;

    public void Show(AppFlowManager flowManager)
    {
        flow = flowManager;
        if (!uiBuilt) BuildUI();
        UpdateStaticInfo();

        // Cache agent reference
        if (flow.Navigator != null)
            playerAgent = flow.Navigator.Agent;
        totalRouteDistance = 0;
    }

    void BuildUI()
    {
        uiBuilt = true;

        // === TOP BAR (destination info) ===
        var topBar = new GameObject("TopBar");
        topBar.transform.SetParent(transform, false);
        var topRect = topBar.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1);
        topRect.anchoredPosition = Vector2.zero;
        topRect.sizeDelta = new Vector2(0, 80);
        var topImg = topBar.AddComponent<Image>();
        topImg.color = new Color(0.06f, 0.07f, 0.1f, 0.94f);
        topImg.raycastTarget = false;

        destLabel = CreateBarText(topBar.transform, "Dest", "",
            new Vector2(0, -8), new Vector2(-20, 24), 14, FontStyles.Bold);
        destLabel.alignment = TextAlignmentOptions.Center;
        destLabel.enableWordWrapping = false;
        destLabel.overflowMode = TextOverflowModes.Ellipsis;

        // Info row: distance | ETA | floor
        distanceLabel = CreateBarText(topBar.transform, "Distance", "...",
            new Vector2(-100, -38), new Vector2(120, 20), 13);
        distanceLabel.color = UIFactory.AccentBlue;

        etaLabel = CreateBarText(topBar.transform, "ETA", "...",
            new Vector2(0, -38), new Vector2(120, 20), 13);
        etaLabel.color = UIFactory.AccentGreen;

        floorLabel = CreateBarText(topBar.transform, "Floor", "F1",
            new Vector2(100, -38), new Vector2(80, 20), 13);
        floorLabel.color = UIFactory.AccentOrange;

        // Mode indicator
        modeLabel = CreateBarText(topBar.transform, "Mode", "",
            new Vector2(0, -58), new Vector2(200, 16), 10);
        modeLabel.color = UIFactory.TextMuted;

        // === PROGRESS BAR ===
        var progBg = new GameObject("ProgressBg");
        progBg.transform.SetParent(topBar.transform, false);
        var progBgRect = progBg.AddComponent<RectTransform>();
        progBgRect.anchorMin = new Vector2(0, 0);
        progBgRect.anchorMax = new Vector2(1, 0);
        progBgRect.pivot = new Vector2(0, 0);
        progBgRect.anchoredPosition = Vector2.zero;
        progBgRect.sizeDelta = new Vector2(0, 3);
        var progBgImg = progBg.AddComponent<Image>();
        progBgImg.color = new Color(0.2f, 0.2f, 0.25f);

        var progFill = new GameObject("ProgressFill");
        progFill.transform.SetParent(progBg.transform, false);
        var progFillRect = progFill.AddComponent<RectTransform>();
        progFillRect.anchorMin = Vector2.zero;
        progFillRect.anchorMax = new Vector2(0, 1); // will be animated
        progFillRect.offsetMin = Vector2.zero;
        progFillRect.offsetMax = Vector2.zero;
        progressBar = progFill.AddComponent<Image>();
        progressBar.color = UIFactory.AccentBlue;

        // === BOTTOM PANEL (controls) ===
        var bottomBar = new GameObject("BottomBar");
        bottomBar.transform.SetParent(transform, false);
        var botRect = bottomBar.AddComponent<RectTransform>();
        botRect.anchorMin = new Vector2(0, 0);
        botRect.anchorMax = new Vector2(1, 0);
        botRect.pivot = new Vector2(0.5f, 0);
        botRect.anchoredPosition = Vector2.zero;
        botRect.sizeDelta = new Vector2(0, 90);
        var botImg = bottomBar.AddComponent<Image>();
        botImg.color = new Color(0.06f, 0.07f, 0.1f, 0.94f);

        // Status
        statusLabel = CreateBarText(bottomBar.transform, "Status", "Navigating...",
            new Vector2(0, 55), new Vector2(-20, 20), 11);
        statusLabel.color = UIFactory.TextSecondary;

        // Stop button
        UIFactory.CreateButton(bottomBar.transform, "StopBtn", "STOP",
            new Vector2(-75, 22), new Vector2(130, 40),
            UIFactory.AccentRed, Color.white,
            () => flow?.OnBackPressed(), 15);

        // Recalculate button
        UIFactory.CreateButton(bottomBar.transform, "RecalcBtn", "RECALCULATE",
            new Vector2(75, 22), new Vector2(130, 40),
            UIFactory.AccentBlue, Color.white,
            OnRecalculate, 13);

        panel = topRect;
    }

    void UpdateStaticInfo()
    {
        if (flow == null) return;

        var dest = flow.SelectedDestination;
        if (dest != null && destLabel != null)
            destLabel.text = $"{dest.buildingCode} - {dest.pointName}";

        if (modeLabel != null)
        {
            string mode = flow.CurrentMode == AppFlowManager.NavigationMode.AR ? "AR Mode" : "Map Mode";
            string source = flow.UseLiveGps ? "Live GPS" : "Manual Start";
            modeLabel.text = $"{mode} | {source}";
        }
    }

    void Update()
    {
        if (flow == null || !gameObject.activeSelf) return;

        UpdateDistance();
        UpdateFloor();
        UpdateProgress();
        UpdateStatus();
    }

    void UpdateDistance()
    {
        if (playerAgent == null || distanceLabel == null || etaLabel == null) return;

        float remaining = 0;
        if (playerAgent.hasPath && playerAgent.path.corners.Length > 1)
        {
            var corners = playerAgent.path.corners;
            for (int i = 1; i < corners.Length; i++)
                remaining += Vector3.Distance(corners[i - 1], corners[i]);

            if (totalRouteDistance < remaining)
                totalRouteDistance = remaining;
        }
        else if (flow.SelectedDestination != null)
        {
            remaining = Vector3.Distance(playerAgent.transform.position,
                flow.SelectedDestination.worldPosition);
        }

        distanceLabel.text = remaining < 1000
            ? $"{remaining:F0}m"
            : $"{remaining / 1000f:F1}km";

        // ETA at walking speed (1.4 m/s average)
        float etaSeconds = remaining / 1.4f;
        if (etaSeconds < 60)
            etaLabel.text = $"<1 min";
        else
            etaLabel.text = $"{Mathf.CeilToInt(etaSeconds / 60f)} min";
    }

    void UpdateFloor()
    {
        if (playerAgent == null || floorLabel == null) return;
        int floor = Mathf.FloorToInt(playerAgent.transform.position.y / 3.5f) + 1;
        floorLabel.text = $"F{floor}";
    }

    void UpdateProgress()
    {
        if (progressBar == null || playerAgent == null) return;

        float progress = 0;
        if (totalRouteDistance > 0 && playerAgent.hasPath)
        {
            float remaining = playerAgent.remainingDistance;
            progress = Mathf.Clamp01(1f - remaining / totalRouteDistance);
        }

        var rt = progressBar.rectTransform;
        rt.anchorMax = new Vector2(progress, 1);
    }

    void UpdateStatus()
    {
        if (statusLabel == null || playerAgent == null) return;

        if (!playerAgent.hasPath && flow.Navigator != null && flow.Navigator.HasPath)
        {
            statusLabel.text = "Calculating path...";
            statusLabel.color = UIFactory.AccentOrange;
        }
        else if (playerAgent.remainingDistance < 2f && playerAgent.hasPath)
        {
            statusLabel.text = "You have arrived!";
            statusLabel.color = UIFactory.AccentGreen;
        }
        else if (playerAgent.hasPath)
        {
            statusLabel.text = "Navigating...";
            statusLabel.color = UIFactory.TextSecondary;
        }
        else
        {
            statusLabel.text = "No path";
            statusLabel.color = UIFactory.AccentRed;
        }
    }

    void OnRecalculate()
    {
        if (flow == null || flow.Navigator == null || flow.SelectedDestination == null) return;
        flow.Navigator.SetDestination(flow.SelectedDestination);
        totalRouteDistance = 0;
        if (statusLabel != null)
        {
            statusLabel.text = "Recalculating...";
            statusLabel.color = UIFactory.AccentOrange;
        }
    }

    TextMeshProUGUI CreateBarText(Transform parent, string name, string text,
        Vector2 position, Vector2 size, float fontSize, FontStyles style = FontStyles.Normal)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = UIFactory.TextPrimary;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }
}
