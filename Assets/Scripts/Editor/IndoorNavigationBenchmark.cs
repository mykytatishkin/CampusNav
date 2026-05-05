using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using System.Collections.Generic;

/// <summary>
/// Generates an additional benchmark scene focused specifically on indoor navigation testing.
/// This scene is isolated and won't affect any other scenes.
/// Features: Multi-room layouts, corridors, doorways, stairs, elevators across multiple floors.
/// Includes dynamic floor visibility system - floors above/below player become transparent.
/// </summary>
public static class IndoorNavigationBenchmark
{
    const float FloorHeight = 3.5f;
    const float WallThickness = 0.2f;
    const float DoorWidth = 1.8f;
    const float RoomSize = 8f;
    
    [MenuItem("CampusNav/Generate Indoor Navigation Benchmark")]
    static void Generate()
    {
        if (!EditorUtility.DisplayDialog("Indoor Navigation Benchmark",
            "Generate a dedicated indoor navigation test scene?\nThis will create 'IndoorBenchmarkWorld' object.\nExisting scenes will not be affected.",
            "Create", "Cancel"))
            return;

        var old = GameObject.Find("IndoorBenchmarkWorld");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("IndoorBenchmarkWorld");
        Undo.RegisterCreatedObjectUndo(root, "Generate Indoor Navigation Benchmark");

        // ===== GROUND FLOOR =====
        var floor0 = CreateFloorSlab(root, "GroundFloor", Vector3.zero, 60f, 40f, new Color(0.85f, 0.82f, 0.78f));
        floor0.isStatic = true;

        // ===== BUILDING STRUCTURE - 3 FLOORS =====
        var building = new GameObject("IndoorBuilding");
        building.transform.SetParent(root.transform);
        building.isStatic = true;

        // Track floor groups for visibility system
        var floorGroups = new List<GameObject>();

        Color wallColor = new Color(0.92f, 0.90f, 0.88f);
        Color floorColor = new Color(0.88f, 0.85f, 0.82f);
        Color doorColor = new Color(0.75f, 0.65f, 0.55f);

        // --- Ground Floor Layout ---
        var floor0Group = Child(building, "Floor_0");
        floor0Group.transform.position = V(0, 0.05f, 0);
        floorGroups.Add(floor0Group);
        
        // Outer walls
        CreateWall(floor0Group, "OuterWall_N", V(0, FloorHeight * 1.5f, 20), 60, FloorHeight * 3, WallThickness, wallColor);
        CreateWall(floor0Group, "OuterWall_S", V(0, FloorHeight * 1.5f, -20), 60, FloorHeight * 3, WallThickness, wallColor);
        CreateWall(floor0Group, "OuterWall_E", V(30, FloorHeight * 1.5f, 0), WallThickness, FloorHeight * 3, 40, wallColor);
        CreateWall(floor0Group, "OuterWall_W", V(-30, FloorHeight * 1.5f, 0), WallThickness, FloorHeight * 3, 40, wallColor);

        // Interior walls - creating rooms and corridors
        // Main corridor (horizontal)
        CreateWall(floor0Group, "Corridor_N", V(0, FloorHeight * 1.5f, 5), 58, FloorHeight * 3, WallThickness, wallColor);
        CreateWall(floor0Group, "Corridor_S", V(0, FloorHeight * 1.5f, -5), 58, FloorHeight * 3, WallThickness, wallColor);
        
        // Vertical dividers creating rooms
        CreateWall(floor0Group, "Divider_1", V(-15, FloorHeight * 1.5f, 0), WallThickness, FloorHeight * 3, 10, wallColor);
        CreateWall(floor0Group, "Divider_2", V(15, FloorHeight * 1.5f, 0), WallThickness, FloorHeight * 3, 10, wallColor);
        CreateWall(floor0Group, "Divider_3", V(-22, FloorHeight * 1.5f, 12), WallThickness, FloorHeight * 3, 8, wallColor);
        CreateWall(floor0Group, "Divider_4", V(-22, FloorHeight * 1.5f, -12), WallThickness, FloorHeight * 3, 8, wallColor);
        CreateWall(floor0Group, "Divider_5", V(22, FloorHeight * 1.5f, 12), WallThickness, FloorHeight * 3, 8, wallColor);
        CreateWall(floor0Group, "Divider_6", V(22, FloorHeight * 1.5f, -12), WallThickness, FloorHeight * 3, 8, wallColor);

        // Doorways (gaps in walls represented by shorter wall segments)
        CreateDoorway(floor0Group, "Door_Corridor_W", V(-25, 0.1f, 0), doorColor);
        CreateDoorway(floor0Group, "Door_Corridor_E", V(25, 0.1f, 0), doorColor);
        CreateDoorway(floor0Group, "Door_Room_NW", V(-15, 0.1f, 8), doorColor);
        CreateDoorway(floor0Group, "Door_Room_SW", V(-15, 0.1f, -8), doorColor);
        CreateDoorway(floor0Group, "Door_Room_NE", V(15, 0.1f, 8), doorColor);
        CreateDoorway(floor0Group, "Door_Room_SE", V(15, 0.1f, -8), doorColor);

        // Stairwell ground floor
        CreateStairwell(floor0Group, "Stairwell", V(-8, 0, 15), FloorHeight, new Color(0.65f, 0.60f, 0.55f));

        // Elevator shaft ground floor
        CreateElevatorShaft(floor0Group, "Elevator", V(8, 0, 15), 2.5f, 2.5f, new Color(0.50f, 0.50f, 0.55f));

        // --- First Floor Layout ---
        var floor1Group = Child(building, "Floor_1");
        floor1Group.transform.position = V(0, FloorHeight + 0.05f, 0);
        floorGroups.Add(floor1Group);
        
        // Floor slab
        CreateFloorSlab(building, "Floor1_Slab", V(0, FloorHeight, 0), 58f, 38f, floorColor).isStatic = true;
        
        // Similar layout to ground floor but with variations
        CreateWall(floor1Group, "Corridor_N", V(0, FloorHeight * 1.5f, 5), 58, FloorHeight * 3, WallThickness, wallColor);
        CreateWall(floor1Group, "Corridor_S", V(0, FloorHeight * 1.5f, -5), 58, FloorHeight * 3, WallThickness, wallColor);
        CreateWall(floor1Group, "Divider_1", V(-18, FloorHeight * 1.5f, 0), WallThickness, FloorHeight * 3, 10, wallColor);
        CreateWall(floor1Group, "Divider_2", V(0, FloorHeight * 1.5f, 0), WallThickness, FloorHeight * 3, 10, wallColor);
        CreateWall(floor1Group, "Divider_3", V(18, FloorHeight * 1.5f, 0), WallThickness, FloorHeight * 3, 10, wallColor);

        CreateDoorway(floor1Group, "Door_Corridor_W", V(-25, FloorHeight + 0.1f, 0), doorColor);
        CreateDoorway(floor1Group, "Door_Corridor_E", V(25, FloorHeight + 0.1f, 0), doorColor);
        CreateDoorway(floor1Group, "Door_Room_W", V(-9, FloorHeight + 0.1f, 8), doorColor);
        CreateDoorway(floor1Group, "Door_Room_Center", V(0, FloorHeight + 0.1f, -8), doorColor);
        CreateDoorway(floor1Group, "Door_Room_E", V(9, FloorHeight + 0.1f, 8), doorColor);

        // Stairwell first floor
        CreateStairwell(floor1Group, "Stairwell", V(-8, FloorHeight, 15), FloorHeight, new Color(0.65f, 0.60f, 0.55f));
        
        // Elevator shaft first floor
        CreateElevatorShaft(floor1Group, "Elevator", V(8, FloorHeight, 15), 2.5f, 2.5f, new Color(0.50f, 0.50f, 0.55f));

        // --- Second Floor Layout ---
        var floor2Group = Child(building, "Floor_2");
        floor2Group.transform.position = V(0, FloorHeight * 2 + 0.05f, 0);
        floorGroups.Add(floor2Group);
        
        // Floor slab
        CreateFloorSlab(building, "Floor2_Slab", V(0, FloorHeight * 2, 0), 58f, 38f, floorColor).isStatic = true;

        // Open plan with some partitions
        CreateWall(floor2Group, "Partition_1", V(-10, FloorHeight * 1.5f, 10), 20, FloorHeight * 3, WallThickness, wallColor);
        CreateWall(floor2Group, "Partition_2", V(10, FloorHeight * 1.5f, -10), 20, FloorHeight * 3, WallThickness, wallColor);
        CreateWall(floor2Group, "Partition_3", V(0, FloorHeight * 1.5f, 0), WallThickness, FloorHeight * 3, 15, wallColor);

        // Stairwell second floor
        CreateStairwell(floor2Group, "Stairwell", V(-8, FloorHeight * 2, 15), FloorHeight, new Color(0.65f, 0.60f, 0.55f));
        
        // Elevator shaft second floor
        CreateElevatorShaft(floor2Group, "Elevator", V(8, FloorHeight * 2, 15), 2.5f, 2.5f, new Color(0.50f, 0.50f, 0.55f));

        // Roof
        CreateFloorSlab(building, "Roof", V(0, FloorHeight * 3 + 0.1f, 0), 60f, 40f, new Color(0.45f, 0.42f, 0.38f)).isStatic = true;

        // ===== NAVMESH LINKS FOR VERTICAL CONNECTIONS =====
        // Stair links
        AddNavMeshLink(building, "Stair_0_1", V(-8, 0.1f, 15), V(-8, FloorHeight + 0.1f, 15), 1, 2);
        AddNavMeshLink(building, "Stair_1_2", V(-8, FloorHeight + 0.1f, 15), V(-8, FloorHeight * 2 + 0.1f, 15), 2, 3);
        
        // Elevator links
        AddNavMeshLink(building, "Elevator_0_1", V(8, 0.15f, 15), V(8, FloorHeight + 0.15f, 15), 1, 2);
        AddNavMeshLink(building, "Elevator_1_2", V(8, FloorHeight + 0.15f, 15), V(8, FloorHeight * 2 + 0.15f, 15), 2, 3);

        // ===== ADDITIONAL COMPLEX ROOMS WING =====
        var wingObj = new GameObject("ComplexWing");
        wingObj.transform.SetParent(root.transform);
        wingObj.transform.position = V(0, 0, -35);
        wingObj.isStatic = true;

        // Wing ground floor
        CreateFloorSlab(wingObj, "Wing_Floor", V(0, 0.05f, 0), 40f, 25f, floorColor);
        
        // Maze-like room layout
        for (int i = 0; i < 4; i++)
        {
            float x = -15 + i * 10;
            CreateWall(wingObj, $"WingRoom_W_{i}", V(x, FloorHeight * 1.5f, 8), 8, FloorHeight * 3, WallThickness, wallColor);
            CreateWall(wingObj, $"WingRoom_E_{i}", V(x, FloorHeight * 1.5f, -8), 8, FloorHeight * 3, WallThickness, wallColor);
            
            // Doorway offset for maze effect
            float doorOffset = (i % 2 == 0) ? -3f : 3f;
            CreateDoorway(wingObj, $"WingDoor_{i}", V(x, 0.1f, doorOffset), doorColor);
        }

        // ===== OBSTACLES & FURNITURE (simulated as boxes) =====
        var obstacles = Child(root, "Obstacles_Furniture");
        
        // Ground floor obstacles
        CreateBox(obstacles, "Desk_1", V(-20, 0.4f, 10), 2f, 0.8f, 1f, new Color(0.60f, 0.45f, 0.30f));
        CreateBox(obstacles, "Desk_2", V(-20, 0.4f, -10), 2f, 0.8f, 1f, new Color(0.60f, 0.45f, 0.30f));
        CreateBox(obstacles, "Table_Center", V(0, 0.35f, 0), 3f, 0.7f, 1.5f, new Color(0.55f, 0.40f, 0.28f));
        CreateBox(obstacles, "Cabinet_1", V(20, 0.5f, 12), 1f, 1f, 0.5f, new Color(0.50f, 0.45f, 0.40f));
        CreateBox(obstacles, "Cabinet_2", V(20, 0.5f, -12), 1f, 1f, 0.5f, new Color(0.50f, 0.45f, 0.40f));

        // First floor obstacles
        CreateBox(obstacles, "Desk_F1_1", V(-20, FloorHeight + 0.4f, 10), 2f, 0.8f, 1f, new Color(0.60f, 0.45f, 0.30f));
        CreateBox(obstacles, "Desk_F1_2", V(0, FloorHeight + 0.4f, -10), 2f, 0.8f, 1f, new Color(0.60f, 0.45f, 0.30f));
        CreateBox(obstacles, "Table_F1", V(20, FloorHeight + 0.35f, 0), 3f, 0.7f, 1.5f, new Color(0.55f, 0.40f, 0.28f));

        // Second floor obstacles  
        CreateBox(obstacles, "Desk_F2_1", V(-5, FloorHeight * 2 + 0.4f, 5), 2f, 0.8f, 1f, new Color(0.60f, 0.45f, 0.30f));
        CreateBox(obstacles, "Desk_F2_2", V(5, FloorHeight * 2 + 0.4f, -5), 2f, 0.8f, 1f, new Color(0.60f, 0.45f, 0.30f));
        CreateBox(obstacles, "Partition_Plant", V(0, FloorHeight * 2 + 0.6f, 8), 0.8f, 1.2f, 0.8f, new Color(0.30f, 0.55f, 0.30f));

        // ===== DESTINATION MARKERS =====
        var markers = Child(root, "DestinationMarkers");
        
        // Ground floor destinations
        CreateMarker(markers, "Dest_Ground_RoomNW", V(-22, 0.5f, 12), new Color(1f, 0.3f, 0.3f), 0.8f);
        CreateMarker(markers, "Dest_Ground_RoomSW", V(-22, 0.5f, -12), new Color(1f, 0.3f, 0.3f), 0.8f);
        CreateMarker(markers, "Dest_Ground_RoomNE", V(22, 0.5f, 12), new Color(1f, 0.4f, 0.3f), 0.8f);
        CreateMarker(markers, "Dest_Ground_RoomSE", V(22, 0.5f, -12), new Color(1f, 0.4f, 0.3f), 0.8f);
        CreateMarker(markers, "Dest_Ground_Center", V(0, 0.5f, 0), new Color(1f, 0.5f, 0.3f), 1f);

        // First floor destinations
        CreateMarker(markers, "Dest_F1_RoomW", V(-13, FloorHeight + 0.5f, 8), new Color(0.3f, 1f, 0.3f), 0.8f);
        CreateMarker(markers, "Dest_F1_RoomCenter", V(0, FloorHeight + 0.5f, -8), new Color(0.3f, 1f, 0.4f), 0.8f);
        CreateMarker(markers, "Dest_F1_RoomE", V(13, FloorHeight + 0.5f, 8), new Color(0.3f, 1f, 0.5f), 0.8f);

        // Second floor destinations
        CreateMarker(markers, "Dest_F2_ZoneA", V(-5, FloorHeight * 2 + 0.5f, 5), new Color(0.3f, 0.3f, 1f), 0.8f);
        CreateMarker(markers, "Dest_F2_ZoneB", V(5, FloorHeight * 2 + 0.5f, -5), new Color(0.3f, 0.4f, 1f), 0.8f);
        CreateMarker(markers, "Dest_F2_Open", V(0, FloorHeight * 2 + 0.5f, -12), new Color(0.3f, 0.5f, 1f), 0.8f);

        // Wing destinations
        for (int i = 0; i < 4; i++)
        {
            float x = -15 + i * 10;
            float z = (i % 2 == 0) ? 10f : -10f;
            CreateMarker(markers, $"Dest_Wing_Room{i}", V(x, 0.5f, z), new Color(1f, 0.8f, 0.3f), 0.6f);
        }

        // Start position marker
        CreateMarker(markers, "Start_Position", V(0, 0.5f, -18), new Color(1f, 0.9f, 0.2f), 1.2f);

        // ===== PLAYER =====
        var player = new GameObject("Player");
        player.transform.SetParent(root.transform);
        player.transform.position = V(0, 0.6f, -18);
        player.tag = "Player";

        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "PlayerModel";
        capsule.transform.SetParent(player.transform);
        capsule.transform.localPosition = V(0, 1, 0);
        capsule.transform.localScale = V(0.8f, 1, 0.8f);
        SetMaterial(capsule, new Color(0.2f, 0.6f, 1f));
        Object.DestroyImmediate(capsule.GetComponent<Collider>());

        var agent = player.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.speed = 4f;
        agent.radius = 0.4f;
        agent.height = 2f;
        agent.angularSpeed = 360f;
        agent.acceleration = 20f;

        // ===== FLOOR VISIBILITY CONTROLLER =====
        var visibilityCtrl = player.AddComponent<FloorVisibilityController>();
        var visSO = new SerializedObject(visibilityCtrl);
        visSO.FindProperty("floorGroups").arraySize = floorGroups.Count;
        for (int i = 0; i < floorGroups.Count; i++)
            visSO.FindProperty("floorGroups").GetArrayElementAtIndex(i).objectReferenceValue = floorGroups[i];
        visSO.ApplyModifiedPropertiesWithoutUndo();

        // ===== CAMERA =====
        var camObj = GameObject.Find("Main Camera");
        if (camObj == null)
        {
            camObj = new GameObject("Main Camera");
            camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        camObj.transform.position = V(0, 25, -35);
        camObj.transform.rotation = Quaternion.Euler(45, 0, 0);

        var camCtrl = camObj.GetComponent<CampusCameraController>();
        if (camCtrl == null) camCtrl = camObj.AddComponent<CampusCameraController>();

        var camSO = new SerializedObject(camCtrl);
        camSO.FindProperty("target").objectReferenceValue = player.transform;
        camSO.ApplyModifiedPropertiesWithoutUndo();

        // ===== NAVMESH SURFACE =====
        var navSurface = new GameObject("IndoorNavMesh");
        navSurface.transform.SetParent(root.transform);
        var surface = navSurface.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;

        // ===== INDOOR-SPECIFIC ROUTE DATABASE =====
        var routeDb = ScriptableObject.CreateInstance<RouteDatabase>();
        routeDb.allPoints = new System.Collections.Generic.List<RoutePoint>();

        // Create route points for indoor navigation graph
        string[] pointNames = {
            "Start", "Ground_Center", "Ground_RoomNW", "Ground_RoomSW", 
            "Ground_RoomNE", "Ground_RoomSE", "Ground_Stairs", "Ground_Elevator",
            "F1_Center", "F1_RoomW", "F1_RoomCenter", "F1_RoomE", "F1_Stairs", "F1_Elevator",
            "F2_ZoneA", "F2_ZoneB", "F2_Open", "F2_Stairs", "F2_Elevator",
            "Wing_Room0", "Wing_Room1", "Wing_Room2", "Wing_Room3"
        };

        Vector3[] positions = {
            V(0, 0.6f, -18),          // Start
            V(0, 0.6f, 0),            // Ground Center
            V(-22, 0.6f, 12),         // Ground NW
            V(-22, 0.6f, -12),        // Ground SW
            V(22, 0.6f, 12),          // Ground NE
            V(22, 0.6f, -12),         // Ground SE
            V(-8, 0.6f, 15),          // Ground Stairs
            V(8, 0.6f, 15),           // Ground Elevator
            V(0, FloorHeight + 0.6f, 0),      // F1 Center
            V(-13, FloorHeight + 0.6f, 8),    // F1 Room W
            V(0, FloorHeight + 0.6f, -8),     // F1 Room Center
            V(13, FloorHeight + 0.6f, 8),     // F1 Room E
            V(-8, FloorHeight + 0.6f, 15),    // F1 Stairs
            V(8, FloorHeight + 0.6f, 15),     // F1 Elevator
            V(-5, FloorHeight * 2 + 0.6f, 5),   // F2 Zone A
            V(5, FloorHeight * 2 + 0.6f, -5),   // F2 Zone B
            V(0, FloorHeight * 2 + 0.6f, -12),  // F2 Open
            V(-8, FloorHeight * 2 + 0.6f, 15),  // F2 Stairs
            V(8, FloorHeight * 2 + 0.6f, 15),   // F2 Elevator
            V(-15, 0.6f, 10),         // Wing 0
            V(-5, 0.6f, -10),         // Wing 1
            V(5, 0.6f, 10),           // Wing 2
            V(15, 0.6f, -10)          // Wing 3
        };

        if (!AssetDatabase.IsValidFolder("Assets/BenchmarkData"))
            AssetDatabase.CreateFolder("Assets", "BenchmarkData");

        for (int i = 0; i < pointNames.Length; i++)
        {
            var rp = ScriptableObject.CreateInstance<RoutePoint>();
            rp.pointName = pointNames[i];
            rp.worldPosition = positions[i];
            rp.floor = Mathf.FloorToInt(positions[i].y / FloorHeight) + 1;
            
            // Assign building codes based on location
            if (pointNames[i].Contains("Wing"))
                rp.buildingCode = "WING";
            else if (pointNames[i].Contains("F1"))
                rp.buildingCode = "MAIN_F1";
            else if (pointNames[i].Contains("F2"))
                rp.buildingCode = "MAIN_F2";
            else if (pointNames[i].Contains("Ground") || pointNames[i] == "Start")
                rp.buildingCode = "MAIN_F0";
            else
                rp.buildingCode = "MAIN";

            rp.category = pointNames[i].Contains("Room") || pointNames[i].Contains("Zone") 
                ? RoutePointCategory.Classroom 
                : RoutePointCategory.Corridor;
            rp.isAccessible = true;

            string safeName = pointNames[i].Replace(" ", "_").Replace("-", "_");
            string path = $"Assets/BenchmarkData/INB_{safeName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<RoutePoint>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(rp, existing);
                Object.DestroyImmediate(rp);
                rp = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(rp, path);
            }
            routeDb.allPoints.Add(rp);
        }

        string dbPath = "Assets/BenchmarkData/IndoorNavRouteDB.asset";
        var existingDb = AssetDatabase.LoadAssetAtPath<RouteDatabase>(dbPath);
        if (existingDb != null)
        {
            existingDb.allPoints = routeDb.allPoints;
            EditorUtility.SetDirty(existingDb);
            Object.DestroyImmediate(routeDb);
            routeDb = existingDb;
        }
        else
        {
            AssetDatabase.CreateAsset(routeDb, dbPath);
        }

        // ===== INDOOR BENCHMARK SYSTEM =====
        var benchObj = new GameObject("IndoorBenchmarkSystem");
        benchObj.transform.SetParent(root.transform);
        var benchmark = benchObj.AddComponent<PathfindingBenchmark>();

        var benchSO = new SerializedObject(benchmark);
        benchSO.FindProperty("routeDatabase").objectReferenceValue = routeDb;
        benchSO.FindProperty("maxConnectionDistance").floatValue = 12f; // Shorter for indoor
        benchSO.ApplyModifiedPropertiesWithoutUndo();

        // ===== INDOOR-SPECIFIC UI =====
        CreateIndoorUI(root, agent, benchmark, routeDb);

        // ===== LIGHTING =====
        var light = GameObject.Find("Directional Light");
        if (light == null)
        {
            light = new GameObject("Directional Light");
            var l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.0f;
            l.shadows = LightShadows.Soft;
        }
        light.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Add point lights inside building
        for (int f = 0; f < 3; f++)
        {
            float y = f * FloorHeight + 3f;
            for (int x = -20; x <= 20; x += 20)
            {
                var ptLight = new GameObject($"PointLight_F{f}_X{x}");
                ptLight.transform.SetParent(root.transform);
                ptLight.transform.position = V(x, y, 0);
                var pl = ptLight.AddComponent<Light>();
                pl.type = LightType.Point;
                pl.intensity = 2f;
                pl.range = 15f;
                pl.color = new Color(1f, 0.98f, 0.95f);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);

        Debug.Log("[IndoorNavigationBenchmark] Generated!\n" +
            "Features:\n" +
            "- 3-floor building with rooms, corridors, doorways\n" +
            "- Stairwell and elevator for vertical navigation\n" +
            "- Additional wing with maze-like room layout\n" +
            "- Furniture/obstacles for realistic pathfinding\n" +
            "- 23 navigation waypoints across all floors\n\n" +
            "Next steps:\n" +
            "1. Select 'IndoorNavMesh' > Inspector > Bake\n" +
            "2. Press Play\n" +
            "3. Use the UI to test indoor navigation scenarios");
    }

    static void CreateIndoorUI(GameObject root, UnityEngine.AI.NavMeshAgent agent, 
        PathfindingBenchmark benchmark, RouteDatabase routeDb)
    {
        // Create Canvas
        var canvasObj = new GameObject("IndoorBenchmarkCanvas");
        canvasObj.transform.SetParent(root.transform);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
            UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // EventSystem
        var existingES = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (existingES != null) Undo.DestroyObjectImmediate(existingES.gameObject);

        var esObj = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var inputModule = esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        var actionsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(
            "Assets/InputSystem_Actions.inputactions");
        if (actionsAsset != null)
        {
            var moduleSO = new SerializedObject(inputModule);
            moduleSO.FindProperty("m_ActionsAsset").objectReferenceValue = actionsAsset;
            moduleSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // Add BenchmarkUI component
        var benchUI = canvasObj.AddComponent<BenchmarkUI>();
        var uiSO = new SerializedObject(benchUI);
        uiSO.FindProperty("benchmark").objectReferenceValue = benchmark;
        uiSO.FindProperty("playerAgent").objectReferenceValue = agent;
        uiSO.FindProperty("routeDatabase").objectReferenceValue = routeDb;

        // Set indoor-specific destination data
        Vector3[] destinations = {
            V(-22, 0.6f, 12),   // Ground NW Room
            V(0, FloorHeight + 0.6f, -8),    // F1 Center Room
            V(5, FloorHeight * 2 + 0.6f, -5), // F2 Zone B
            V(-5, 0.6f, 10)     // Wing Room 0
        };
        
        string[] destNames = {
            "Ground NW Room",
            "1st Floor Center",
            "2nd Floor Zone B", 
            "Wing Room 0"
        };

        var destPos = uiSO.FindProperty("destinationPositions");
        destPos.arraySize = destinations.Length;
        for (int i = 0; i < destinations.Length; i++)
            destPos.GetArrayElementAtIndex(i).vector3Value = destinations[i];

        var destNm = uiSO.FindProperty("destinationNames");
        destNm.arraySize = destNames.Length;
        for (int i = 0; i < destNames.Length; i++)
            destNm.GetArrayElementAtIndex(i).stringValue = destNames[i];

        uiSO.ApplyModifiedPropertiesWithoutUndo();
    }

    // ===== HELPER FUNCTIONS =====

    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

    static GameObject Child(GameObject parent, string name)
    {
        var c = new GameObject(name);
        c.transform.SetParent(parent.transform);
        return c;
    }

    static GameObject CreateFloorSlab(GameObject parent, string name, Vector3 pos, float w, float d, Color col)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent.transform);
        obj.transform.position = pos;
        obj.transform.localScale = V(w, 0.1f, d);
        SetMaterial(obj, col);
        return obj;
    }

