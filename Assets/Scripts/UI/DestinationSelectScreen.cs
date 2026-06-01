using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DestinationSelectScreen : MonoBehaviour
{
    AppFlowManager flow;
    CampusData campus;
    RectTransform contentRoot;
    TMP_InputField searchField;
    TMP_Dropdown buildingDropdown;
    TMP_Dropdown categoryDropdown;
    readonly List<GameObject> items = new();
    bool uiBuilt;
    RectTransform panel;

    // Categories to show as destinations (not internal points like corridors/stairs)
    static readonly RoutePointCategory[] destinationCategories = {
        RoutePointCategory.Classroom, RoutePointCategory.Auditorium,
        RoutePointCategory.Lab, RoutePointCategory.Office,
        RoutePointCategory.Library, RoutePointCategory.Cafeteria,
        RoutePointCategory.WC, RoutePointCategory.VendingMachine,
        RoutePointCategory.Entrance, RoutePointCategory.Parking,
        RoutePointCategory.Other
    };

    public void Show(AppFlowManager flowManager, CampusData campusData)
    {
        flow = flowManager;
        campus = campusData;
        if (!uiBuilt) BuildUI();
        PopulateFilters();
        RefreshList();
    }

    void BuildUI()
    {
        uiBuilt = true;
        panel = UIFactory.CreateFullscreenPanel(transform, "Background");

        UIFactory.CreateBackButton(panel, () => flow?.OnBackPressed());

        UIFactory.CreateTextAbsolute(panel, "Title", "Select Destination",
            new Vector2(0, 200), new Vector2(400, 36), 24, UIFactory.TextPrimary);

        string campusName = campus != null ? campus.campusName : "";
        UIFactory.CreateTextAbsolute(panel, "CampusLabel", campusName,
            new Vector2(0, 175), new Vector2(300, 20), 12, UIFactory.TextSecondary);

        // Search field
        searchField = UIFactory.CreateSearchField(panel, "Search",
            new Vector2(0, 145), new Vector2(340, 36), "Search room, auditorium...");
        searchField.onValueChanged.AddListener(_ => RefreshList());

        // Filters row — building dropdown
        var filterRow = new GameObject("Filters");
        filterRow.transform.SetParent(panel, false);
        var filterRect = filterRow.AddComponent<RectTransform>();
        filterRect.anchorMin = new Vector2(0.5f, 0.5f);
        filterRect.anchorMax = new Vector2(0.5f, 0.5f);
        filterRect.pivot = new Vector2(0.5f, 0.5f);
        filterRect.anchoredPosition = new Vector2(0, 110);
        filterRect.sizeDelta = new Vector2(340, 32);

        buildingDropdown = CreateDropdown(filterRow.transform, "BuildingFilter",
            new Vector2(-85, 0), new Vector2(160, 32));
        buildingDropdown.onValueChanged.AddListener(_ => RefreshList());

        categoryDropdown = CreateDropdown(filterRow.transform, "CategoryFilter",
            new Vector2(85, 0), new Vector2(160, 32));
        categoryDropdown.onValueChanged.AddListener(_ => RefreshList());

        // Scrollable list
        var (_, content) = UIFactory.CreateScrollList(panel, "DestList",
            new Vector2(0, -45), new Vector2(340, 310));
        contentRoot = content;
    }

    void PopulateFilters()
    {
        if (campus == null || campus.routeDatabase == null) return;

        // Building filter
        buildingDropdown.ClearOptions();
        var bldgOptions = new List<string> { "All Buildings" };
        bldgOptions.AddRange(campus.routeDatabase.GetAllBuildingCodes());
        buildingDropdown.AddOptions(bldgOptions);

        // Category filter
        categoryDropdown.ClearOptions();
        var catOptions = new List<string> { "All Types" };
        foreach (var cat in destinationCategories)
            catOptions.Add(FormatCategory(cat));
        categoryDropdown.AddOptions(catOptions);
    }

    void RefreshList()
    {
        foreach (var item in items)
            if (item != null) Destroy(item);
        items.Clear();

        if (campus == null || campus.routeDatabase == null) return;

        string query = searchField != null ? searchField.text : "";
        var results = campus.routeDatabase.Search(query);

        // Filter to destination categories only
        results = results.Where(p => destinationCategories.Contains(p.category)).ToList();

        // Building filter
        if (buildingDropdown != null && buildingDropdown.value > 0)
        {
            string code = buildingDropdown.options[buildingDropdown.value].text;
            results = results.Where(p => p.buildingCode == code).ToList();
        }

        // Category filter
        if (categoryDropdown != null && categoryDropdown.value > 0)
        {
            string catName = categoryDropdown.options[categoryDropdown.value].text;
            var matchCat = destinationCategories.FirstOrDefault(c => FormatCategory(c) == catName);
            results = results.Where(p => p.category == matchCat).ToList();
        }

        foreach (var point in results)
        {
            var p = point;
            Color accent = CategoryColor(point.category);
            string title = $"{point.buildingCode} - {point.pointName}";
            string subtitle = $"Floor {point.floor} | {FormatCategory(point.category)}";
            if (!string.IsNullOrEmpty(point.roomNumber))
                subtitle += $" | Room {point.roomNumber}";

            var item = UIFactory.CreateListItem(contentRoot, title, subtitle, accent,
                () => flow?.OnDestinationSelected(p));
            items.Add(item);
        }
    }

    static string FormatCategory(RoutePointCategory cat)
    {
        return cat switch
        {
            RoutePointCategory.Classroom => "Classroom",
            RoutePointCategory.Auditorium => "Auditorium",
            RoutePointCategory.Lab => "Laboratory",
            RoutePointCategory.Office => "Office",
            RoutePointCategory.Library => "Library",
            RoutePointCategory.Cafeteria => "Cafeteria",
            RoutePointCategory.WC => "WC",
            RoutePointCategory.VendingMachine => "Vending",
            RoutePointCategory.Entrance => "Entrance",
            RoutePointCategory.Parking => "Parking",
            RoutePointCategory.Elevator => "Elevator",
            _ => cat.ToString()
        };
    }

    static Color CategoryColor(RoutePointCategory cat)
    {
        return cat switch
        {
            RoutePointCategory.Classroom => UIFactory.AccentBlue,
            RoutePointCategory.Auditorium => new Color(0.6f, 0.3f, 0.9f),
            RoutePointCategory.Lab => new Color(0.2f, 0.8f, 0.7f),
            RoutePointCategory.Office => UIFactory.AccentOrange,
            RoutePointCategory.Library => new Color(0.9f, 0.8f, 0.2f),
            RoutePointCategory.Cafeteria => UIFactory.AccentOrange,
            RoutePointCategory.WC => UIFactory.AccentBlue,
            RoutePointCategory.Entrance => UIFactory.AccentGreen,
            RoutePointCategory.Parking => UIFactory.TextMuted,
            _ => UIFactory.TextSecondary
        };
    }

    TMP_Dropdown CreateDropdown(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        var img = obj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f);

        var dd = obj.AddComponent<TMP_Dropdown>();

        // Caption
        var captionObj = new GameObject("Caption");
        captionObj.transform.SetParent(obj.transform, false);
        var capRect = captionObj.AddComponent<RectTransform>();
        capRect.anchorMin = Vector2.zero;
        capRect.anchorMax = Vector2.one;
        capRect.offsetMin = new Vector2(8, 2);
        capRect.offsetMax = new Vector2(-24, -2);
        var capTmp = captionObj.AddComponent<TextMeshProUGUI>();
        capTmp.fontSize = 11;
        capTmp.color = UIFactory.TextPrimary;
        capTmp.alignment = TextAlignmentOptions.MidlineLeft;

        dd.captionText = capTmp;

        // Template
        var templateObj = new GameObject("Template");
        templateObj.transform.SetParent(obj.transform, false);
        var templateRect = templateObj.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1);
        templateRect.anchoredPosition = Vector2.zero;
        templateRect.sizeDelta = new Vector2(0, 200);

        var templateImg = templateObj.AddComponent<Image>();
        templateImg.color = new Color(0.12f, 0.12f, 0.16f);

        var templateScroll = templateObj.AddComponent<ScrollRect>();
        templateScroll.horizontal = false;

        // Viewport
        var viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(templateObj.transform, false);
        var vpRect = viewportObj.AddComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        viewportObj.AddComponent<RectMask2D>();

        templateScroll.viewport = vpRect;

        // Content
        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        var cRect = contentObj.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0, 1);
        cRect.anchorMax = new Vector2(1, 1);
        cRect.pivot = new Vector2(0.5f, 1);
        cRect.anchoredPosition = Vector2.zero;
        cRect.sizeDelta = new Vector2(0, 0);

        templateScroll.content = cRect;

        // Item
        var itemObj = new GameObject("Item");
        itemObj.transform.SetParent(contentObj.transform, false);
        var itemRect = itemObj.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);
        itemRect.sizeDelta = new Vector2(0, 28);

        var toggle = itemObj.AddComponent<Toggle>();

        var itemLabelObj = new GameObject("Item Label");
        itemLabelObj.transform.SetParent(itemObj.transform, false);
        var itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(8, 0);
        itemLabelRect.offsetMax = new Vector2(-8, 0);
        var itemTmp = itemLabelObj.AddComponent<TextMeshProUGUI>();
        itemTmp.fontSize = 12;
        itemTmp.color = UIFactory.TextPrimary;
        itemTmp.alignment = TextAlignmentOptions.MidlineLeft;

        dd.itemText = itemTmp;

        var itemBgObj = new GameObject("Item Background");
        itemBgObj.transform.SetParent(itemObj.transform, false);
        itemBgObj.transform.SetAsFirstSibling();
        var ibRect = itemBgObj.AddComponent<RectTransform>();
        ibRect.anchorMin = Vector2.zero;
        ibRect.anchorMax = Vector2.one;
        ibRect.offsetMin = Vector2.zero;
        ibRect.offsetMax = Vector2.zero;
        var ibImg = itemBgObj.AddComponent<Image>();
        ibImg.color = new Color(0.15f, 0.15f, 0.2f);

        toggle.targetGraphic = ibImg;
        toggle.graphic = ibImg;

        dd.template = templateRect;
        templateObj.SetActive(false);

        return dd;
    }
}
