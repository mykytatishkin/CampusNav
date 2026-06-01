using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Indoor selection screen. Shows floors and rooms for a building.
/// When a room is selected, the panel hides and a small "Menu" button appears.
/// Clicking "Menu" re-opens the panel.
/// </summary>
public class FloorSelectUI : MonoBehaviour
{
    AppFlowManager flow;
    IndoorManager indoorManager;
    IndoorBuildingData buildingData;
    int selectedFloor = 1;

    bool uiBuilt;
    GameObject panelRoot;      // the fullscreen panel with all controls
    GameObject menuButton;     // small toggle button visible when panel is hidden
    RectTransform panel;
    TextMeshProUGUI titleText;
    TextMeshProUGUI floorLabel;
    RectTransform floorButtonsParent;
    RectTransform contentRoot;
    readonly List<GameObject> roomItems = new();
    readonly List<GameObject> floorBtnObjects = new();

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Show(AppFlowManager flowManager, CampusData campusData, string buildingCode)
    {
        flow = flowManager;
        indoorManager = FindAnyObjectByType<IndoorManager>();

        if (indoorManager == null) { Debug.LogError("[FloorSelectUI] No IndoorManager found!"); return; }

        buildingData = indoorManager.GetBuildingData(buildingCode);
        if (buildingData == null) { Debug.LogError($"[FloorSelectUI] No indoor data for {buildingCode}"); return; }

        selectedFloor = 1;

        if (!uiBuilt) BuildUI();

        // Show panel, hide menu button
        panelRoot.SetActive(true);
        menuButton.SetActive(false);

        UpdateTitle();
        RebuildFloorButtons();
        RefreshRoomList();
    }

    void BuildUI()
    {
        uiBuilt = true;

        // ===== MAIN PANEL (right side, not fullscreen) =====
        panelRoot = new GameObject("PanelRoot");
        panelRoot.transform.SetParent(transform, false);
        var prRect = panelRoot.AddComponent<RectTransform>();
        prRect.anchorMin = new Vector2(0.65f, 0);
        prRect.anchorMax = new Vector2(1, 1);
        prRect.offsetMin = Vector2.zero;
        prRect.offsetMax = Vector2.zero;
        var prImg = panelRoot.AddComponent<Image>();
        prImg.color = UIFactory.BgDark;
        prImg.raycastTarget = true;
        panel = prRect;

        // Back / Exit building button
        UIFactory.CreateBackButton(panel, OnExitBuilding);

        // Title
        titleText = UIFactory.CreateTextAbsolute(panel, "Title", "",
            new Vector2(0, 200), new Vector2(300, 36), 20, UIFactory.TextPrimary);

        // Floor label
        floorLabel = UIFactory.CreateTextAbsolute(panel, "FloorLabel", "Floor 1",
            new Vector2(0, 175), new Vector2(280, 22), 13, UIFactory.AccentBlue);

        // Floor buttons container
        var floorRow = new GameObject("FloorButtons");
        floorRow.transform.SetParent(panel, false);
        var floorRowRect = floorRow.AddComponent<RectTransform>();
        floorRowRect.anchorMin = new Vector2(0.5f, 0.5f);
        floorRowRect.anchorMax = new Vector2(0.5f, 0.5f);
        floorRowRect.pivot = new Vector2(0.5f, 0.5f);
        floorRowRect.anchoredPosition = new Vector2(0, 145);
        floorRowRect.sizeDelta = new Vector2(300, 38);
        floorButtonsParent = floorRowRect;

        // Room list
        var (_, content) = UIFactory.CreateScrollList(panel, "RoomList",
            new Vector2(0, -20), new Vector2(300, 320));
        contentRoot = content;

        // Exit button at bottom
        UIFactory.CreateButton(panel, "ExitBtn", "EXIT BUILDING",
            new Vector2(0, -210), new Vector2(260, 42),
            UIFactory.AccentRed, Color.white,
            OnExitBuilding, 15);

        // ===== SMALL MENU BUTTON (visible when panel is hidden) =====
        menuButton = new GameObject("MenuButton");
        menuButton.transform.SetParent(transform, false);
        var mbRect = menuButton.AddComponent<RectTransform>();
        mbRect.anchorMin = new Vector2(1, 0.5f);
        mbRect.anchorMax = new Vector2(1, 0.5f);
        mbRect.pivot = new Vector2(1, 0.5f);
        mbRect.anchoredPosition = new Vector2(-10, 0);
        mbRect.sizeDelta = new Vector2(90, 40);

        var mbImg = menuButton.AddComponent<Image>();
        mbImg.color = UIFactory.AccentBlue;

        var mbBtn = menuButton.AddComponent<Button>();
        var mbColors = mbBtn.colors;
        mbColors.normalColor = UIFactory.AccentBlue;
        mbColors.highlightedColor = UIFactory.AccentBlue * 1.2f;
        mbColors.pressedColor = UIFactory.AccentBlue * 0.8f;
        mbBtn.colors = mbColors;
        mbBtn.onClick.AddListener(OnMenuButtonClicked);

        var mbLabelObj = new GameObject("Label");
        mbLabelObj.transform.SetParent(menuButton.transform, false);
        var mbLabelRect = mbLabelObj.AddComponent<RectTransform>();
        mbLabelRect.anchorMin = Vector2.zero;
        mbLabelRect.anchorMax = Vector2.one;
        mbLabelRect.offsetMin = Vector2.zero;
        mbLabelRect.offsetMax = Vector2.zero;
        var mbTmp = mbLabelObj.AddComponent<TextMeshProUGUI>();
        mbTmp.text = "MENU";
        mbTmp.fontSize = 16;
        mbTmp.color = Color.white;
        mbTmp.alignment = TextAlignmentOptions.Center;
        mbTmp.fontStyle = FontStyles.Bold;
        mbTmp.raycastTarget = false;

        menuButton.SetActive(false);
    }