    static void CreateWall(GameObject parent, string name, Vector3 pos, float w, float h, float d, Color col)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent.transform);
        obj.transform.position = pos;
        obj.transform.localScale = V(w, h, d);
        SetMaterial(obj, col);
        obj.isStatic = true;
    }

    static void CreateDoorway(GameObject parent, string name, Vector3 pos, Color col)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent.transform);
        obj.transform.position = pos;
        obj.transform.localScale = V(DoorWidth, 0.05f, WallThickness + 0.1f);
        SetMaterial(obj, col);
        obj.isStatic = true;
    }

    static void CreateStairwell(GameObject parent, string name, Vector3 pos, float height, Color col)
    {
        // Stair base
        var stairObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stairObj.name = name;
        stairObj.transform.SetParent(parent.transform);
        stairObj.transform.position = pos + V(0, height / 2, 0);
        stairObj.transform.localScale = V(3f, height, 4f);
        SetMaterial(stairObj, col);
        stairObj.isStatic = true;

        // Stair steps visual
        int steps = Mathf.FloorToInt(height / 0.18f);
        for (int i = 0; i < steps; i++)
        {
            float stepY = i * 0.18f;
            float stepZ = i * 0.25f;
            var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = $"{name}_Step{i}";
            step.transform.SetParent(parent.transform);
            step.transform.position = pos + V(0, stepY + 0.09f, -1.5f + stepZ);
            step.transform.localScale = V(2.5f, 0.18f, 0.3f);
            SetMaterial(step, new Color(col.r * 0.9f, col.g * 0.9f, col.b * 0.9f));
            step.isStatic = true;
        }
    }

    static void CreateElevatorShaft(GameObject parent, string name, Vector3 pos, float w, float d, Color col)
    {
        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shaft.name = name;
        shaft.transform.SetParent(parent.transform);
        shaft.transform.position = pos + V(0, FloorHeight * 1.5f, 0);
        shaft.transform.localScale = V(w, FloorHeight * 3, d);
        SetMaterial(shaft, col);
        shaft.isStatic = true;
    }

    static void AddNavMeshLink(GameObject parent, string name, Vector3 start, Vector3 end, int f1, int f2)
    {
        var linkObj = new GameObject(name);
        linkObj.transform.SetParent(parent.transform);
        linkObj.transform.position = start;

        var link = linkObj.AddComponent<NavMeshLink>();
        link.startPoint = Vector3.zero;
        link.endPoint = end - start;
        link.width = 2.5f;
        link.bidirectional = true;

        var el = linkObj.AddComponent<ElevatorLink>();
        el.Initialize("INDOOR", f1, f2);
    }

    static void CreateBox(GameObject parent, string name, Vector3 pos, float w, float h, float d, Color col)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent.transform);
        obj.transform.position = pos;
        obj.transform.localScale = V(w, h, d);
        SetMaterial(obj, col);
        obj.isStatic = true;
    }

    static GameObject CreateMarker(GameObject parent, string name, Vector3 pos, Color col, float radius)
    {
        var m = new GameObject(name);
        m.transform.SetParent(parent.transform);
        m.transform.position = pos;

        var vis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        vis.name = "Visual";
        vis.transform.SetParent(m.transform);
        vis.transform.position = pos;
        vis.transform.localScale = V(radius, 0.3f, radius);
        SetMaterial(vis, col);
        Object.DestroyImmediate(vis.GetComponent<Collider>());
        return m;
    }

    static void SetMaterial(GameObject obj, Color col)
    {
        var r = obj.GetComponent<Renderer>();
        if (r == null) return;
        
        // Create material with URP Lit shader - keep it simple like other generators
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", col);
        r.sharedMaterial = m;
    }

    // Floor visibility controller - makes floors above/below player transparent
    public class FloorVisibilityController : MonoBehaviour
    {
        [Tooltip("List of floor groups (each group contains all objects for one floor level)")]
        public GameObject[] floorGroups = new GameObject[0];
        
        [Tooltip("Transparency alpha for inactive floors (0.0 = invisible, 1.0 = fully visible)")]
        [Range(0.0f, 1.0f)]
        public float inactiveAlpha = 0.15f;
        
        [Tooltip("Alpha for active floor (should be 1.0)")]
        [Range(0.0f, 1.0f)]
        public float activeAlpha = 1.0f;
        
        private Transform playerTransform;
        private int currentFloor = 0;
        private const float FloorHeight = 3.5f;
        private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
        private Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();
        
        void Start()
        {
            playerTransform = transform;
            CacheOriginalMaterials();
            UpdateFloorVisibility();
        }
        
        void LateUpdate()
        {
            UpdateFloorVisibility();
        }
        
        void CacheOriginalMaterials()
        {
            originalColors.Clear();
            originalMaterials.Clear();
            
            foreach (GameObject floorGroup in floorGroups)
            {
                if (floorGroup == null) continue;
                
                Renderer[] renderers = floorGroup.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer rend in renderers)
                {
                    if (rend != null && rend.sharedMaterial != null)
                    {
                        originalColors[rend] = rend.sharedMaterial.color;
                        originalMaterials[rend] = rend.sharedMaterial;
                    }
                }
            }
        }
        
        void UpdateFloorVisibility()
        {
            if (playerTransform == null) return;
            
            float playerY = playerTransform.position.y;
            int newFloor = Mathf.FloorToInt(playerY / FloorHeight);
            newFloor = Mathf.Clamp(newFloor, 0, floorGroups.Length - 1);
            
            if (newFloor != currentFloor)
            {
                currentFloor = newFloor;
                ApplyVisibility();
            }
        }
        
        void ApplyVisibility()
        {
            for (int i = 0; i < floorGroups.Length; i++)
            {
                if (floorGroups[i] == null) continue;
                
                bool isActiveFloor = (i == currentFloor);
                float targetAlpha = isActiveFloor ? activeAlpha : inactiveAlpha;
                
                Renderer[] renderers = floorGroups[i].GetComponentsInChildren<Renderer>(true);
                foreach (Renderer rend in renderers)
                {
                    if (rend == null || rend.sharedMaterial == null) continue;
                    
                    // Get original color
                    Color originalColor;
                    if (originalColors.TryGetValue(rend, out originalColor))
                    {
                        // Create material instance for this renderer
                        Material mat = rend.material;
                        
                        // Set alpha
                        mat.color = new Color(originalColor.r, originalColor.g, originalColor.b, targetAlpha);
                        
                        // Configure transparency
                        if (targetAlpha < 1.0f)
                        {
                            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            mat.SetInt("_ZWrite", 0);
                            mat.DisableKeyword("_ALPHATEST_ON");
                            mat.EnableKeyword("_ALPHABLEND_ON");
                            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            mat.renderQueue = 3000;
                        }
                        else
                        {
                            // Reset to opaque
                            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                            mat.SetInt("_ZWrite", 1);
                            mat.DisableKeyword("_ALPHABLEND_ON");
                            mat.renderQueue = -1;
                        }
                    }
                }
            }
        }
    }
}
