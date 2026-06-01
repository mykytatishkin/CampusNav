using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CampusSelectScreen : MonoBehaviour
{
    AppFlowManager flow;
    RectTransform contentRoot;
    readonly List<GameObject> items = new();
    bool uiBuilt;
    RectTransform panel;

    public void Show(AppFlowManager flowManager, CampusData[] campuses)
    {
        flow = flowManager;
        if (!uiBuilt) BuildUI();
        PopulateCampuses(campuses);
    }

    void BuildUI()
    {
        uiBuilt = true;
        panel = UIFactory.CreateFullscreenPanel(transform, "Background");

        UIFactory.CreateBackButton(panel, () => flow?.OnBackPressed());

        UIFactory.CreateTextAbsolute(panel, "Title", "Select Campus",
            new Vector2(0, 160), new Vector2(400, 40), 28, UIFactory.TextPrimary);

        UIFactory.CreateTextAbsolute(panel, "Subtitle",
            flow?.CurrentMode == AppFlowManager.NavigationMode.AR ? "AR Mode" : "Map Mode",
            new Vector2(0, 130), new Vector2(300, 24), 14, UIFactory.AccentBlue);

        var (_, content) = UIFactory.CreateScrollList(panel, "CampusList",
            new Vector2(0, -20), new Vector2(340, 360));
        contentRoot = content;
    }

    void PopulateCampuses(CampusData[] campuses)
    {
        foreach (var item in items)
            if (item != null) Destroy(item);
        items.Clear();

        if (campuses == null) return;

        foreach (var campus in campuses)
        {
            if (campus == null) continue;
            var c = campus;

            string buildingList = "";
            if (campus.buildings != null && campus.buildings.Count > 0)
            {
                var codes = new List<string>();
                foreach (var b in campus.buildings)
                    codes.Add(b.code);
                buildingList = string.Join(", ", codes);
            }

            var item = UIFactory.CreateListItem(contentRoot, campus.campusName,
                $"{campus.address}\nBuildings: {buildingList}",
                UIFactory.AccentBlue,
                () => flow?.OnCampusSelected(c));

            // Make it taller to fit subtitle
            var le = item.GetComponent<LayoutElement>();
            if (le != null) { le.minHeight = 68; le.preferredHeight = 68; }

            items.Add(item);
        }
    }
}