    void UpdateTitle()
    {
        if (titleText != null && buildingData != null)
            titleText.text = buildingData.displayName;
    }

    void RebuildFloorButtons()
    {
        foreach (var obj in floorBtnObjects)
            if (obj != null) Destroy(obj);
        floorBtnObjects.Clear();

        if (buildingData == null) return;

        int floors = buildingData.floors;
        float btnWidth = Mathf.Min(50f, 290f / floors - 4f);
        float startX = -(floors - 1) * (btnWidth + 4f) / 2f;

        for (int i = 1; i <= floors; i++)
        {
            int floor = i;
            float x = startX + (i - 1) * (btnWidth + 4f);
            Color bgColor = (floor == selectedFloor) ? UIFactory.AccentBlue : UIFactory.BgCard;

            var btn = UIFactory.CreateButton(floorButtonsParent, $"F{floor}", $"F{floor}",
                new Vector2(x, 0), new Vector2(btnWidth, 34),
                bgColor, Color.white,
                () => OnFloorClicked(floor), 13);

            floorBtnObjects.Add(btn.gameObject);
        }
    }

    void OnFloorClicked(int floor)
    {
        if (floor == selectedFloor) return;

        if (indoorManager != null && buildingData != null)
            indoorManager.GoToFloor(buildingData.buildingCode, floor);

        selectedFloor = floor;

        if (floorLabel != null)
            floorLabel.text = $"Floor {floor} (via stairs)";

        for (int i = 0; i < floorBtnObjects.Count; i++)
        {
            if (floorBtnObjects[i] == null) continue;
            var img = floorBtnObjects[i].GetComponent<Image>();
            if (img != null)
                img.color = (i + 1 == selectedFloor) ? UIFactory.AccentBlue : UIFactory.BgCard;
        }

        RefreshRoomList();
    }

