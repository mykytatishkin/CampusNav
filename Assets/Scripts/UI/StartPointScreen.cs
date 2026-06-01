using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartPointScreen : MonoBehaviour
{
    AppFlowManager flow;
    CampusData campus;
    RectTransform contentRoot;
    TMP_InputField searchField;
    readonly List<GameObject> items = new();
    bool uiBuilt;
    RectTransform panel;
    TextMeshProUGUI destInfoText;

    // Categories suitable as start points
    static readonly RoutePointCategory[] startCategories = {
        RoutePointCategory.Entrance, RoutePointCategory.Classroom,
        RoutePointCategory.Auditorium, RoutePointCategory.Lab,
        RoutePointCategory.Office, RoutePointCategory.Cafeteria,
        RoutePointCategory.Library, RoutePointCategory.WC,
        RoutePointCategory.Parking
    };

    public void Show(AppFlowManager flowManager, CampusData campusData)
    {
        flow = flowManager;
        campus = campusData;
        if (!uiBuilt) BuildUI();
        UpdateDestInfo();
        RefreshList();
    }

    void BuildUI()
    {
        uiBuilt = true;
        panel = UIFactory.CreateFullscreenPanel(transform, "Background");

        UIFactory.CreateBackButton(panel, () => flow?.OnBackPressed());

        UIFactory.CreateTextAbsolute(panel, "Title", "Where are you?",
            new Vector2(0, 200), new Vector2(400, 36), 24, UIFactory.TextPrimary);

        // Destination info
        destInfoText = UIFactory.CreateTextAbsolute(panel, "DestInfo", "",
            new Vector2(0, 172), new Vector2(340, 20), 12, UIFactory.AccentBlue);

        // LIVE GPS button — prominent at top
        var liveBtn = UIFactory.CreateButton(panel, "LiveGPS", "LIVE GPS",
            new Vector2(0, 135), new Vector2(340, 50),
            UIFactory.AccentGreen, Color.white,
            OnLivePressed, 18);

        // GPS availability indicator
        var gpsStatus = UIFactory.CreateTextAbsolute(panel, "GpsStatus", "",
            new Vector2(0, 103), new Vector2(340, 18), 10, UIFactory.TextMuted);
        UpdateGpsStatus(gpsStatus);

        // Separator
        UIFactory.CreateTextAbsolute(panel, "OrLabel", "-- or select start point --",
            new Vector2(0, 82), new Vector2(340, 20), 11, UIFactory.TextMuted);

        // Search
        searchField = UIFactory.CreateSearchField(panel, "Search",
            new Vector2(0, 55), new Vector2(340, 32), "Search start location...");
        searchField.onValueChanged.AddListener(_ => RefreshList());

        // Scrollable list
        var (_, content) = UIFactory.CreateScrollList(panel, "StartList",
            new Vector2(0, -65), new Vector2(340, 260));
        contentRoot = content;
    }

    void UpdateDestInfo()
    {
        if (destInfoText == null || flow == null) return;
        var dest = flow.SelectedDestination;
        destInfoText.text = dest != null
            ? $"Destination: {dest.buildingCode} - {dest.pointName} (F{dest.floor})"
            : "";
    }

    void UpdateGpsStatus(TextMeshProUGUI label)
    {
        if (label == null || flow == null) return;

        var geo = flow.GeoAnchor;
        if (geo == null || !geo.IsLocationReady)
        {
            label.text = "GPS not available - will use approximate position";
            label.color = UIFactory.AccentOrange;
        }
        else if (flow.IsGpsNearUniversity())
        {
            label.text = $"GPS ready - accuracy {geo.CurrentAccuracy:F0}m - on campus";
            label.color = UIFactory.AccentGreen;
        }
        else
        {
            label.text = $"GPS ready - accuracy {geo.CurrentAccuracy:F0}m - not on campus";
            label.color = UIFactory.AccentOrange;
        }
    }

    void OnLivePressed()
    {
        flow?.OnLiveGpsSelected();
    }

    void RefreshList()
    {
        foreach (var item in items)
            if (item != null) Destroy(item);
        items.Clear();

        if (campus == null || campus.routeDatabase == null) return;

        string query = searchField != null ? searchField.text : "";
        var results = campus.routeDatabase.Search(query);

        // Filter to start-suitable categories
        results = results.Where(p => startCategories.Contains(p.category)).ToList();

        // Sort: entrances first, then by building + floor
        results = results
            .OrderByDescending(p => p.category == RoutePointCategory.Entrance)
            .ThenBy(p => p.buildingCode)
            .ThenBy(p => p.floor)
            .ThenBy(p => p.pointName)
            .ToList();

        foreach (var point in results)
        {
            var p = point;
            Color accent = point.category == RoutePointCategory.Entrance
                ? UIFactory.AccentGreen
                : UIFactory.AccentBlue;

            string title = $"{point.buildingCode} - {point.pointName}";
            string subtitle = $"Floor {point.floor} | {point.category}";

            var item = UIFactory.CreateListItem(contentRoot, title, subtitle, accent,
                () => flow?.OnStartPointSelected(p));
            items.Add(item);
        }
    }
}
