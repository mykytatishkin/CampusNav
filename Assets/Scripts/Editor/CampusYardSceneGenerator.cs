using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Unity.AI.Navigation;

/// <summary>
/// Generates the Campus Yard scene with separate indoor floor plans per building.
///
/// Flow:
/// 1. MainMenu → CampusSelect → DestinationSelect (pick entrance) → StartPoint → Navigation
/// 2. CampusNavigator auto-navigates player to entrance
/// 3. EntranceTrigger → IndoorManager teleports player to indoor area (Y=-100)
/// 4. FloorSelectUI shows floors + rooms → user picks room → NavMeshAgent navigates inside
/// 5. Stairs = NavMeshLinks between floors in indoor area
/// </summary>
public static class CampusYardSceneGenerator
{
    // Yard terrain constants (same as CampusSceneGenerator)
    const float F = 3.5f;
    const float WallT = 0.3f;
    const float TerrainNorthZ = 90f;
    const float TerrainSouthZ = -55f;
    const int TerrainLevels = 6;
    const float StepHeight = 1.5f;
    const float BoundaryPlankDepthZ = 3.2f;
    const float PlankThickness = 0.14f;
    const float PathLengthOverlap = 0.2f;
    const float DoorwayWidth = 4f;

    // Indoor constants
    const float IndoorBaseY = -100f;
    const float IndoorSpacingX = 120f;
    const float CorridorWidth = 4f;
    const float RoomDoorWidth = 1.8f;
    const float RoomDoorHeight = 2.5f;
    const float IndoorWall = 0.15f;

    static float TerrainBandWidth() => (TerrainNorthZ - TerrainSouthZ) / TerrainLevels;

    // Flat terrain — all buildings on the same level.
    static float TerrainY(float z) => 0f;

    struct BldDef
    {
        public string Code, Display;
        public Vector3 Pos, EntrancePos;
        public float W, D;
        public int Floors, RoomsPerSide;
    }

    // ============================================================
    //  Building layout based on the REAL Saulėtekio rūmai campus map.
    //  Coordinate: +X = East, +Z = North. Terrain rises south→north.
    //  "Jūs esate čia" (blue dot) = south-east corner, player start.
    //
    //  Map (top = North):
    //         [P - North road]
    //                   S4          S6   S7
    //              S5         (corridors)
    //                    S2
    //         [P]             [P]
    //              S1         S3
    //                    [P]  . ← "Jūs esate čia"
    //
    //  S1(SRC)  = south-west,  large square
    //  S3(SRK-I)= south-east,  long east-west building
    //  S2(SRA-I)= center-east, between S3 and S4
    //  S5(SRK-II)= center-west, left of S2
    //  S4(SRA-II)= north-center, above S2
    //  S6(SRL-II)= north-east, above S4
    //  S7(SRL-I) = far north-east, east of S6
    // ============================================================
    static readonly BldDef[] BldDefs =
    {
        // S1 - SRC: Centriniai rūmai (Central/Admin). South-west. Large square. Entrance from south (▲).
        new() { Code = "SRC",     Display = "S1 - Centriniai rūmai",
                W = 38, D = 35, Floors = 3, RoomsPerSide = 5,
                Pos = V(-15, 0, -25),  EntrancePos = V(-15, 0, -43) },

        // S2 - SRA-I: Auditorinis korpusas. Center-right, east of S5. Entrance from east (▶).
        new() { Code = "SRA-I",   Display = "S2 - Auditorinis korpusas",
                W = 28, D = 30, Floors = 4, RoomsPerSide = 5,
                Pos = V(25, 0, 15),   EntrancePos = V(39, 0, 15) },

        // S3 - SRK-I: Mokomasis korpusas. South-east, long building running E-W. Entrance from south (▲).
        new() { Code = "SRK-I",   Display = "S3 - Mokomasis korpusas",
                W = 55, D = 18, Floors = 4, RoomsPerSide = 7,
                Pos = V(35, 0, -25),  EntrancePos = V(35, 0, -34) },

        // S4 - SRA-II: Auditorinis korpusas II (Sustainability Hub). North-center, above S2.
        new() { Code = "SRA-II",  Display = "S4 - Auditorinis korpusas II",
                W = 25, D = 22, Floors = 3, RoomsPerSide = 4,
                Pos = V(18, 0, 50),   EntrancePos = V(18, 0, 39) },

        // S5 - SRK-II: Mokomasis korpusas II (Env. Engineering). Center-west, left of S2.
        new() { Code = "SRK-II",  Display = "S5 - Mokomasis korpusas II",
                W = 20, D = 30, Floors = 3, RoomsPerSide = 4,
                Pos = V(-10, 0, 20),  EntrancePos = V(-20, 0, 20) },

        // S6 - SRL-II: Laboratorinis korpusas (Fund. Sciences). North-east, above S4.
        new() { Code = "SRL-II",  Display = "S6 - Laboratorinis korpusas II",
                W = 22, D = 20, Floors = 3, RoomsPerSide = 4,
                Pos = V(28, 0, 72),   EntrancePos = V(28, 0, 62) },

        // S7 - SRL-I: Laboratorinis korpusas. Far north-east, east of S6.
        new() { Code = "SRL-I",   Display = "S7 - Laboratorinis korpusas I",
                W = 40, D = 18, Floors = 6, RoomsPerSide = 5,
                Pos = V(60, 0, 72),   EntrancePos = V(60, 0, 63) },
    };

