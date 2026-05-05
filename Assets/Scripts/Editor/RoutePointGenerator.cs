using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class RoutePointGenerator
{
    const float F = 3.5f; // floor height — must match CampusSceneGenerator

    // Building definitions: code, position, width, depth, floors
    struct BuildingDef
    {
        public string Code;
        public Vector3 Pos;
        public float W, D;
        public int Floors;
        public BuildingDef(string code, Vector3 pos, float w, float d, int floors)
        { Code = code; Pos = pos; W = w; D = d; Floors = floors; }
    }

    static float TerrainY(float z)
    {
        float terrainNorthZ = 90f, stepHeight = 1.5f;
        int levels = 6;
        float band = (terrainNorthZ - (-55f)) / levels;
        float rel = terrainNorthZ - z;
        int idx = Mathf.FloorToInt(rel / band);
        if (idx < 0) idx = 0;
        if (idx >= levels) idx = levels - 1;
        return idx * stepHeight;
    }

    [MenuItem("CampusNav/Generate All Route Points")]
    static void GenerateAll()
    {
        if (!EditorUtility.DisplayDialog("Generate Route Points",
            "Создать все RoutePoint ассеты и заполнить RouteDatabase?\nСуществующие ассеты в Assets/RoutePoints/ будут перезаписаны.",
            "Создать", "Отмена"))
            return;

        // Ensure directory exists
        if (!AssetDatabase.IsValidFolder("Assets/RoutePoints"))
            AssetDatabase.CreateFolder("Assets", "RoutePoints");

        var allPoints = new List<RoutePoint>();

        // Building definitions matching CampusSceneGenerator
        var buildings = new BuildingDef[]
        {
            new("SRK-I",  new Vector3(-50, TerrainY(60), 60),  55, 18, 4),
            new("SRC",    new Vector3(42, TerrainY(65), 65),    28, 28, 3),
            new("SRA-I",  new Vector3(0, TerrainY(40), 40),     38, 22, 4),
            new("SRK-II", new Vector3(52, TerrainY(18), 18),    18, 45, 3),
            new("SRA-II", new Vector3(15, TerrainY(-12), -12),  32, 18, 3),
            new("SRL-I",  new Vector3(5, TerrainY(-35), -35),   28, 18, 6),
            new("SRL-II", new Vector3(-45, TerrainY(-35), -35), 50, 18, 3),
        };

        // ===== ENTRANCES =====
        allPoints.Add(Create("Entrance_Main_SRK1", "SRK-I", 0, "E01",
            new Vector3(-50, TerrainY(69), 69), RoutePointCategory.Entrance, "Main entrance"));
        allPoints.Add(Create("Entrance_SRC", "SRC", 0, "E02",
            new Vector3(42, TerrainY(79), 79), RoutePointCategory.Entrance, "SRC main entrance"));
        allPoints.Add(Create("Entrance_SRA1_Parking", "SRA-I", 0, "E03",
            new Vector3(-19, TerrainY(40), 40), RoutePointCategory.Entrance, "SRA-I parking side"));
        allPoints.Add(Create("Entrance_SRL1_South", "SRL-I", 0, "E04",
            new Vector3(5, TerrainY(-44), -44), RoutePointCategory.Entrance, "SRL-I south entrance"));
        allPoints.Add(Create("Entrance_SRL2_West", "SRL-II", 0, "E05",
            new Vector3(-70, TerrainY(-35), -35), RoutePointCategory.Entrance, "SRL-II west entrance"));
        allPoints.Add(Create("Entrance_SRK2_East", "SRK-II", 0, "E06",
            new Vector3(61, TerrainY(18), 18), RoutePointCategory.Entrance, "SRK-II east entrance"));

        // ===== ELEVATORS =====
        allPoints.Add(Create("Elevator_SRK1_Main", "SRK-I", 0, "LF01",
            new Vector3(-40, TerrainY(60), 60), RoutePointCategory.Elevator, "Main elevator SRK-I"));
        allPoints.Add(Create("Elevator_SRK1_Side", "SRK-I", 0, "LF02",
            new Vector3(-30, TerrainY(60), 60), RoutePointCategory.Elevator, "Side elevator SRK-I"));
        allPoints.Add(Create("Elevator_SRA1", "SRA-I", 0, "LF03",
            new Vector3(-15, TerrainY(51), 51), RoutePointCategory.Elevator, "SRA-I corridor elevator"));
        allPoints.Add(Create("Elevator_SRL1", "SRL-I", 0, "LF04",
            new Vector3(5, TerrainY(-30), -30), RoutePointCategory.Elevator, "SRL-I elevator"));
        allPoints.Add(Create("Elevator_SRC", "SRC", 0, "LF05",
            new Vector3(42, TerrainY(70), 70), RoutePointCategory.Elevator, "SRC elevator"));

        // ===== CAFETERIA =====
        allPoints.Add(Create("Cafeteria_SRA1", "SRA-I", 1, "CF01",
            new Vector3(5, TerrainY(40) + 0.1f, 35), RoutePointCategory.Cafeteria, "Cafeteria on 1st floor SRA-I"));

        // ===== VENDING MACHINES =====
        allPoints.Add(Create("Vending_SRA1_Entrance", "SRA-I", 1, "VM01",
            new Vector3(-5, TerrainY(40) + 0.1f, 40), RoutePointCategory.VendingMachine, "Vending machine at SRA-I entrance"));
        allPoints.Add(Create("Vending_SRA2_Entrance", "SRA-II", 1, "VM02",
            new Vector3(15, TerrainY(-12) + 0.1f, -8), RoutePointCategory.VendingMachine, "Vending machine at SRA-II entrance"));

        // ===== SMOKING AREAS (before entrances) =====
        allPoints.Add(Create("Smoking_SRK1", "SRK-I", 0, "SM01",
            new Vector3(-55, TerrainY(72), 72), RoutePointCategory.SmokingArea, "Smoking area near SRK-I entrance"));
        allPoints.Add(Create("Smoking_SRC", "SRC", 0, "SM02",
            new Vector3(38, TerrainY(82), 82), RoutePointCategory.SmokingArea, "Smoking area near SRC entrance"));
        allPoints.Add(Create("Smoking_SRA1", "SRA-I", 0, "SM03",
            new Vector3(-22, TerrainY(43), 43), RoutePointCategory.SmokingArea, "Smoking area near SRA-I"));
        allPoints.Add(Create("Smoking_SRL1", "SRL-I", 0, "SM04",
            new Vector3(10, TerrainY(-47), -47), RoutePointCategory.SmokingArea, "Smoking area near SRL-I entrance"));

        // ===== WC (at least one per building, per floor where applicable) =====
        foreach (var bld in buildings)
        {
            for (int floor = 1; floor <= bld.Floors; floor++)
            {
                float y = bld.Pos.y + (floor - 1) * F + 0.1f;
                // WC typically near the entrance side (north)
                Vector3 wcPos = new(bld.Pos.x + bld.W * 0.35f, y, bld.Pos.z + bld.D * 0.35f);
                string code = bld.Code.Replace("-", "");
                allPoints.Add(Create($"WC_{code}_F{floor}", bld.Code, floor, $"WC{floor}",
                    wcPos, RoutePointCategory.WC, $"Restroom {bld.Code} floor {floor}"));
            }
        }

        // ===== STAIRS (one stairwell per building) =====
        foreach (var bld in buildings)
        {
            if (bld.Floors > 1)
            {
                Vector3 stairsPos = new(bld.Pos.x - bld.W * 0.3f, bld.Pos.y + 0.1f, bld.Pos.z + bld.D * 0.3f);
                string code = bld.Code.Replace("-", "");
                allPoints.Add(Create($"Stairs_{code}", bld.Code, 1, "ST01",
                    stairsPos, RoutePointCategory.Stairs, $"Main stairwell {bld.Code}"));
            }
        }

        // ===== CLASSROOMS / AUDITORIUMS / LABS / OFFICES =====
        // Generate rooms for each building, each floor
        // Room numbering: BuildingFloor-RoomNumber (e.g., SRK-I 2-01)
        foreach (var bld in buildings)
        {
            int roomsPerFloor = GetRoomsPerFloor(bld.Code);
            for (int floor = 1; floor <= bld.Floors; floor++)
            {
                float y = bld.Pos.y + (floor - 1) * F + 0.1f;
                for (int room = 1; room <= roomsPerFloor; room++)
                {
                    // Distribute rooms along the building width
                    float t = (float)room / (roomsPerFloor + 1);
                    float x = bld.Pos.x + Mathf.Lerp(-bld.W * 0.4f, bld.W * 0.4f, t);
                    // Alternate rooms on north/south side
                    float zOffset = (room % 2 == 0) ? bld.D * 0.25f : -bld.D * 0.25f;
                    float z = bld.Pos.z + zOffset;

                    Vector3 pos = new(x, y, z);
                    string roomNum = $"{floor}{room:D2}";
                    string roomName = $"{bld.Code} {roomNum}";
                    var category = GetRoomCategory(bld.Code, floor, room);

                    allPoints.Add(Create($"Room_{bld.Code.Replace("-", "")}_{roomNum}",
                        bld.Code, floor, roomNum, pos, category,
                        $"{category} in {bld.Code}, floor {floor}"));
                }
            }
        }

        // ===== CORRIDORS (key waypoints between buildings) =====
        allPoints.Add(Create("Corridor_SRK1_SRA1", "", 1, "CR01",
            new Vector3(-16, TerrainY(51) + 0.1f, 51), RoutePointCategory.Corridor, "Corridor SRK-I to SRA-I"));
        allPoints.Add(Create("Bridge_SRA1_SRC", "", 3, "CR02",
            new Vector3(24, TerrainY(55) + F * 2 + 0.1f, 55), RoutePointCategory.Corridor, "Bridge SRA-I to SRC (3rd floor)"));
        allPoints.Add(Create("Bridge_SRA1_SRK2", "", 3, "CR03",
            new Vector3(31, TerrainY(32) + F * 2 + 0.1f, 32), RoutePointCategory.Corridor, "Bridge SRA-I to SRK-II (3rd floor)"));
        allPoints.Add(Create("Corridor_SRL1_SRL2", "", 1, "CR04",
            new Vector3(-15, TerrainY(-35) + 0.1f, -35), RoutePointCategory.Corridor, "Corridor SRL-I to SRL-II"));
        allPoints.Add(Create("Corridor_SRA2_SRL1", "", 1, "CR05",
            new Vector3(10, TerrainY(-24) + 0.1f, -24), RoutePointCategory.Corridor, "Corridor SRA-II to SRL-I"));

        // ===== RAMPS =====
        allPoints.Add(Create("Ramp_SRK1", "SRK-I", 0, "RM01",
            new Vector3(-48, TerrainY(69), 69), RoutePointCategory.Ramp, "Wheelchair ramp SRK-I"));
        allPoints.Add(Create("Ramp_SRC", "SRC", 0, "RM02",
            new Vector3(40, TerrainY(79), 79), RoutePointCategory.Ramp, "Wheelchair ramp SRC"));

        // ===== PARKING =====
        allPoints.Add(Create("Parking_Main", "", 0, "PK01",
            new Vector3(-40, TerrainY(40), 40), RoutePointCategory.Parking, "Main parking lot near SRA-I"));

        // ===== LIBRARY =====
        allPoints.Add(Create("Library_SRK1", "SRK-I", 2, "LB01",
            new Vector3(-45, TerrainY(60) + F + 0.1f, 60), RoutePointCategory.Library, "Library in SRK-I, 2nd floor"));

        // ===== NAVIGATION WAYPOINTS (Outdoor grid to allow steering around buildings) =====
        for (float x = -100; x <= 100; x += 25)
        {
            for (float z = -60; z <= 90; z += 25)
            {
                // Shift grid slightly to not be perfectly aligned with buildings
                float ox = x + 5, oz = z + 5;
                allPoints.Add(Create($"WP_{ox}_{oz}", "", 0, "WP",
                    new Vector3(ox, TerrainY(oz) + 0.1f, oz), RoutePointCategory.Other, "Navigation waypoint"));
            }
        }

        // ===== Save all to database =====
        var dbGuids = AssetDatabase.FindAssets("t:RouteDatabase");
        RouteDatabase db = null;
        if (dbGuids.Length > 0)
        {
            db = AssetDatabase.LoadAssetAtPath<RouteDatabase>(AssetDatabase.GUIDToAssetPath(dbGuids[0]));
        }
        else
        {
            db = ScriptableObject.CreateInstance<RouteDatabase>();
            AssetDatabase.CreateAsset(db, "Assets/RouteDatabase.asset");
        }

        db.allPoints.Clear();
        db.allPoints.AddRange(allPoints);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[RoutePointGenerator] Created {allPoints.Count} route points and populated RouteDatabase.");
    }

    static RoutePoint Create(string name, string buildingCode, int floor, string roomNumber,
        Vector3 worldPos, RoutePointCategory category, string description)
    {
        string safeName = name.Replace(" ", "_").Replace("-", "_");
        string path = $"Assets/RoutePoints/{safeName}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<RoutePoint>(path);
        if (existing != null)
        {
            existing.pointName = name;
            existing.buildingCode = buildingCode;
            existing.floor = floor;
            existing.roomNumber = roomNumber;
            existing.worldPosition = worldPos;
            existing.category = category;
            existing.description = description;
            existing.isAccessible = true;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var rp = ScriptableObject.CreateInstance<RoutePoint>();
        rp.pointName = name;
        rp.buildingCode = buildingCode;
        rp.floor = floor;
        rp.roomNumber = roomNumber;
        rp.worldPosition = worldPos;
        rp.category = category;
        rp.description = description;
        rp.isAccessible = true;

        AssetDatabase.CreateAsset(rp, path);
        return rp;
    }

    static int GetRoomsPerFloor(string buildingCode)
    {
        return buildingCode switch
        {
            "SRK-I" => 8,   // Large building
            "SRK-II" => 6,
            "SRA-I" => 7,
            "SRA-II" => 5,
            "SRC" => 5,
            "SRL-I" => 6,
            "SRL-II" => 8,  // Long building
            _ => 5
        };
    }

    static RoutePointCategory GetRoomCategory(string building, int floor, int room)
    {
        // Distribute room types realistically
        // Floor 1: mostly offices/admin, some classrooms
        // Floor 2-3: classrooms and auditoriums
        // Floor 4+: labs and offices

        if (floor == 1)
        {
            if (room <= 2) return RoutePointCategory.Office;
            return RoutePointCategory.Classroom;
        }
        if (floor <= 3)
        {
            if (room == 1) return RoutePointCategory.Auditorium;
            if (room % 3 == 0) return RoutePointCategory.Lab;
            return RoutePointCategory.Classroom;
        }
        // Upper floors
        if (room % 2 == 0) return RoutePointCategory.Lab;
        return RoutePointCategory.Office;
    }
}
