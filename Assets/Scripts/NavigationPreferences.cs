using UnityEngine;

public class NavigationPreferences : MonoBehaviour
{
    [SerializeField] private bool useElevators = true;
    [SerializeField] private bool accessibleRoutesOnly;

    ElevatorLink[] allLinks;

    public bool UseElevators
    {
        get => useElevators;
        set
        {
            useElevators = value;
            ApplyPreferences();
        }
    }

    public bool AccessibleRoutesOnly
    {
        get => accessibleRoutesOnly;
        set
        {
            accessibleRoutesOnly = value;
            ApplyPreferences();
        }
    }

    void Start()
    {
        RefreshLinks();
        ApplyPreferences();
    }

    public void RefreshLinks()
    {
        allLinks = FindObjectsByType<ElevatorLink>(FindObjectsSortMode.None);
    }

    public void SetElevatorsEnabled(bool enabled)
    {
        UseElevators = enabled;
    }

    void ApplyPreferences()
    {
        if (allLinks == null) return;

        foreach (var link in allLinks)
        {
            if (link == null) continue;

            bool shouldEnable = true;

            if (!useElevators && link.RequiresElevator)
                shouldEnable = false;

            if (accessibleRoutesOnly && !link.IsAccessible)
                shouldEnable = false;

            link.SetEnabled(shouldEnable);
        }
    }
}