    [MenuItem("CampusNav/Generate Campus Yard Scene")]
    static void Generate()
    {
        if (!EditorUtility.DisplayDialog("Generate Campus Yard",
            "Generate Campus Yard with indoor floor plans?\nExisting 'CampusYard' will be replaced.",
            "Generate", "Cancel"))
            return;

        var old = GameObject.Find("CampusYard");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("CampusYard");
        Undo.RegisterCreatedObjectUndo(root, "Generate Campus Yard");

        // Apply terrain Y
        var blds = new List<BldDef>();
        foreach (var b in BldDefs)
        {
            var c = b;
            c.Pos = V(b.Pos.x, TerrainY(b.Pos.z), b.Pos.z);
            c.EntrancePos = V(b.EntrancePos.x, TerrainY(b.EntrancePos.z), b.EntrancePos.z);
            blds.Add(c);
        }

        // ==================== OUTDOOR (flat ground) ====================
        FlatGround(root, 220, 220);

        // ===== OUTDOOR PATHS =====
        var paths = Child(root, "OutdoorPaths");
        // North road
        PathEW(paths, "Road_North", 20, 85, 140, 8);
        // South road / parking approach
        PathEW(paths, "Road_South", 15, -48, 120, 7);
        // West N-S walkway (past SRC, SRK-II)
        PathNS(paths, "Walk_WestNS", -35, -48, 30, 6);
        // Central N-S (S3 up through S2 to S4, S6, S7)
        PathNS(paths, "Walk_CenterNS", 25, -40, 80, 7);
        // East N-S (along S3 east side going north)
        PathNS(paths, "Walk_EastNS", 62, -45, 80, 5);
        // South cross (connecting S1 to S3)
        PathEW(paths, "Walk_SouthCross", 10, -30, 80, 6);
        // Mid cross (connecting S5 to S2)
        PathEW(paths, "Walk_MidCross", 5, 18, 70, 6);
        // North cross (connecting S4 to S6/S7)
        PathEW(paths, "Walk_NorthCross", 35, 70, 65, 5);
        // Path to S1 entrance (south approach)
        PathNS(paths, "Walk_toS1", -15, -48, -43, 5);
        // Path to S3 entrance (south approach)
        PathNS(paths, "Walk_toS3", 35, -45, -34, 5);
        // Parking west approach
        PathNS(paths, "Walk_ParkingW", -40, -5, 18, 5);
        // Path from blue dot area to campus
        PathEW(paths, "Walk_BlueDot", 25, -48, 50, 6);

        // ===== BUILDINGS =====
        var bldGroup = Child(root, "Buildings");
        foreach (var b in blds)
            OutdoorBuilding(bldGroup, b.Code, b.Pos, b.W, b.D, b.Floors);

        // ===== CORRIDORS =====
        var corGroup = Child(root, "Corridors");
        // S1(SRC) ↔ S3(SRK-I): east-west at south, ground floor
        Corridor(corGroup, "Cor_S1_S3", V(4, TerrainY(-25), -25), V(8, TerrainY(-25), -25), 5, F);
        // S5(SRK-II) ↔ S2(SRA-I): east-west, ground floor
        Corridor(corGroup, "Cor_S5_S2", V(0, TerrainY(18), 18), V(11, TerrainY(18), 18), 5, F);
        // S2(SRA-I) ↔ S3(SRK-I): north-south, connecting center
        Corridor(corGroup, "Cor_S2_S3", V(28, TerrainY(0), 0), V(28, TerrainY(-16), -16), 4, F);
        // S2(SRA-I) ↔ S4(SRA-II): north-south
        Corridor(corGroup, "Cor_S2_S4", V(22, TerrainY(30), 30), V(22, TerrainY(39), 39), 4, F);
        // S4(SRA-II) ↔ S6(SRL-II): north
        Corridor(corGroup, "Cor_S4_S6", V(25, TerrainY(61), 61), V(25, TerrainY(62), 62), 4, F);
        // S6(SRL-II) ↔ S7(SRL-I): east-west
        Corridor(corGroup, "Cor_S6_S7", V(39, TerrainY(72), 72), V(40, TerrainY(72), 72), 4, F);

        // ===== ENTRANCE TRIGGERS & MARKERS =====
        var trigGroup = Child(root, "EntranceTriggers");
        var markGroup = Child(root, "EntranceMarkers");
        foreach (var b in blds)
        {
            CreateEntranceTrigger(trigGroup, b);
            CreateEntranceMarker(markGroup, b);
        }

        // ===== DECORATIONS =====
        var deco = Child(root, "Decorations");
        // West parking (between S1 and S5)
        Box(deco, "Parking_West", V(-38, TerrainY(0)+0.05f, 0), V(16, 0.08f, 18), Col(0.35f,0.35f,0.38f));
        // South parking (below S1 and S3)
        Box(deco, "Parking_South", V(20, TerrainY(-52)+0.05f, -52), V(45, 0.08f, 10), Col(0.35f,0.35f,0.38f));
        // North parking
        Box(deco, "Parking_North", V(0, TerrainY(88)+0.05f, 88), V(30, 0.08f, 8), Col(0.35f,0.35f,0.38f));
        // Trees along west side
        for (int i = 0; i < 10; i++)
            Tree(deco, $"Tree_W{i}", V(-42, TerrainY(-40+i*12), -40+i*12));
        // Trees along east side
        for (int i = 0; i < 6; i++)
            Tree(deco, $"Tree_E{i}", V(78, TerrainY(-30+i*18), -30+i*18));

        // ==================== INDOOR AREAS (Y=-100, spaced along X) ====================
        var indoorRoot = Child(root, "IndoorAreas");
        var indoorDataList = new List<IndoorBuildingData>();
        var indoorGOs = new List<GameObject>();

        for (int bi = 0; bi < blds.Count; bi++)
        {
            var b = blds[bi];
            // Indoor area uses the building's actual dimensions for realistic proportions
            float indoorLen = b.W;
            float indoorWid = Mathf.Max(b.D, 14f); // min 14m wide for corridor + rooms
            Vector3 origin = V(bi * IndoorSpacingX, IndoorBaseY, 0);

            var data = new IndoorBuildingData
            {
                buildingCode = b.Code,
                displayName = b.Display,
                indoorOrigin = origin,
                floors = b.Floors,
                buildingLength = indoorLen,
                buildingWidth = indoorWid,
                corridorWidth = CorridorWidth,
                floorHeight = F,
                roomsPerSide = b.RoomsPerSide,
                yardEntrancePosition = b.EntrancePos,
            };
            indoorDataList.Add(data);
            indoorGOs.Add(GenerateIndoorArea(indoorRoot, data));
        }

        // ==================== PLAYER ====================
        var player = new GameObject("Player");
        player.transform.SetParent(root.transform);
        // "Jūs esate čia" — blue dot, south-east of campus (below S3)
        player.transform.position = V(45, 1.2f, -48);
        player.tag = "Player";

        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "PlayerModel";
        capsule.transform.SetParent(player.transform);
        capsule.transform.localPosition = V(0, 1, 0);
        capsule.transform.localScale = V(0.8f, 1, 0.8f);
        SetMat(capsule, Col(0.2f, 0.6f, 1));
        capsule.tag = "Player";

        var rb = player.AddComponent<Rigidbody>();
        rb.isKinematic = true; rb.useGravity = false;

        var agent = player.AddComponent<NavMeshAgent>();
        agent.speed = 3.5f; agent.radius = 0.5f; agent.height = 2f;
        agent.acceleration = 12; agent.angularSpeed = 360;
        agent.stoppingDistance = 0.5f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        // ==================== CAMERA ====================
        var camObj = GameObject.Find("Main Camera");
        if (camObj == null) { camObj = new GameObject("Main Camera"); camObj.AddComponent<Camera>(); camObj.tag = "MainCamera"; }
        var cam = camObj.GetComponent<Camera>();
        if (cam) { cam.farClipPlane = 500; cam.nearClipPlane = 0.3f; }

        var camCtrl = camObj.GetComponent<CampusCameraController>() ?? camObj.AddComponent<CampusCameraController>();
        camCtrl.SwitchMode(CampusCameraController.CameraMode.Free);
        camCtrl.FocusOn(V(45, 1, -48));
        camCtrl.SetOrbit(45, 60, 60);
        Wire(camCtrl, "target", player.transform);

        // ==================== NAVIGATOR ====================
        var navMgr = new GameObject("NavigatorManager");
        navMgr.transform.SetParent(root.transform);
        var navigator = navMgr.AddComponent<CampusNavigator>();

        var lr = navMgr.AddComponent<LineRenderer>();
        lr.startWidth = 0.4f; lr.endWidth = 0.4f; lr.positionCount = 0;
        var lrMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        lrMat.color = Col(0.1f, 0.7f, 1);
        lr.sharedMaterial = lrMat;

        var arrow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        arrow.name = "PathArrow"; arrow.transform.SetParent(navMgr.transform);
        arrow.transform.localScale = V(0.6f, 0.6f, 0.6f);
        SetMat(arrow, Col(1, 0.4f, 0.1f)); arrow.SetActive(false);

        var navSO = new SerializedObject(navigator);
        navSO.FindProperty("agent").objectReferenceValue = agent;
        navSO.FindProperty("lineRenderer").objectReferenceValue = lr;
        navSO.FindProperty("arrow").objectReferenceValue = arrow.transform;
        navSO.ApplyModifiedPropertiesWithoutUndo();

        // ==================== GEO SYSTEMS ====================
        var geoObj = new GameObject("GeoAnchorSystem");
        geoObj.transform.SetParent(root.transform);
        var geoAnchor = geoObj.AddComponent<GeoAnchorSystem>();

        var geoTracker = player.AddComponent<PlayerGeoTracker>();
        var tSO = new SerializedObject(geoTracker);
        tSO.FindProperty("geoAnchor").objectReferenceValue = geoAnchor;
        tSO.FindProperty("agent").objectReferenceValue = agent;
        tSO.FindProperty("cameraController").objectReferenceValue = camCtrl;
        tSO.ApplyModifiedPropertiesWithoutUndo();

        // ==================== INDOOR MANAGER ====================
        var indoorMgrObj = new GameObject("IndoorManager");
        indoorMgrObj.transform.SetParent(root.transform);
        var indoorMgr = indoorMgrObj.AddComponent<IndoorManager>();

        var imSO = new SerializedObject(indoorMgr);
        imSO.FindProperty("playerAgent").objectReferenceValue = agent;
        imSO.FindProperty("cameraController").objectReferenceValue = camCtrl;
        imSO.FindProperty("navigator").objectReferenceValue = navigator;

        // Populate buildings list
        var bldListProp = imSO.FindProperty("buildings");
        bldListProp.arraySize = indoorDataList.Count;
        for (int i = 0; i < indoorDataList.Count; i++)
        {
            var elem = bldListProp.GetArrayElementAtIndex(i);
            var d = indoorDataList[i];
            elem.FindPropertyRelative("buildingCode").stringValue = d.buildingCode;
            elem.FindPropertyRelative("displayName").stringValue = d.displayName;
            elem.FindPropertyRelative("indoorOrigin").vector3Value = d.indoorOrigin;
            elem.FindPropertyRelative("floors").intValue = d.floors;
            elem.FindPropertyRelative("buildingLength").floatValue = d.buildingLength;
            elem.FindPropertyRelative("buildingWidth").floatValue = d.buildingWidth;
            elem.FindPropertyRelative("corridorWidth").floatValue = d.corridorWidth;
            elem.FindPropertyRelative("floorHeight").floatValue = d.floorHeight;
            elem.FindPropertyRelative("roomsPerSide").intValue = d.roomsPerSide;
            elem.FindPropertyRelative("yardEntrancePosition").vector3Value = d.yardEntrancePosition;
        }

        // Populate indoor root references
        var rootsProp = imSO.FindProperty("indoorRoots");
        rootsProp.arraySize = indoorGOs.Count;
        for (int i = 0; i < indoorGOs.Count; i++)
            rootsProp.GetArrayElementAtIndex(i).objectReferenceValue = indoorGOs[i];

        imSO.ApplyModifiedPropertiesWithoutUndo();

        // ==================== UI ====================
        var canvasObj = new GameObject("UI_Canvas");
        canvasObj.transform.SetParent(root.transform);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        EnsureEventSystem();

        var mm = Screen(canvasObj, "MainMenuScreen").AddComponent<MainMenuScreen>();
        var cs = Screen(canvasObj, "CampusSelectScreen").AddComponent<CampusSelectScreen>();
        var ds = Screen(canvasObj, "DestinationSelectScreen").AddComponent<DestinationSelectScreen>();
        var sp = Screen(canvasObj, "StartPointScreen").AddComponent<StartPointScreen>();
        var ns = Screen(canvasObj, "NavigationScreen").AddComponent<NavigationScreen>();
        var fs = Screen(canvasObj, "FloorSelectUI").AddComponent<FloorSelectUI>();

        // ==================== APP FLOW MANAGER ====================
        var flowObj = new GameObject("AppFlowManager");
        flowObj.transform.SetParent(root.transform);
        var flow = flowObj.AddComponent<AppFlowManager>();

        // CampusData
        string cdPath = "Assets/Resources/MainCampusData.asset";
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        var cd = AssetDatabase.LoadAssetAtPath<CampusData>(cdPath);
        if (cd == null) { cd = ScriptableObject.CreateInstance<CampusData>(); AssetDatabase.CreateAsset(cd, cdPath); }
        cd.campusName = "Vilnius Tech - Saulėtekio Rūmai";
        cd.campusCode = "SR";
        cd.address = "Saulėtekio al. 11, Vilnius";
        cd.buildings ??= new List<BuildingInfo>();
        cd.buildings.Clear();
        foreach (var b in blds)
            cd.buildings.Add(new BuildingInfo { code = b.Code, displayName = b.Display, floors = b.Floors, worldPosition = b.Pos });
        var rdGuids = AssetDatabase.FindAssets("t:RouteDatabase");
        if (rdGuids.Length > 0)
            cd.routeDatabase = AssetDatabase.LoadAssetAtPath<RouteDatabase>(AssetDatabase.GUIDToAssetPath(rdGuids[0]));
        EditorUtility.SetDirty(cd);

        var fSO = new SerializedObject(flow);
        fSO.FindProperty("mainMenuScreen").objectReferenceValue = mm;
        fSO.FindProperty("campusSelectScreen").objectReferenceValue = cs;
        fSO.FindProperty("destinationSelectScreen").objectReferenceValue = ds;
        fSO.FindProperty("startPointScreen").objectReferenceValue = sp;
        fSO.FindProperty("navigationScreen").objectReferenceValue = ns;
        fSO.FindProperty("indoorSelectScreen").objectReferenceValue = fs;
        fSO.FindProperty("navigator").objectReferenceValue = navigator;
        fSO.FindProperty("geoAnchor").objectReferenceValue = geoAnchor;
        fSO.FindProperty("playerTracker").objectReferenceValue = geoTracker;
        fSO.FindProperty("cameraController").objectReferenceValue = camCtrl;
        fSO.FindProperty("campusWorldRoot").objectReferenceValue = root;
        var arr = fSO.FindProperty("availableCampuses");
        arr.arraySize = 1;
        arr.GetArrayElementAtIndex(0).objectReferenceValue = cd;
        fSO.ApplyModifiedPropertiesWithoutUndo();

        // ==================== NAVMESH ====================
        var navSurface = new GameObject("NavMeshSurface");
        navSurface.transform.SetParent(root.transform);
        var surf = navSurface.AddComponent<NavMeshSurface>();
        surf.collectObjects = CollectObjects.All;

        // ==================== LIGHTING ====================
        var lightObj = GameObject.Find("Directional Light");
        if (lightObj == null) { lightObj = new GameObject("Directional Light"); var l = lightObj.AddComponent<Light>(); l.type = LightType.Directional; l.intensity = 1.2f; l.shadows = LightShadows.Soft; }
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        // ==================== DONE ====================
        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        AssetDatabase.SaveAssets();

        Debug.Log("[CampusYard] Scene generated with indoor floor plans!\n" +
                  "1. Run 'CampusNav > Generate All Route Points' if needed\n" +
                  "2. Bake NavMesh: Window > AI > Navigation > Bake\n" +
                  "3. Press Play — navigate to an entrance, get teleported inside");
    }