    void RefreshRoomList()
    {
        foreach (var item in roomItems)
            if (item != null) Destroy(item);
        roomItems.Clear();

        if (buildingData == null || indoorManager == null) return;

        if (floorLabel != null && !floorLabel.text.Contains("stairs"))
            floorLabel.text = $"Floor {selectedFloor}";

        int roomsPerSide = buildingData.roomsPerSide;

        // Left side rooms
        for (int r = 0; r < roomsPerSide; r++)
        {
            int roomIdx = r;
            string roomNum = IndoorManager.GetRoomLabel(buildingData.buildingCode, selectedFloor, r, true);
            string roomType = GetRoomType(selectedFloor, r, true);
            Color accent = RoomTypeColor(roomType);

            var item = UIFactory.CreateListItem(contentRoot,
                $"{roomNum} - {roomType}",
                $"Floor {selectedFloor} | Left side",
                accent,
                () => OnRoomSelected(roomIdx, true));
            roomItems.Add(item);
        }

        // Right side rooms
        for (int r = 0; r < roomsPerSide; r++)
        {
            int roomIdx = r;
            string roomNum = IndoorManager.GetRoomLabel(buildingData.buildingCode, selectedFloor, r, false);
            string roomType = GetRoomType(selectedFloor, r, false);
            Color accent = RoomTypeColor(roomType);

            var item = UIFactory.CreateListItem(contentRoot,
                $"{roomNum} - {roomType}",
                $"Floor {selectedFloor} | Right side",
                accent,
                () => OnRoomSelected(roomIdx, false));
            roomItems.Add(item);
        }

        // Stairwells
        var stair1 = UIFactory.CreateListItem(contentRoot,
            "Stairwell A (front)", $"Floor {selectedFloor}",
            UIFactory.TextSecondary, () => NavigateToStair(0));
        roomItems.Add(stair1);

        var stair2 = UIFactory.CreateListItem(contentRoot,
            "Stairwell B (middle)", $"Floor {selectedFloor}",
            UIFactory.TextSecondary, () => NavigateToStair(1));
        roomItems.Add(stair2);
    }

    void OnRoomSelected(int roomIndex, bool leftSide)
    {
        if (indoorManager == null || buildingData == null) return;
        indoorManager.NavigateToRoom(buildingData.buildingCode, selectedFloor, roomIndex, leftSide);

        // Hide panel, show menu button
        panelRoot.SetActive(false);
        menuButton.SetActive(true);
    }

    void NavigateToStair(int stairIndex)
    {
        if (indoorManager == null || buildingData == null) return;
        Vector3 pos = indoorManager.GetStairPosition(buildingData, selectedFloor, stairIndex);
        var agent = FindAnyObjectByType<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(pos);
        }

        // Hide panel, show menu button
        panelRoot.SetActive(false);
        menuButton.SetActive(true);
    }

    void OnMenuButtonClicked()
    {
        // Show panel, hide menu button
        panelRoot.SetActive(true);
        menuButton.SetActive(false);
    }

    void OnExitBuilding()
    {
        if (indoorManager != null)
            indoorManager.ExitBuilding();
        if (flow != null)
            flow.OnBackPressed();
    }

    static string GetRoomType(int floor, int roomIndex, bool leftSide)
    {
        if (floor == 1)
        {
            if (roomIndex == 0 && leftSide) return "Admin Office";
            if (roomIndex == 0) return "Security";
            if (leftSide) return "Office";
            return "Classroom";
        }
        if (floor <= 3)
        {
            if (roomIndex == 0 && leftSide) return "Auditorium";
            if (roomIndex % 3 == 0) return "Laboratory";
            if (leftSide) return "Classroom";
            return "Seminar Room";
        }
        if (roomIndex == 0) return "Research Lab";
        if (leftSide) return "Laboratory";
        return "Office";
    }

    static Color RoomTypeColor(string roomType)
    {
        if (roomType.Contains("Auditorium")) return new Color(0.6f, 0.3f, 0.9f);
        if (roomType.Contains("Lab")) return new Color(0.2f, 0.8f, 0.7f);
        if (roomType.Contains("Office") || roomType.Contains("Admin")) return UIFactory.AccentOrange;
        if (roomType.Contains("Classroom")) return UIFactory.AccentBlue;
        if (roomType.Contains("Seminar")) return new Color(0.9f, 0.4f, 0.6f);
        if (roomType.Contains("Security")) return UIFactory.AccentRed;
        if (roomType.Contains("Research")) return new Color(0.4f, 0.4f, 0.9f);
        return UIFactory.TextSecondary;
    }
}
