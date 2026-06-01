using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuScreen : MonoBehaviour
{
    AppFlowManager flow;
    Button arButton;
    TextMeshProUGUI arStatusText;
    TextMeshProUGUI gpsInfoText;
    bool uiBuilt;

    public void Show(AppFlowManager flowManager)
    {
        flow = flowManager;
        if (!uiBuilt) BuildUI();
        UpdateARAvailability();
    }

    void Update()
    {
        if (flow != null && gameObject.activeSelf)
            UpdateARAvailability();
    }

    void BuildUI()
    {
        uiBuilt = true;
        var panel = UIFactory.CreateFullscreenPanel(transform, "Background");

        // App title
        UIFactory.CreateTextAbsolute(panel, "Title", "CampusNav",
            new Vector2(0, 120), new Vector2(400, 60), 42, UIFactory.TextPrimary);

        UIFactory.CreateTextAbsolute(panel, "Subtitle", "Vilnius Tech - Sauletekio Rumai",
            new Vector2(0, 80), new Vector2(400, 30), 16, UIFactory.TextSecondary);

        // Map button
        UIFactory.CreateButton(panel, "MapButton", "MAP",
            new Vector2(0, 10), new Vector2(280, 60),
            UIFactory.AccentBlue, Color.white,
            () => flow?.OnModeSelected(AppFlowManager.NavigationMode.Map), 20);

        // AR button
        arButton = UIFactory.CreateButton(panel, "ARButton", "AR",
            new Vector2(0, -65), new Vector2(280, 60),
            UIFactory.AccentGreen, Color.white,
            OnARPressed, 20);

        // AR status text below AR button
        arStatusText = UIFactory.CreateTextAbsolute(panel, "ARStatus", "",
            new Vector2(0, -105), new Vector2(300, 24), 11, UIFactory.TextMuted);

        // GPS info at bottom
        gpsInfoText = UIFactory.CreateTextAbsolute(panel, "GpsInfo", "",
            new Vector2(0, -180), new Vector2(340, 40), 10, UIFactory.TextMuted);
        gpsInfoText.enableWordWrapping = true;

        // Version
        UIFactory.CreateTextAbsolute(panel, "Version", "v1.0 - Diploma Project",
            new Vector2(0, -220), new Vector2(300, 20), 10, UIFactory.TextMuted);
    }

    void OnARPressed()
    {
        if (flow == null) return;

        if (flow.IsGpsNearUniversity())
        {
            flow.OnModeSelected(AppFlowManager.NavigationMode.AR);
        }
    }

    void UpdateARAvailability()
    {
        if (arButton == null || arStatusText == null || flow == null) return;

        var geo = flow.GeoAnchor;
        bool gpsReady = geo != null && geo.IsLocationReady;
        bool nearUni = flow.IsGpsNearUniversity();

        arButton.interactable = nearUni;

        var btnImage = arButton.GetComponent<Image>();
        if (btnImage != null)
        {
            btnImage.color = nearUni
                ? UIFactory.AccentGreen
                : new Color(0.3f, 0.3f, 0.35f);
        }

        if (nearUni)
        {
            arStatusText.text = "<color=#88FF88>GPS: You are near campus - AR available</color>";
        }
        else if (gpsReady)
        {
            arStatusText.text = "GPS: Not near university - AR unavailable";
            if (gpsInfoText != null)
                gpsInfoText.text = $"Location: {geo.CurrentLatitude:F5}, {geo.CurrentLongitude:F5}\nAccuracy: {geo.CurrentAccuracy:F0}m";
        }
        else
        {
            arStatusText.text = "GPS: Initializing... AR requires campus proximity";
            if (gpsInfoText != null)
                gpsInfoText.text = "Waiting for GPS signal...";
        }

        if (arStatusText != null) arStatusText.richText = true;
    }
}