    // ============================================================
    //  INDOOR AREA GENERATION
    //  Each building gets a full indoor floor plan at Y=-100
    //  Layout per floor:
    //    +------+------+------+------+------+------+
    //    |Room L|Room L|Room L|Stairs|Room L|Room L|  <- left side
    //    +------+------+------+------+------+------+
    //    |        C O R R I D O R                   |
    //    +------+------+------+------+------+------+
    //    |Room R|Room R|Room R|Stairs|Room R|Room R|  <- right side
    //    +------+------+------+------+------+------+
    // ============================================================

    static GameObject GenerateIndoorArea(GameObject parent, IndoorBuildingData data)
    {
        var bldRoot = new GameObject($"Indoor_{data.buildingCode}");
        bldRoot.transform.SetParent(parent.transform);
        bldRoot.transform.position = data.indoorOrigin;
        bldRoot.isStatic = true;

        float len = data.buildingLength;
        float wid = data.buildingWidth;
        float corW = data.corridorWidth;
        float roomDepth = (wid - corW) / 2f - IndoorWall;
        float roomWidth = len / data.roomsPerSide;
        int stairSlot = data.roomsPerSide / 2; // middle slot is stairs

        for (int floor = 0; floor < data.floors; floor++)
        {
            float fy = floor * data.floorHeight;
            var floorRoot = new GameObject($"Floor_{floor + 1}");
            floorRoot.transform.SetParent(bldRoot.transform);
            floorRoot.transform.localPosition = V(0, fy, 0);
            floorRoot.isStatic = true;

            // Floor slab
            Box(floorRoot, "Slab", V(0, 0.05f, 0), V(len + 0.4f, 0.1f, wid + 0.4f), Col(0.88f, 0.86f, 0.82f));

            // No ceiling — the next floor's slab acts as the ceiling

            // Outer walls
            Box(floorRoot, "Wall_N", V(0, F / 2, wid / 2), V(len, F, IndoorWall), Col(0.82f, 0.8f, 0.78f));
            Box(floorRoot, "Wall_S", V(0, F / 2, -wid / 2), V(len, F, IndoorWall), Col(0.82f, 0.8f, 0.78f));

            // East wall with entrance doorway on floor 0
            if (floor == 0)
            {
                float sideH = (F - RoomDoorHeight);
                float sideW = (wid - RoomDoorWidth) / 2;
                Box(floorRoot, "Wall_E_Top", V(len / 2, F - sideH / 2, 0), V(IndoorWall, sideH, wid), Col(0.82f, 0.8f, 0.78f));
                Box(floorRoot, "Wall_E_L", V(len / 2, RoomDoorHeight / 2, -sideW / 2 - RoomDoorWidth / 2), V(IndoorWall, RoomDoorHeight, sideW), Col(0.82f, 0.8f, 0.78f));
                Box(floorRoot, "Wall_E_R", V(len / 2, RoomDoorHeight / 2, sideW / 2 + RoomDoorWidth / 2), V(IndoorWall, RoomDoorHeight, sideW), Col(0.82f, 0.8f, 0.78f));
            }
            else
                Box(floorRoot, "Wall_E", V(len / 2, F / 2, 0), V(IndoorWall, F, wid), Col(0.82f, 0.8f, 0.78f));

            Box(floorRoot, "Wall_W", V(-len / 2, F / 2, 0), V(IndoorWall, F, wid), Col(0.82f, 0.8f, 0.78f));

            // Corridor floor (slightly different color)
            Box(floorRoot, "CorridorFloor", V(0, 0.11f, 0), V(len - 0.2f, 0.02f, corW), Col(0.78f, 0.76f, 0.72f));

            // Room dividers and labels for LEFT side (negative Z)
            float leftZ = -(corW / 2 + roomDepth / 2 + IndoorWall / 2);
            float rightZ = corW / 2 + roomDepth / 2 + IndoorWall / 2;

            // Corridor walls (separating rooms from corridor) with door gaps
            GenerateCorridorWall(floorRoot, "CorWall_Left", len, corW / 2 + IndoorWall / 2, -1, data.roomsPerSide, roomWidth, stairSlot);
            GenerateCorridorWall(floorRoot, "CorWall_Right", len, corW / 2 + IndoorWall / 2, 1, data.roomsPerSide, roomWidth, stairSlot);

            // Room dividers + labels
            for (int r = 0; r < data.roomsPerSide; r++)
            {
                float rx = -len / 2 + roomWidth * r + roomWidth / 2;
                bool isStair = (r == 0 || r == stairSlot);

                if (isStair)
                {
                    // Stairwell marker
                    Box(floorRoot, $"Stair_{r}_L", V(rx, 0.12f, leftZ), V(roomWidth - 0.4f, 0.04f, roomDepth - 0.4f), Col(0.6f, 0.55f, 0.5f));
                    Box(floorRoot, $"Stair_{r}_R", V(rx, 0.12f, rightZ), V(roomWidth - 0.4f, 0.04f, roomDepth - 0.4f), Col(0.6f, 0.55f, 0.5f));

                    // Stair label
                    AddLabel(floorRoot, $"StairLabel_{r}", V(rx, 2f, 0), r == 0 ? "Stairs A" : "Stairs B", Col(0.9f, 0.3f, 0.3f));
                }
                else
                {
                    // Room floors (colored per type)
                    string labelL = IndoorManager.GetRoomLabel(data.buildingCode, floor + 1, r, true);
                    string labelR = IndoorManager.GetRoomLabel(data.buildingCode, floor + 1, r, false);

                    Color roomColL = RoomColor(floor + 1, r, true);
                    Color roomColR = RoomColor(floor + 1, r, false);

                    Box(floorRoot, $"RoomFloor_L{r}", V(rx, 0.12f, leftZ), V(roomWidth - 0.4f, 0.02f, roomDepth - 0.4f), roomColL);
                    Box(floorRoot, $"RoomFloor_R{r}", V(rx, 0.12f, rightZ), V(roomWidth - 0.4f, 0.02f, roomDepth - 0.4f), roomColR);

                    // Room labels
                    AddLabel(floorRoot, $"Label_L{r}", V(rx, 1.8f, leftZ), labelL, Color.white);
                    AddLabel(floorRoot, $"Label_R{r}", V(rx, 1.8f, rightZ), labelR, Color.white);
                }

                // Room divider walls (between adjacent rooms)
                if (r > 0)
                {
                    float divX = -len / 2 + roomWidth * r;
                    Box(floorRoot, $"Div_L{r}", V(divX, F / 2, leftZ), V(IndoorWall, F, roomDepth), Col(0.85f, 0.83f, 0.8f));
                    Box(floorRoot, $"Div_R{r}", V(divX, F / 2, rightZ), V(IndoorWall, F, roomDepth), Col(0.85f, 0.83f, 0.8f));
                }
            }

            // Stairwell NavMeshLinks to next floor
            if (floor < data.floors - 1)
            {
                for (int s = 0; s < 2; s++)
                {
                    int slot = (s == 0) ? 0 : stairSlot;
                    float sx = -len / 2 + roomWidth * slot + roomWidth / 2;

                    var linkObj = new GameObject($"StairLink_F{floor + 1}to{floor + 2}_{s}");
                    linkObj.transform.SetParent(floorRoot.transform);
                    linkObj.transform.localPosition = V(sx, 0.1f, 0);

                    var link = linkObj.AddComponent<NavMeshLink>();
                    link.startPoint = Vector3.zero;
                    link.endPoint = V(0, data.floorHeight, 0);
                    link.width = 2.5f;
                    link.bidirectional = true;

                    var el = linkObj.AddComponent<ElevatorLink>();
                    el.Initialize(data.buildingCode, floor + 1, floor + 2, false);
                }
            }
        }

        // Building name label floating above
        AddLabel(bldRoot, "BuildingLabel", V(0, data.floors * F + 2, 0), data.buildingCode, Col(0.2f, 0.8f, 1));

        return bldRoot;
    }

