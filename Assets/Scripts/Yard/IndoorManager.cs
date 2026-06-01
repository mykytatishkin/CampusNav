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
    [SerializeField] private List<GameObject> indoorRoots = new(); // direct refs to Indoor_XXX objects

    // Runtime state
    string currentBuildingCode;
    Vector3 yardReturnPosition;
    bool isIndoors;
    int currentFloor = 1;

    public bool IsIndoors => isIndoors;
    public string CurrentBuildingCode => currentBuildingCode;
    public int CurrentFloor => currentFloor;
    public List<IndoorBuildingData> Buildings => buildings;

    /// <summary>Get indoor root — try direct refs first, fallback to scene search.</summary>
    Transform GetIndoorRoot(string buildingCode)
    {
        // Try direct references
        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i].buildingCode == buildingCode && i < indoorRoots.Count && indoorRoots[i] != null)
                return indoorRoots[i].transform;
        }

        // Fallback: search scene
        Debug.LogWarning($"[IndoorManager] Direct ref missing for {buildingCode}, searching scene...");
        var go = GameObject.Find($"Indoor_{buildingCode}");
        if (go != null) return go.transform;

        // Last resort: search all transforms
        var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in allTransforms)
        {
            if (t.name == $"Indoor_{buildingCode}" && t.gameObject.scene.isLoaded)
                return t;
        }

        Debug.LogError($"[IndoorManager] CANNOT find Indoor_{buildingCode} anywhere!");
        return null;
    }

    /// <summary>Show current floor, hide all others.</summary>
    void UpdateFloorVisibility(string buildingCode, int visibleFloor)
    {
        var root = GetIndoorRoot(buildingCode);
        if (root == null) return;

        int hidden = 0, shown = 0;
        for (int c = 0; c < root.childCount; c++)
        {
            var child = root.GetChild(c);
            if (!child.name.StartsWith("Floor_")) continue;
            if (!int.TryParse(child.name.Substring(6), out int floorNum)) continue;

            bool visible = floorNum == visibleFloor;
            child.gameObject.SetActive(visible);
            if (visible) shown++; else hidden++;
        }
        Debug.Log($"[IndoorManager] {buildingCode} floor {visibleFloor}: shown={shown} hidden={hidden}");
    }

    /// <summary>Show all floors (used when exiting building).</summary>
    void ShowAllFloors(string buildingCode)
    {
        var root = GetIndoorRoot(buildingCode);
        if (root == null) return;

        for (int c = 0; c < root.childCount; c++)
        {
            var child = root.GetChild(c);
            if (child.name.StartsWith("Floor_"))
                child.gameObject.SetActive(true);
        }
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

        // Focus camera on indoor area — top-down view
        if (cameraController != null)
        {
            cameraController.FocusOn(spawnPos);
            cameraController.SetOrbit(0, 70f, 30f);
            cameraController.SwitchMode(CampusCameraController.CameraMode.Follow);
        }

        Debug.Log($"[IndoorManager] Entered {buildingCode}, spawned at {spawnPos}");
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
