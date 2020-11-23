using System.Collections.Generic;
using UnityEngine;
using Resources;

public abstract class Structure {
    public enum Type {
            FLOOR,
            EDGE,
            WALL,
            CEILING,
            CORNER,
            DOOR,
            COLUMN,
            LIGHT,
        }
    public struct Tile {
        public Type type;

        public Vector2Int position;
        public Vector2Int facing;
        public Tile(Vector2Int _pos, Vector2Int _face, Type _type) {
            position = _pos;
            facing = _face;
            type = _type;
        }
    }
    protected List<Tile> interior;
    protected List<Tile> ceiling;
    protected List<Tile> edges;
    protected List<Tile> walls;
    protected List<Tile> corners;
    protected List<Tile> doors;
    protected List<Tile> columns;
    protected List<Tile> lights;
    protected bool inDungeon;
    protected int width;
    protected int length;
    protected Vector2Int center;
    protected Vector2Int start;
    protected Vector2Int end;
    public Structure(List<Tile> interior, List<Tile> edges, List<Tile> walls, List<Tile> ceiling, List<Tile> corners, List<Tile> doors, List<Tile> columns, List<Tile> lights, Vector2Int center, Vector2Int start, Vector2Int end, bool inDungeon, int width, int length) {
        this.interior = interior;
        this.walls = walls;
        this.edges = edges;
        this.ceiling = ceiling;
        this.corners = corners;
        this.doors = doors;
        this.columns = columns;
        this.lights = lights;
        this.inDungeon = inDungeon;
        this.width = width;
        this.length = length;
        this.center = center;
        this.start = start;
        this.end = end;
    }

    public Structure() {
        interior = new List<Tile>();
        edges = new List<Tile>();
        walls = new List<Tile>();
        ceiling = new List<Tile>();
        corners = new List<Tile>();
        doors = new List<Tile>();
        columns = new List<Tile>();
        lights = new List<Tile>();
        inDungeon = false;
        width = 0;
        length = 0;
        center = Vector2Int.zero;
        start = Vector2Int.zero;
        end = Vector2Int.zero;
    }

    public void AddTile(Vector2Int position, Vector2Int facing, Type type) {   
        switch(type) {
            case Type.FLOOR:
                interior.Add(new Tile(position, facing, type));
                break;
            case Type.EDGE:
                edges.Add(new Tile(position, facing, type));
                break;
            case Type.WALL:
                walls.Add(new Tile(position, facing, type));
                break;
            case Type.CEILING:
                ceiling.Add(new Tile(position, facing, type));
                break;
            case Type.CORNER:
                corners.Add(new Tile(position, facing, type));
                break;
            case Type.DOOR:
                doors.Add(new Tile(position, facing, type));
                break;
            case Type.COLUMN:
                columns.Add(new Tile(position, facing, type));
                break;
            case Type.LIGHT:
                lights.Add(new Tile(position, facing, type));
                break;
        } 
    }

    public List<Tile> GetTiles(Type type) {   
        List<Tile> tiles = null;
        switch(type) {
            case Type.FLOOR:
                tiles = interior;
                break;
            case Type.EDGE:
                tiles = edges;
                break;                
            case Type.WALL:
                tiles = walls;
                break;
            case Type.CEILING:
                tiles = ceiling;
            break;
            case Type.CORNER:
                tiles = corners;
                break;
            case Type.DOOR:
                tiles = doors;
                break;
            case Type.COLUMN:
                tiles = columns;
                break;
            case Type.LIGHT:
                tiles = lights;
                break;
        } 
        return tiles;
    }

    protected bool IsEdge(DungeonGenerator map, Vector2Int placement, Vector2Int direction, out Type edge) {
        Vector2Int right = Vector2Int.zero;
        if (direction.x == 0)
            right.x = direction.y;
        else if (direction.y == 0)
            right.y = -direction.x;
        if (map.GetMapData(placement + direction) == SquareData.CLOSED || map.GetMapData(placement + direction) == SquareData.G_CLOSED || map.GetMapData(placement + direction) == SquareData.NJ_CLOSED || map.GetMapData(placement + direction) == SquareData.NJ_G_CLOSED) {
            edge = Type.EDGE;
            return true;
        } else if (map.GetMapData(placement + direction) == SquareData.H_DOOR || map.GetMapData(placement + direction) == SquareData.V_DOOR) {
            edge = Type.DOOR;
            return true;
        }
        edge = Type.FLOOR;
        return false;
    }

    public abstract void SetEdges(DungeonGenerator map);
    public abstract void SetCorners(DungeonGenerator map);
    public abstract void SetLights(DungeonGenerator map);
    public virtual void SetCeiling(DungeonGenerator map) {
        foreach (Tile floor in interior)
            AddTile(floor.position, floor.facing, Type.CEILING);

        foreach (Tile edge in edges)
            AddTile(edge.position, edge.facing, Type.CEILING);

        foreach (Tile corner in corners)
            AddTile(corner.position, corner.facing, Type.CEILING);
    }

    public int Width {get => width; set {width = value;}}
    public int Length {get => length; set {length = value;}}
    public Vector2Int Center {get => center; set {center = value;}}
    public Vector2Int Start {get => start; set {start = value;}}
    public Vector2Int End {get => end; set {end = value;}}
    public bool InDungeon {get => inDungeon; set {inDungeon = value;}}

    public static bool Compare(Structure first, Structure second) {return first.GetTiles(Type.FLOOR).Count > second.GetTiles(Type.FLOOR).Count;}
}