    /// <summary>Generates a corridor wall with door gaps for each room slot.</summary>
    static void GenerateCorridorWall(GameObject parent, string name, float len, float zOffset, int side, int rooms, float roomW, int stairSlot)
    {
        float z = zOffset * side;

        for (int r = 0; r < rooms; r++)
        {
            float rx = -len / 2 + roomW * r + roomW / 2;
            bool isStair = (r == 0 || r == stairSlot);

            if (isStair)
            {
                // Stairwell: open (no wall segment, for walkthrough)
                continue;
            }

            // Wall segment with door gap in the center
            float segW = (roomW - RoomDoorWidth) / 2;
            if (segW > 0.3f)
            {
                // Left segment
                Box(parent, $"{name}_seg{r}L",
                    V(rx - RoomDoorWidth / 2 - segW / 2, F / 2, z),
                    V(segW, F, IndoorWall), Col(0.85f, 0.83f, 0.8f));
                // Right segment
                Box(parent, $"{name}_seg{r}R",
                    V(rx + RoomDoorWidth / 2 + segW / 2, F / 2, z),
                    V(segW, F, IndoorWall), Col(0.85f, 0.83f, 0.8f));
                // Top segment (above door)
                Box(parent, $"{name}_seg{r}T",
                    V(rx, (F + RoomDoorHeight) / 2, z),
                    V(RoomDoorWidth, F - RoomDoorHeight, IndoorWall), Col(0.85f, 0.83f, 0.8f));
            }
            else
            {
                // Room too narrow, skip door gap
                Box(parent, $"{name}_seg{r}",
                    V(rx, F / 2, z), V(roomW - 0.1f, F, IndoorWall), Col(0.85f, 0.83f, 0.8f));
            }
        }
    }

