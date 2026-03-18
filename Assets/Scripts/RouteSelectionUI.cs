using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RouteSelectionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CampusNavigator navigator;
    [SerializeField] private RouteDatabase routeDatabase;

    [Header("UI Elements")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_InputField searchField;
    [SerializeField] private TMP_Dropdown buildingFilter;
    [SerializeField] private TMP_Dropdown categoryFilter;
    [SerializeField] private Transform listContent;
    [SerializeField] private GameObject routeButtonPrefab;
    [SerializeField] private Button togglePanelButton;
    [SerializeField] private Button clearRouteButton;
    [SerializeField] private TextMeshProUGUI currentRouteLabel;

    readonly List<GameObject> spawnedButtons = new();
    bool panelOpen;

    void Start()
    {
        if (togglePanelButton != null)
            togglePanelButton.onClick.AddListener(TogglePanel);
        if (clearRouteButton != null)
            clearRouteButton.onClick.AddListener(OnClearRoute);
        if (searchField != null)
            searchField.onValueChanged.AddListener(_ => RefreshList());
        if (buildingFilter != null)
        {
            PopulateBuildingFilter();
            buildingFilter.onValueChanged.AddListener(_ => RefreshList());
        }
        if (categoryFilter != null)
        {
            PopulateCategoryFilter();
            categoryFilter.onValueChanged.AddListener(_ => RefreshList());
        }

        if (panelRoot != null) panelRoot.SetActive(false);
        UpdateCurrentRouteLabel();
    }

    void TogglePanel()
    {
        panelOpen = !panelOpen;
        if (panelRoot != null) panelRoot.SetActive(panelOpen);
        if (panelOpen) RefreshList();
    }

    void PopulateBuildingFilter()
    {
        buildingFilter.ClearOptions();
        var options = new List<string> { "All Buildings" };
        if (routeDatabase != null)
            options.AddRange(routeDatabase.GetAllBuildingCodes());
        buildingFilter.AddOptions(options);
    }

    void PopulateCategoryFilter()
    {
        categoryFilter.ClearOptions();
        var options = new List<string> { "All Categories" };
        foreach (var cat in System.Enum.GetNames(typeof(RoutePointCategory)))
            options.Add(cat);
        categoryFilter.AddOptions(options);
    }

    void RefreshList()
    {
        foreach (var btn in spawnedButtons)
            Destroy(btn);
        spawnedButtons.Clear();

        if (routeDatabase == null || listContent == null || routeButtonPrefab == null) return;

        string query = searchField != null ? searchField.text : "";
        List<RoutePoint> results = routeDatabase.Search(query);

        if (buildingFilter != null && buildingFilter.value > 0)
        {
            string code = buildingFilter.options[buildingFilter.value].text;
            results = results.FindAll(p => p.buildingCode == code);
        }

        if (categoryFilter != null && categoryFilter.value > 0)
        {
            string catName = categoryFilter.options[categoryFilter.value].text;
            if (System.Enum.TryParse<RoutePointCategory>(catName, out var cat))
                results = results.FindAll(p => p.category == cat);
        }

        foreach (var point in results)
        {
            var btnObj = Instantiate(routeButtonPrefab, listContent);
            btnObj.SetActive(true);

            var label = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = $"{point.buildingCode} - {point.pointName} (F{point.floor})";

            var btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                var captured = point;
                btn.onClick.AddListener(() => OnSelectRoute(captured));
            }

            spawnedButtons.Add(btnObj);
        }
    }

    void OnSelectRoute(RoutePoint point)
    {
        if (navigator != null)
            navigator.SetDestination(point);

        UpdateCurrentRouteLabel();
        panelOpen = false;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    void OnClearRoute()
    {
        if (navigator != null)
            navigator.ClearPath();
        UpdateCurrentRouteLabel();
    }

    void UpdateCurrentRouteLabel()
    {
        if (currentRouteLabel == null) return;
        var dest = navigator != null ? navigator.CurrentDestination : null;
        currentRouteLabel.text = dest != null
            ? $"Route: {dest.buildingCode} - {dest.pointName}"
            : "No route selected";
    }
}
