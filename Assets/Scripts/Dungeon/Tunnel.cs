using System.Collections.Generic;
using UnityEngine;
using Resources;

public class Tunnel : Structure {
   public Tunnel(List<Tile> interior, List<Tile> edges, List<Tile> walls, List<Tile> ceiling, List<Tile> corners, List<Tile> doors, List<Tile> columns, List<Tile> lights, Vector2Int center, Vector2Int start, Vector2Int end, bool inDungeon, int sizeX, int sizeY) : base(interior, edges, walls, ceiling, corners, doors, columns, lights, center, start, end, inDungeon, sizeX, sizeY) {}

   public Tunnel() : base() {}

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
 
   public override void SetCorners(DungeonGenerator map) {}

   /*public override void SetLights(DungeonGenerator map) {
      if (columns.Count == 0) {
         foreach (Tile wall in walls) {
            Vector2Int pos = wall.position;
            Vector2Int fwd = wall.facing;
            Vector2Int right = Vector2Int.zero;

            if (fwd.x == 0)
               right.x = fwd.y;
            else if (fwd.y == 0)
               right.y = -fwd.x;

            bool isDark = true;
            
            if (map.GetMapData(pos+right) == SquareData.IT_OPEN || map.GetMapData(pos-right) == SquareData.IT_OPEN) {
               if (map.GetMapData(pos+right) == SquareData.IT_OPEN)
                  AddTile(pos, right+fwd, Type.COLUMN);
               for (int i = -3; i <= 3; i++)
                  for (int j = 0; j <= 3; j++)
                     if (map.HasLightAt(pos+fwd*j+right*i))
                        isDark = false;
                     if (isDark) {
                        map.SetLightAt(pos);
                        AddTile(pos, fwd, Type.LIGHT);
                     }
            } else if (map.GetMapData(pos+right) == SquareData.H_DOOR || map.GetMapData(pos+right) == SquareData.V_DOOR) {
               AddTile(pos, right+fwd, Type.COLUMN);
            } else if (map.GetMapData(pos-right) == SquareData.H_DOOR || map.GetMapData(pos-right) == SquareData.V_DOOR) {
               AddTile(pos, -right+fwd, Type.COLUMN);
            } else {
               Type edge = Type.CEILING;
               if (IsEdge(map, pos+fwd, right, out edge) || IsEdge(map, pos+fwd, -right, out edge)) {
                  if (IsEdge(map, pos+fwd, right, out edge))
                     if (edge == Type.EDGE)
                        AddTile(pos+right, -right+fwd, Type.COLUMN);
                  isDark = true;
                  for (int i = -3; i <= 3; i++)
                     for (int j = 0; j <= 3; j++)
                        if (map.HasLightAt(pos+fwd*j+right*i))
                           isDark = false;
                        if (isDark) {
                           map.SetLightAt(pos);
                           AddTile(pos, fwd, Type.LIGHT);
                        }
                  } else if (edge == Type.DOOR) {
                     isDark = true;
                     for (int i = 0; i <= 3; i++)
                        for (int j = -3; j <= 3; j++)
                           if (map.HasLightAt(pos+fwd*j+right*i))
                              isDark = false;
                     if (isDark) {
                        map.SetLightAt(pos);
                        AddTile(pos, fwd, Type.LIGHT);
                     }
                  } 
               }   
         }
      } else {
         foreach(Tile column in columns) {
            Vector2Int pos = column.position;
            Vector2Int fwd = column.facing;
            Vector2Int right = Vector2Int.zero;

            if (fwd.x == 0)
               right.x = fwd.y;
            else if (fwd.y == 0)
               right.y = -fwd.x;

            bool isDark = true;
            
            for (int i = -3; i <= 3; i++)
               for (int j = 0; j <= 3; j++)
                  if (map.HasLightAt(pos+fwd*j+right*i))
                     isDark = false;
            
            if (isDark) {
               map.SetLightAt(pos);
               AddTile(pos, fwd, Type.LIGHT);
            }
         }
      }
   }*/
}
