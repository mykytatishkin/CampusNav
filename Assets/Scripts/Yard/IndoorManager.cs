using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Manages separate indoor areas for each building.
/// Each building has a full indoor floor plan positioned at Y=-100 with offset per building.
/// Handles teleportation between yard and indoor areas.
/// Manages floor visibility: only current floor and below are visible, floors above are hidden.
/// </summary>
public class IndoorManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent playerAgent;
    [SerializeField] private CampusCameraController cameraController;
    [SerializeField] private CampusNavigator navigator;

    [Header("Indoor Data")]
    [SerializeField] private List<IndoorBuildingData> buildings = new();

    // Runtime state
    string currentBuildingCode;
    Vector3 yardReturnPosition;
    bool isIndoors;
    int currentFloor = 1;

    // Cached floor GameObjects for current building
    readonly Dictionary<string, Transform[]> buildingFloorObjects = new();

    public bool IsIndoors => isIndoors;
    public string CurrentBuildingCode => currentBuildingCode;
    public int CurrentFloor => currentFloor;
    public List<IndoorBuildingData> Buildings => buildings;

    void Start()
    {
        // Cache all indoor floor objects for quick show/hide
        CacheFloorObjects();
    }

    void CacheFloorObjects()
    {
        buildingFloorObjects.Clear();

        // Find the IndoorAreas root
        var indoorRoot = transform.parent?.Find("IndoorAreas");
        if (indoorRoot == null) return;

        foreach (var data in buildings)
        {
            var bldTransform = indoorRoot.Find($"Indoor_{data.buildingCode}");
            if (bldTransform == null) continue;

            var floors = new Transform[data.floors];
            for (int i = 0; i < data.floors; i++)
            {
                floors[i] = bldTransform.Find($"Floor_{i + 1}");
            }
            buildingFloorObjects[data.buildingCode] = floors;
        }
    }

    /// <summary>Show only the current floor and below, hide floors above.</summary>
    void UpdateFloorVisibility(string buildingCode, int visibleFloor)
    {
        if (!buildingFloorObjects.TryGetValue(buildingCode, out var floors)) return;

        for (int i = 0; i < floors.Length; i++)
        {
            if (floors[i] == null) continue;
            // Floor index i corresponds to floor number i+1
            // Show current floor and all below, hide all above
            floors[i].gameObject.SetActive(i + 1 <= visibleFloor);
        }
    }

    /// <summary>Show all floors (used when exiting building).</summary>
    void ShowAllFloors(string buildingCode)
    {
        if (!buildingFloorObjects.TryGetValue(buildingCode, out var floors)) return;
        foreach (var f in floors)
            if (f != null) f.gameObject.SetActive(true);
    }

    public void AddBuilding(IndoorBuildingData data)
    {
        buildings.Add(data);
    }

    public IndoorBuildingData GetBuildingData(string buildingCode)
    {
        return buildings.Find(b => b.buildingCode == buildingCode);
    }

    /// <summary>Teleport player from yard into a building's indoor area.</summary>
    public void EnterBuilding(string buildingCode)
    {
        var data = GetBuildingData(buildingCode);
        if (data == null)
        {
            Debug.LogWarning($"[IndoorManager] No indoor data for {buildingCode}");
            return;
        }

        if (isIndoors) return;

        // Re-cache if not cached yet (e.g. scene was just generated)
        if (buildingFloorObjects.Count == 0)
            CacheFloorObjects();

        isIndoors = true;
        currentBuildingCode = buildingCode;
        currentFloor = 1;

        // Save yard position for return
        if (playerAgent != null)
            yardReturnPosition = playerAgent.transform.position;

        // Teleport to indoor entrance (front of corridor, floor 1)
        Vector3 spawnPos = GetCorridorEntrance(data, 1);
        TeleportPlayer(spawnPos);

        // Show only floor 1
        UpdateFloorVisibility(buildingCode, 1);

        // Focus camera on indoor area
        if (cameraController != null)
        {
            cameraController.FocusOn(spawnPos);
            cameraController.SetOrbit(0, 50f, 25f);
            cameraController.SwitchMode(CampusCameraController.CameraMode.Follow);
        }

        Debug.Log($"[IndoorManager] Entered {buildingCode}, showing floor 1");
    }

    /// <summary>Teleport player back to the yard.</summary>
    public void ExitBuilding()
    {
        if (!isIndoors) return;

        // Restore all floors before leaving
        if (currentBuildingCode != null)
            ShowAllFloors(currentBuildingCode);

        isIndoors = false;
        currentBuildingCode = null;

        TeleportPlayer(yardReturnPosition);

        if (cameraController != null)
        {
            cameraController.FocusOn(yardReturnPosition);
            cameraController.SetOrbit(45f, 60f, 60f);
            cameraController.SwitchMode(CampusCameraController.CameraMode.Free);
        }

        Debug.Log("[IndoorManager] Exited building");
    }

    /// <summary>Navigate to a specific room in the current building.</summary>
    public void NavigateToRoom(string buildingCode, int floor, int roomIndex, bool leftSide)
    {
        var data = GetBuildingData(buildingCode);
        if (data == null) return;

        currentFloor = floor;
        Vector3 roomPos = GetRoomPosition(data, floor, roomIndex, leftSide);

        // Update floor visibility
        UpdateFloorVisibility(buildingCode, floor);

        // Use NavMeshAgent directly for indoor navigation
        if (playerAgent != null)
        {
            playerAgent.isStopped = false;
            playerAgent.SetDestination(roomPos);
        }

        if (cameraController != null)
            cameraController.SwitchMode(CampusCameraController.CameraMode.Follow);
    }

    /// <summary>Navigate to a stairwell and then teleport to target floor.</summary>
    public void GoToFloor(string buildingCode, int targetFloor)
    {
        var data = GetBuildingData(buildingCode);
        if (data == null) return;

        // Teleport to the stairwell position on the target floor
        Vector3 stairPos = GetStairPosition(data, targetFloor, 0);
        TeleportPlayer(stairPos);
        currentFloor = targetFloor;

        // Update floor visibility — show only current floor and below
        UpdateFloorVisibility(buildingCode, targetFloor);

        if (cameraController != null)
            cameraController.FocusOn(stairPos);

        Debug.Log($"[IndoorManager] Moved to floor {targetFloor}");
    }

    // ==================== POSITION HELPERS ====================

    /// <summary>Corridor entrance position (front of building, given floor).</summary>
    public Vector3 GetCorridorEntrance(IndoorBuildingData data, int floor)
    {
        float y = data.indoorOrigin.y + (floor - 1) * data.floorHeight + 0.1f;
        return new Vector3(
            data.indoorOrigin.x + data.buildingLength * 0.45f,
            y,
            data.indoorOrigin.z
        );
    }

    /// <summary>Room center position in the indoor area.</summary>
    public Vector3 GetRoomPosition(IndoorBuildingData data, int floor, int roomIndex, bool leftSide)
    {
        float y = data.indoorOrigin.y + (floor - 1) * data.floorHeight + 0.1f;
        float corridorHalfW = data.corridorWidth / 2f;
        float roomDepth = (data.buildingWidth - data.corridorWidth) / 2f;

        float roomWidth = data.buildingLength / data.roomsPerSide;
        float x = data.indoorOrigin.x - data.buildingLength / 2f + roomWidth * (roomIndex + 0.5f);

        float z = leftSide
            ? data.indoorOrigin.z - corridorHalfW - roomDepth / 2f
            : data.indoorOrigin.z + corridorHalfW + roomDepth / 2f;

        return new Vector3(x, y, z);
    }

    /// <summary>Stairwell position (stairIndex: 0=front, 1=middle).</summary>
    public Vector3 GetStairPosition(IndoorBuildingData data, int floor, int stairIndex)
    {
        float y = data.indoorOrigin.y + (floor - 1) * data.floorHeight + 0.1f;
        float x = stairIndex == 0
            ? data.indoorOrigin.x - data.buildingLength * 0.4f
            : data.indoorOrigin.x;
        return new Vector3(x, y, data.indoorOrigin.z);
    }

    /// <summary>Get room label for a given building, floor, room index, side.</summary>
    public static string GetRoomLabel(string buildingCode, int floor, int roomIndex, bool leftSide)
    {
        int roomNum = floor * 100 + roomIndex * 2 + (leftSide ? 1 : 2);
        return $"{roomNum}";
    }

    void TeleportPlayer(Vector3 position)
    {
        if (playerAgent != null)
        {
            playerAgent.ResetPath();
            playerAgent.Warp(position);
        }
    }
}

[System.Serializable]
public class IndoorBuildingData
{
    public string buildingCode;
    public string displayName;
    public Vector3 indoorOrigin;
    public int floors;
    public float buildingLength;
    public float buildingWidth;
    public float corridorWidth;
    public float floorHeight;
    public int roomsPerSide;
    public Vector3 yardEntrancePosition;
}