    static Color RoomColor(int floor, int roomIndex, bool leftSide)
    {
        if (floor == 1 && roomIndex <= 1) return Col(0.85f, 0.75f, 0.55f); // office
        if (floor >= 2 && roomIndex == 1 && leftSide) return Col(0.7f, 0.5f, 0.8f); // auditorium
        if (roomIndex % 3 == 0) return Col(0.5f, 0.75f, 0.8f); // lab
        return Col(0.7f, 0.8f, 0.7f); // classroom
    }

    static void AddLabel(GameObject parent, string name, Vector3 localPos, string text, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform);
        obj.transform.localPosition = localPos;
        var tm = obj.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 36;
        tm.characterSize = 0.1f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
    }

    // ==================== ENTRANCE TRIGGER/MARKER ====================

    static void CreateEntranceTrigger(GameObject parent, BldDef b)
    {
        var obj = new GameObject($"Entrance_{b.Code}");
        obj.transform.SetParent(parent.transform);
        obj.transform.position = b.EntrancePos;
        var box = obj.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = V(5, 4, 5);
        box.center = V(0, 2, 0);
        var tr = obj.AddComponent<EntranceTrigger>();
        tr.buildingCode = b.Code;
        tr.buildingDisplayName = b.Display;
        tr.totalFloors = b.Floors;
    }

    static void CreateEntranceMarker(GameObject parent, BldDef b)
    {
        var obj = new GameObject($"Marker_{b.Code}");
        obj.transform.SetParent(parent.transform);
        obj.transform.position = b.EntrancePos;

        var vis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        vis.name = "Glow";
        vis.transform.SetParent(obj.transform);
        vis.transform.localPosition = V(0, 0.5f, 0);
        vis.transform.localScale = V(2.5f, 0.8f, 2.5f);
        SetMat(vis, Col(0.15f, 0.85f, 0.15f));
        Object.DestroyImmediate(vis.GetComponent<Collider>());

        var lbl = new GameObject("Label");
        lbl.transform.SetParent(obj.transform);
        lbl.transform.localPosition = V(0, 4, 0);
        var tm = lbl.AddComponent<TextMesh>();
        tm.text = b.Code; tm.fontSize = 48; tm.characterSize = 0.15f;
        tm.anchor = TextAnchor.MiddleCenter; tm.color = Col(0.1f, 0.8f, 0.1f);
    }

    // ==================== OUTDOOR BUILDING (simplified box) ====================

    static void OutdoorBuilding(GameObject parent, string name, Vector3 pos, float w, float d, int floors)
    {
        var bld = new GameObject(name);
        bld.transform.SetParent(parent.transform);
        bld.transform.localPosition = pos;
        bld.isStatic = true;
        float h = floors * F;

        Box(bld, "Floor", V(0, 0.05f, 0), V(w, 0.1f, d), Col(0.92f, 0.91f, 0.87f));
        float sideW = (w - DoorwayWidth) / 2;
        if (sideW > 0.4f)
        {
            Box(bld, "Wall_N_L", V(-w/2+sideW/2, h/2, d/2), V(sideW, h, WallT), Col(0.78f,0.76f,0.82f));
            Box(bld, "Wall_N_R", V(w/2-sideW/2, h/2, d/2), V(sideW, h, WallT), Col(0.78f,0.76f,0.82f));
        }
        else Box(bld, "Wall_N", V(0, h/2, d/2), V(w, h, WallT), Col(0.78f,0.76f,0.82f));

        Box(bld, "Wall_S", V(0, h/2, -d/2), V(w, h, WallT), Col(0.78f,0.76f,0.82f));
        Box(bld, "Wall_E", V(w/2, h/2, 0), V(WallT, h, d), Col(0.78f,0.76f,0.82f));
        Box(bld, "Wall_W", V(-w/2, h/2, 0), V(WallT, h, d), Col(0.78f,0.76f,0.82f));
        Box(bld, "Roof", V(0, h+0.1f, 0), V(w+0.4f, 0.2f, d+0.4f), Col(0.52f,0.44f,0.38f));

        var col = bld.AddComponent<BoxCollider>();
        col.center = V(0, h/2, 0);
        col.size = V(w+6, h+4, d+6);
        col.isTrigger = true;
        bld.AddComponent<BuildingRevealer>();
    }

    // ==================== CORRIDOR ====================

    static void Corridor(GameObject parent, string name, Vector3 a, Vector3 b, float w, float h)
    {
        var cor = new GameObject(name); cor.transform.SetParent(parent.transform); cor.isStatic = true;
        Vector3 mid = (a+b)*0.5f, dir = b-a;
        float len = dir.magnitude;
        if (len < 0.01f) { len = w; dir = Vector3.forward; }
        Quaternion rot = Quaternion.LookRotation(dir.normalized);
        float lenF = len + w + PathLengthOverlap;

        var fl = Prim(cor, "Floor", PrimitiveType.Cube, mid+rot*V(0,0.05f,0), V(w,0.1f,lenF), Col(0.88f,0.86f,0.78f)); fl.transform.rotation = rot; fl.isStatic = true;
        var wl = Prim(cor, "WallL", PrimitiveType.Cube, mid+rot*V(-w/2,h/2,0), V(WallT,h,lenF), Col(0.78f,0.76f,0.82f)); wl.transform.rotation = rot; wl.isStatic = true;
        var wr = Prim(cor, "WallR", PrimitiveType.Cube, mid+rot*V(w/2,h/2,0), V(WallT,h,lenF), Col(0.78f,0.76f,0.82f)); wr.transform.rotation = rot; wr.isStatic = true;
        var ce = Prim(cor, "Ceil", PrimitiveType.Cube, mid+rot*V(0,h,0), V(w+WallT*2,0.15f,lenF), Col(0.52f,0.44f,0.38f)); ce.transform.rotation = rot; ce.isStatic = true;
    }

    // ==================== TERRAIN ====================

    // Simple flat ground plane
    static void FlatGround(GameObject parent, float halfX, float halfZ)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Ground";
        go.transform.SetParent(parent.transform);
        go.transform.position = V(0, -0.05f, 0);
        go.transform.localScale = V(halfX * 2, 0.1f, halfZ * 2);
        go.isStatic = true;
        SetMat(go, Col(0.38f, 0.52f, 0.32f));
    }

    // ==================== PATH HELPERS ====================

    static void PathEW(GameObject p, string n, float xC, float z, float lX, float cW) { float h=lX/2; PathSeg(p,n,V(xC-h,0,z),V(xC+h,0,z),cW); }

    static void PathNS(GameObject p, string n, float x, float z0, float z1, float cW)
    {
        float zMin=Mathf.Min(z0,z1), zMax=Mathf.Max(z0,z1);
        PathSeg(p, n, V(x, 0, zMin), V(x, 0, zMax), cW);
    }

    static void PathSeg(GameObject p, string n, Vector3 a, Vector3 b, float cW) { PathSeg(p,n,a,b,cW,Col(0.72f,0.72f,0.68f),0.08f); }
    static void PathSeg(GameObject p, string n, Vector3 a, Vector3 b, float cW, Color c, float tY)
    {
        Vector3 mid=(a+b)/2, dir=b-a; float len=dir.magnitude; if(len<0.01f) return;
        Quaternion rot=Quaternion.LookRotation(dir.normalized);
        float ext=len+PathLengthOverlap+(cW>80?0.25f:cW);
        var o=Prim(p,n,PrimitiveType.Cube,mid+rot*V(0,tY/2,0),V(cW,tY,ext),c); o.transform.rotation=rot; o.isStatic=true;
    }

    // ==================== PRIMITIVES ====================

    static Vector3 V(float x, float y, float z) => new(x, y, z);
    static Color Col(float r, float g, float b) => new(r, g, b);
    static GameObject Child(GameObject p, string n) { var c=new GameObject(n); c.transform.SetParent(p.transform); return c; }

    static GameObject Box(GameObject parent, string name, Vector3 lp, Vector3 sc, Color col)
    { var b=GameObject.CreatePrimitive(PrimitiveType.Cube); b.name=name; b.transform.SetParent(parent.transform);
      b.transform.localPosition=lp; b.transform.localScale=sc; SetMat(b,col); b.isStatic=true; return b; }

    static GameObject Prim(GameObject parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color col)
    { var o=GameObject.CreatePrimitive(type); o.name=name; o.transform.SetParent(parent.transform);
      o.transform.position=pos; o.transform.localScale=scale; SetMat(o,col); return o; }

    static void Tree(GameObject parent, string name, Vector3 pos)
    {
        var o=new GameObject(name); o.transform.SetParent(parent.transform); o.transform.position=pos;
        var t=GameObject.CreatePrimitive(PrimitiveType.Cylinder); t.transform.SetParent(o.transform);
        t.transform.localPosition=V(0,1.5f,0); t.transform.localScale=V(0.4f,1.5f,0.4f); SetMat(t,Col(0.45f,0.3f,0.15f)); Object.DestroyImmediate(t.GetComponent<Collider>());
        var f=GameObject.CreatePrimitive(PrimitiveType.Sphere); f.transform.SetParent(o.transform);
        f.transform.localPosition=V(0,4,0); f.transform.localScale=V(3,3.5f,3); SetMat(f,Col(0.25f,0.55f,0.2f)); Object.DestroyImmediate(f.GetComponent<Collider>());
    }

    static void SetMat(GameObject obj, Color col)
    { var r=obj.GetComponent<Renderer>(); if(!r) return;
      var s=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard")??Shader.Find("Legacy Shaders/Diffuse");
      var m=new Material(s); if(m.HasProperty("_BaseColor")) m.SetColor("_BaseColor",col); else if(m.HasProperty("_Color")) m.SetColor("_Color",col);
      r.sharedMaterial=m; }

    static void Wire(Component comp, string prop, Object value)
    { var so=new SerializedObject(comp); so.FindProperty(prop).objectReferenceValue=value; so.ApplyModifiedPropertiesWithoutUndo(); }

    static GameObject Screen(GameObject canvas, string name)
    { var o=new GameObject(name); o.transform.SetParent(canvas.transform,false);
      var r=o.AddComponent<RectTransform>(); r.anchorMin=Vector2.zero; r.anchorMax=Vector2.one; r.offsetMin=Vector2.zero; r.offsetMax=Vector2.zero; return o; }

    static void EnsureEventSystem()
    {
        var es=GameObject.Find("EventSystem");
        if(es==null) es=new GameObject("EventSystem");
        if(es.GetComponent<UnityEngine.EventSystems.EventSystem>()==null) es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        if(es.GetComponent<UnityEngine.EventSystems.BaseInputModule>()==null)
        { var a=AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/InputSystem_Actions.inputactions");
          if(a!=null){var m=es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(); m.actionsAsset=a;}
          else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>(); }
    }
}
