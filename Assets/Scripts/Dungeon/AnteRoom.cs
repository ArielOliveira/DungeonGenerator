using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnteRoom : Structure {
    public AnteRoom(List<Tile> interior, List<Tile> edges, List<Tile> walls, List<Tile> ceiling, List<Tile> corners, List<Tile> doors, List<Tile> columns, List<Tile> lights, Vector2Int center, Vector2Int start, Vector2Int end, bool inDungeon, int sizeX, int sizeY) : base(interior, edges, walls, ceiling, corners, doors, columns, lights, center, start, end, inDungeon, sizeX, sizeY) {}

    public AnteRoom () : base() {}

     public override void SetEdges(DungeonGenerator map) {
      Vector2Int[] directions = {Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right};
         List<Tile> cleanEdges = new List<Tile>();
         foreach (Tile tile in interior) {
            Vector2Int facing = Vector2Int.zero;
            Type edge = Type.FLOOR;
            bool isEdge = false;
            for (int i = 0; i < 4; i++) {
               if (IsEdge(map, tile.position, directions[i], out edge)) {
                  if (edge != Type.DOOR) {
                     isEdge = true;
                     facing = -directions[i];
                     AddTile(tile.position+directions[i], facing, Type.WALL);
                  }
               }
            }
            if (isEdge) {
               AddTile(tile.position, facing, Type.EDGE);
               cleanEdges.Add(tile);
            }
         }
         foreach (Tile tile in cleanEdges) 
            interior.Remove(tile);
         
         cleanEdges.Clear();
   }
    public override void SetLights(DungeonGenerator map) {}
    public override void SetCorners(DungeonGenerator map) {}
}
