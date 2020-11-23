using System.Collections.Generic;
using UnityEngine;
using Resources;

public class Room : Structure {    
    private List<Room> rooms;
    public Room(List<Tile> interior, List<Tile> edges, List<Tile> walls, List<Tile> ceiling, List<Tile> corners, List<Tile> doors, List<Tile> columns, List<Tile> lights, Vector2Int center, Vector2Int start, Vector2Int end, bool inDungeon, int sizeX, int sizeY) : base(interior, edges, walls, ceiling, corners, doors, columns, lights, center, start, end, inDungeon, sizeX, sizeY) {}

    public Room() : base() {}

    public Tile GetRandomSquare() {return interior[DungeonGenerator.random.Next(interior.Count-1)];}

    public bool Contact(Vector2Int contact) {
        foreach (Tile tile in interior) {
            if (tile.position == contact) 
                return true;
        }
        return false;
    }

    public override void SetEdges(DungeonGenerator map) {
        Vector2Int[] directions = {Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right};
        List<Tile> cleanEdges = new List<Tile>();
        foreach (Tile tile in interior) {
            Vector2Int facing = Vector2Int.zero;
            Type edge = Type.FLOOR;
            bool isEdge = false;
            int i = 0;
            int count = 0;
            for (i = 0; i < 4; i++) {
                if (IsEdge(map, tile.position, directions[i], out edge)) {
                    isEdge = true;      
                    facing = -directions[i];
                    count++;
                    if (edge != Type.DOOR) {                       
                        AddTile(tile.position+directions[i], facing, Type.WALL);
                    } 
                }
            }

            if (isEdge) {
                cleanEdges.Add(tile);
                if (count == 1) {
                    AddTile(tile.position, facing, Type.EDGE);
                } else if (count == 2) {
                    if ((map.GetMapData(tile.position) == SquareData.H_DOOR) || (map.GetMapData(tile.position) == SquareData.V_DOOR)) {
                        AddTile(tile.position, facing, Type.EDGE);
                    }
                }
            }
        }

        foreach (Tile tile in cleanEdges)
            interior.Remove(tile);

        cleanEdges.Clear();
    }
    public override void SetLights(DungeonGenerator map) {
        foreach (Tile door in doors) {
            AddTile(door.position, door.facing, Type.LIGHT);
            map.SetLightAt(door.position);
            AddTile(door.position, -door.facing, Type.LIGHT);
            map.SetLightAt(door.position);
        }
    }
    public override void SetCorners(DungeonGenerator map) {}
}
