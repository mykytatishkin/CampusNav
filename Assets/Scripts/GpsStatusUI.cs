using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays GPS status, accuracy, coordinates, and campus state
/// as a small overlay in the corner of the screen.
/// </summary>
public class GpsStatusUI : MonoBehaviour
{
    [SerializeField] private GeoAnchorSystem geoAnchor;
    [SerializeField] private PlayerGeoTracker playerTracker;

    RectTransform panel;
    TextMeshProUGUI statusText;
    Image statusDot;

    void Start()
    {
        BuildUI();
    }

    void BuildUI()
    {
        // Panel in top-left corner
        var panelObj = new GameObject("GpsStatusPanel");
        panelObj.transform.SetParent(transform, false);
        panel = panelObj.AddComponent<RectTransform>();
        panel.anchorMin = new Vector2(0, 1);
        panel.anchorMax = new Vector2(0, 1);
        panel.pivot = new Vector2(0, 1);
        panel.anchoredPosition = new Vector2(8, -8);
        panel.sizeDelta = new Vector2(200, 60);

        var bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        bg.raycastTarget = false;

        // Status dot (green/yellow/red)
        var dotObj = new GameObject("StatusDot");
        dotObj.transform.SetParent(panelObj.transform, false);
        var dotRect = dotObj.AddComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(0, 1);
        dotRect.anchorMax = new Vector2(0, 1);
        dotRect.pivot = new Vector2(0, 1);
        dotRect.anchoredPosition = new Vector2(8, -8);
        dotRect.sizeDelta = new Vector2(10, 10);
        statusDot = dotObj.AddComponent<Image>();
        statusDot.color = Color.red;
        statusDot.raycastTarget = false;

        // Status text
        var textObj = new GameObject("StatusText");
        textObj.transform.SetParent(panelObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(24, 4);
        textRect.offsetMax = new Vector2(-4, -4);
        statusText = textObj.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 9;
        statusText.color = Color.white;
        statusText.alignment = TextAlignmentOptions.TopLeft;
        statusText.enableWordWrapping = true;
        statusText.raycastTarget = false;
    }

    void Update()
    {
        if (statusText == null) return;

        if (geoAnchor == null)
        {
            statusDot.color = Color.gray;
            statusText.text = "GPS: No anchor";
            return;
        }

        if (!geoAnchor.IsLocationReady)
        {
            statusDot.color = Color.red;
            statusText.text = "GPS: Initializing...";
            return;
        }

        string mode = playerTracker != null ? playerTracker.Mode.ToString() : "?";
        bool hasFix = playerTracker != null && playerTracker.HasGpsFix;

        if (hasFix)
        {
            float acc = geoAnchor.CurrentAccuracy;
            // Green if accuracy < 10m, yellow if < 30m, red otherwise
            statusDot.color = acc < 10f ? Color.green : acc < 30f ? Color.yellow : new Color(1f, 0.5f, 0f);

            string campus = geoAnchor.IsOnCampus ? "<color=#88FF88>On Campus</color>" : "<color=#FFAA44>Off Campus</color>";
            statusText.text = $"GPS: {mode} | {acc:F0}m\n" +
                              $"{geoAnchor.CurrentLatitude:F5}, {geoAnchor.CurrentLongitude:F5}\n" +
                              campus;
        }
        else
        {
            statusDot.color = Color.yellow;
            statusText.text = $"GPS: {mode}\nWaiting for fix...";
        }
    }
}
