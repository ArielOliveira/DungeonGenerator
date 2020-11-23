using UnityEngine;
using System.Collections.Generic;
using Random = System.Random;
using Resources;
using Type = Structure.Type;


public class DungeonGenerator : MonoBehaviour {
    public List<GameObject> wallObjs;
    //public GameObject edge;
    //public GameObject corner;
    public List<GameObject> floorObjs;
    public List<GameObject> doorObjs;
    public GameObject ceilingObj;
    public GameObject column;
    public GameObject lightObj;

    private Transform dungeon;
    public static Random random;
    private static DungeonGenerator instance = null;
    private SquareData[,] map;
    private bool[,] hasLightAt;
    private FlagsDirs[,] MapFlagsDirs;
    private SquareData background;
    private List<Direction> openings;
    private List<RectFill> design;
    private CrawlerData tunnelCrawlerStats;
    private int sizeX;
    private int sizeY;
    private int activeGeneration;
    private int seedCrawlersInTunnels;   //will be seeded after the completion of the dungeon run if this is > 0
    private int joinDist;   //a wall that will close the gap does so when distance to wall is less than this
    private int minRoomSz , medRoomSz , larRoomSz , maxRoomSz;  //minRS <= smallRoom < medRS <= medRoom < larRS <= larRoom < maxRS
    private int numSmallRoomsL , numMediumRoomsL , numLargeRoomsL;   //maximum wanted numbers of rooms in LABYRINTH
    private int currSmallRoomsL , currMediumRoomsL , currLargeRoomsL; //used for counting, updated by Roomies
    private int numSmallRoomsD , numMediumRoomsD , numLargeRoomsD;   //maximum wanted numbers of rooms in DUNGEON
    private int currSmallRoomsD , currMediumRoomsD , currLargeRoomsD; //used for counting, updated by Roomies
    private List<int> babyDelayProbsForGenerationC;
    private List<int> babyDelayProbsForGenerationT;
    private List<int> babyDelayProbsForGenerationR;
    private List<TripleInt> roomSizeProbSideways;
    private List<TripleInt> roomSizeProbBranching;
    private List<int> maxAgesC;
    private List<int> maxAgesT;
    private List<int> stepLengths;
    private List<int> corrWidths;
    
    #region Tunnelers
    private List<int> joinPref;
    private List<int> sizeUpProbs, sizeDownProbs;
    private List<int> anteRoomProbs;
    private bool crawlersInTunnels;    //if true, Crawlers can enter Tunnels and build walls in there
    private bool crawlersInAnterooms;    //if true, Crawlers can enter Anterooms and build walls in there
    private bool columnsInTunnels;
    private int patience;
    private int tunnelCrawlerGeneration;   //the generation in which Crawlers are seeded into tunnels;
    private int tunnelCrawlerClosedProb;
    private int tunnelJoinDistance;
    private double roomAspectRatio;
    private Tunneler lastChanceTunneler;
    private int genDelayLastChance;
    private int genSpeedUpOnAnteRooms;
    
    private int sizeUpGenDelay;
    #endregion
    private List<Builder> dungeonBuilders;
    private List<Room> rooms;
    private List<Tunnel> tunnels;
    private List<AnteRoom> anteRooms;
    private int mutator;
    private int noHeadProb;
   
    public void InitFromSetup(Setup setup, int seed = 0) {
        if (instance == null)
            instance = this;
        else 
            Debug.Assert(false);
        if (seed == 0)
            random = new Random();
        else
            random = new Random(seed);

        if (!setup.map.IsConsistent)
            Debug.Assert(false);
        if (!setup.builders.IsConsistent)
            Debug.Assert(false);
        if (!setup.roomParameters.IsConsistent())
            Debug.Assert(false);
        if (!setup.crawlerGenes.IsConsistent)
            Debug.Assert(false);
        if (!setup.tunnelerGenes.IsConsistent)
            Debug.Assert(false);
        if (!setup.generalGenes.IsConsistent)
            Debug.Assert(false);

        //Map Initialization
        dungeon = new GameObject("Dungeon").transform;
        transform.position = new Vector3(sizeX/2, 0f, sizeY/2);
        sizeX = setup.map.SizeX;
        sizeY = setup.map.SizeY;
        map = new SquareData[sizeX, sizeY];
        hasLightAt = new bool[sizeX, sizeY];
        MapFlagsDirs = new FlagsDirs[sizeX, sizeY];
        background = setup.map.BackgroundType;
        openings = setup.map.Openings;
        design = setup.map.DesignElements;
        tunnelJoinDistance = setup.map.TunnelJoinDist;
        joinDist = setup.map.JoinDistance;

        //Structures
        rooms = new List<Room>();
        tunnels = new List<Tunnel>();
        anteRooms = new List<AnteRoom>();

        //Set border and map based on background
        SetRect(1, 1, sizeX-2, sizeY-2, background);
        SetRect(0, 0, sizeX-1, 0, SquareData.G_CLOSED);
        SetRect(0, 0, 0, sizeY-1, SquareData.G_CLOSED);
        SetRect(sizeX-1, 0, sizeX-1, sizeY-1, SquareData.G_CLOSED);
        SetRect(0, sizeY-1, sizeX-1, sizeY-1, SquareData.G_CLOSED);

        //Set design elements
        foreach (RectFill element in setup.map.DesignElements)
            SetRect(element);
            

        foreach (Direction opening in setup.map.Openings) {
            switch(opening) {
                case Direction.NO: SetRect(0, sizeY/2 - 1, 2, sizeY/2 + 1, SquareData.G_OPEN);
                    break;
                case Direction.WE: SetRect(sizeX/2 - 1, 0, sizeX/2 + 1, 2, SquareData.G_OPEN);
                    break;
                case Direction.EA: SetRect(sizeX/2 - 1, sizeY - 3, sizeX/2 + 1, sizeY - 1, SquareData.G_OPEN);
                    break;
                case Direction.SO: SetRect(sizeX - 3, sizeY/2 - 1, sizeX - 1, sizeY/2 + 1, SquareData.G_OPEN);
                    break;
                case Direction.NW: SetRect(0, 0, 2, 2, SquareData.G_OPEN);
                    break;
                case Direction.NE: SetRect(0, sizeY - 3, 2, sizeY - 1, SquareData.G_OPEN);
                    break;
                case Direction.SW: SetRect(sizeX - 3, 0, sizeX - 1, 2, SquareData.G_OPEN);
                    break;
                case Direction.SE: SetRect(sizeX - 3, sizeY - 3, sizeX - 1, sizeY - 1, SquareData.G_OPEN);
                    break;
                default: Debug.Assert(false);
                    break;
            }
            
        }
        //End map initialization

        //Miscellaneous
        patience = setup.miscellaneous.Patience;
        mutator = setup.miscellaneous.Mutator;
        noHeadProb = setup.miscellaneous.NoHeadProb;
        sizeUpGenDelay = setup.miscellaneous.SizeUpGenDelay;
        columnsInTunnels = setup.miscellaneous.ColumnsInTunnels;
        roomAspectRatio = setup.miscellaneous.RoomAspectRatio;
        if (roomAspectRatio >= 1 || roomAspectRatio <= 0)
            roomAspectRatio = 0.6f;
        genSpeedUpOnAnteRooms = setup.miscellaneous.GenSpeedUpOnAnteRoom;
        if (genSpeedUpOnAnteRooms <= 0)
            genSpeedUpOnAnteRooms = 1;
        crawlersInTunnels = setup.miscellaneous.ColumnsInTunnels;
        crawlersInTunnels = setup.miscellaneous.CrawlersInAnteRooms;
        seedCrawlersInTunnels = setup.miscellaneous.SeedCrawlersInTunnels;

        //Room Parameters
        minRoomSz = setup.roomParameters.MinRoomSz;
        medRoomSz = setup.roomParameters.MedRoomSz;
        larRoomSz = setup.roomParameters.LarRoomSz;
        maxRoomSz = setup.roomParameters.MaxRoomSz;

        numSmallRoomsL = setup.roomParameters.NumSmallL;
        numMediumRoomsL = setup.roomParameters.NumMedL;
        numLargeRoomsL = setup.roomParameters.NumLarL;
        
        numSmallRoomsD = setup.roomParameters.NumSmallD;
        numMediumRoomsD = setup.roomParameters.NumMedD;
        numLargeRoomsD = setup.roomParameters.NumLarD;


        //Crawler Genes
        babyDelayProbsForGenerationC = setup.crawlerGenes.BabyGenerationProbsC;
        tunnelCrawlerGeneration = setup.crawlerGenes.TunnelCrawlerGeneration;
        tunnelCrawlerClosedProb = setup.crawlerGenes.TunnelCrawlerClosedProb;
        tunnelCrawlerStats = setup.crawlerGenes.TunnelCrawlerStats;

        //Tunneler Genes
        babyDelayProbsForGenerationT = setup.tunnelerGenes.BabyGenerationProbsT;
        joinPref = setup.tunnelerGenes.JoinPref;
        roomSizeProbSideways = setup.tunnelerGenes.RoomSizeProbS;
        roomSizeProbBranching = setup.tunnelerGenes.RoomSizeProbB;
        sizeUpProbs = setup.tunnelerGenes.SizeUpProb;
        sizeDownProbs = setup.tunnelerGenes.SizeDownProb;
        anteRoomProbs = setup.tunnelerGenes.AnteRoomProb;

        //General Genes
        babyDelayProbsForGenerationR = setup.generalGenes.BabyGenerationProbsR;
        maxAgesC = setup.generalGenes.MaxAgeC;
        maxAgesT = setup.generalGenes.MaxAgeT;
        stepLengths = setup.generalGenes.StepLengths;
        corrWidths = setup.generalGenes.CorridorWidth;
        
        TunnelerData lCT = setup.generalGenes.LastChanceTunneler;
        lastChanceTunneler = new Tunneler(ref instance, Vector2Int.zero, Vector2Int.zero, lCT.age, lCT.maxAge, lCT.generation, Vector2Int.zero, lCT.stepLength, lCT.tunnelWidth, lCT.straightDoubleSpawnProb, lCT.turnDoubleSpawnProb, lCT.changeDirProb, lCT.makeRoomRightProb, lCT.makeRoomLeftProb, lCT.joinPref);
        genDelayLastChance = setup.generalGenes.GenDelayLastChance;

        //Builders
        dungeonBuilders = new List<Builder>();
        foreach (CrawlerData c in setup.builders.Crawlers) 
            CreateCrawler(c.location, Directions.Transform(c.direction), -c.age, c.maxAge, c.generation, Directions.Transform(c.desiredDirection), c.stepLength, c.opening, c.corridorWidth, c.straightSingleSpawnProb, c.straightDoubleSpawnProb, c.turnSingleSpawnProb, c.turnDoubleSpawnProb, c.changeDirectionProb);
        
        foreach (Setup.Pair<CrawlerData, CrawlerData> pair in setup.builders.CrawlerPairs) {
            bool firstIsOpen = true;
            CrawlerData c = pair.First;
            if (random.Next(2) == 0)
                firstIsOpen = false;
            if (firstIsOpen)
                c.opening = 1;
            else
                c.opening = 0;
            CreateCrawler(c.location, Directions.Transform(c.direction), -c.age, c.maxAge, c.generation, Directions.Transform(c.desiredDirection), c.stepLength, c.opening, c.corridorWidth, c.straightSingleSpawnProb, c.straightDoubleSpawnProb, c.turnSingleSpawnProb, c.turnDoubleSpawnProb, c.changeDirectionProb);
            SetMapData(c.location.x, c.location.y, SquareData.CLOSED);
            c = pair.Second;
            if (firstIsOpen)
                c.opening = 0;
            else
                c.opening = 1;
            CreateCrawler(c.location, Directions.Transform(c.direction), -c.age, c.maxAge, c.generation, Directions.Transform(c.desiredDirection), c.stepLength, c.opening, c.corridorWidth, c.straightSingleSpawnProb, c.straightDoubleSpawnProb, c.turnSingleSpawnProb, c.turnDoubleSpawnProb, c.changeDirectionProb);
            SetMapData(c.location.x, c.location.y, SquareData.CLOSED);    
        }

        foreach(TunnelerData t in setup.builders.Tunnelers) {
            CreateTunneler(t.location, Directions.Transform(t.direction), -t.age, t.maxAge, t.generation, Directions.Transform(t.desiredDirection), t.stepLength, t.tunnelWidth, t.straightDoubleSpawnProb, t.turnDoubleSpawnProb, t.changeDirProb, t.makeRoomRightProb, t.makeRoomLeftProb, t.joinPref);
        }

        //Rand Crawlers
        for (int gen = 0; gen < setup.randCrawlerGenes.RandCrawlerPerGen.Count; gen++) {
            int crawlersPer1000Squares = setup.randCrawlerGenes.RandCrawlerPerGen[gen];
            if (crawlersPer1000Squares > 0) {
                int crawlersPerTopBottomWall = (sizeY * crawlersPer1000Squares) / 1000;
                if (crawlersPerTopBottomWall == 0) {
                    if (random.Next(1000) < (sizeY * crawlersPer1000Squares))
                        crawlersPerTopBottomWall = 1;
                }
                int yIndex = 0;
                for (int i = 0; i < crawlersPerTopBottomWall; i++) {
                    yIndex = 2 + random.Next(sizeY - 4);
                    Vector2Int locNorth = new Vector2Int(0, yIndex);
                    Vector2Int fwdNorth = new Vector2Int(1, 0);
                    Vector2Int desiredFwd = fwdNorth;
                    CreateCrawler(locNorth, fwdNorth, 0, GetMaxAgeC(gen), gen, desiredFwd, GetStepLength(gen), 1, GetCorrWidth(gen), Mutate2(setup.randCrawlerGenes.RandC_sSSP), Mutate2(setup.randCrawlerGenes.RandC_sDSP), Mutate2(setup.randCrawlerGenes.RandC_tSSP), Mutate2(setup.randCrawlerGenes.RandC_tDSP), Mutate2(setup.randCrawlerGenes.RandC_cDP));

                    yIndex = 2 + random.Next(sizeY - 4);
                    Vector2Int locSouth = new Vector2Int(sizeX-1, yIndex);
                    Vector2Int fwdSouth = new Vector2Int(-1, 0);
                    desiredFwd = fwdSouth;
                    CreateCrawler(locSouth, fwdSouth, 0, GetMaxAgeC(gen), gen, desiredFwd, GetStepLength(gen), 1, GetCorrWidth(gen), Mutate2(setup.randCrawlerGenes.RandC_sSSP), Mutate2(setup.randCrawlerGenes.RandC_sDSP), Mutate2(setup.randCrawlerGenes.RandC_tSSP), Mutate2(setup.randCrawlerGenes.RandC_tDSP), Mutate2(setup.randCrawlerGenes.RandC_cDP));
                }
                int crawlersPerLeftRightWall = (sizeX * crawlersPer1000Squares) / 1000;
                if (crawlersPerLeftRightWall == 0) {
                    if (random.Next(1000) < (sizeX * crawlersPer1000Squares))
                        crawlersPerLeftRightWall = 1;
                }
                int xIndex = 0;
                for (int i = 0; i < crawlersPerLeftRightWall; i++) {
                    xIndex = 2 + random.Next(sizeX - 4);
                    Vector2Int locWest = new Vector2Int(xIndex, 0);
                    Vector2Int fwdWest = new Vector2Int(0, 1);
                    Vector2Int desiredFwd = fwdWest;
                    CreateCrawler(locWest, fwdWest, 0, GetMaxAgeC(gen), gen, desiredFwd, GetStepLength(gen), 1, GetCorrWidth(gen), Mutate2(setup.randCrawlerGenes.RandC_sSSP), Mutate2(setup.randCrawlerGenes.RandC_sDSP), Mutate2(setup.randCrawlerGenes.RandC_tSSP), Mutate2(setup.randCrawlerGenes.RandC_tDSP), Mutate2(setup.randCrawlerGenes.RandC_cDP));

                    xIndex = 2 + random.Next(sizeX - 4);
                    Vector2Int locEast = new Vector2Int(xIndex, sizeY-1);
                    Vector2Int fwdEast = new Vector2Int(0, -1);
                    desiredFwd = fwdEast;
                    CreateCrawler(locEast, fwdEast, 0, GetMaxAgeC(gen), gen, desiredFwd, GetStepLength(gen), 1, GetCorrWidth(gen), Mutate2(setup.randCrawlerGenes.RandC_sSSP), Mutate2(setup.randCrawlerGenes.RandC_sDSP), Mutate2(setup.randCrawlerGenes.RandC_tSSP), Mutate2(setup.randCrawlerGenes.RandC_tDSP), Mutate2(setup.randCrawlerGenes.RandC_cDP));
                }
            }
        }
    }

    public void SetRect(int startX, int startY, int endX, int endY, SquareData type) {
        if (endX < startX || endY < startY) {
            Debug.Assert(false);
            return;
        } else {
            for (int i = startX; i <= endX; i++) {
                for (int j = startY; j <= endY; j++) {
                    SetMapData(i, j, type);
                }
            }
        }
    }
    
    public void SetRect(RectFill rect) {
        if (rect.endX < rect.startX || rect.endY < rect.startY) {
            Debug.Assert(false);
            return;
        } else {
            for (int i = rect.startX; i <= rect.endX; i++) {
                for (int j = rect.startY; j <= rect.endY; j++) {
                    SetMapData(i, j, rect.type);
                }
            }
        }

    }
    public bool AdvanceGeneration() {
        bool thereAreBuilders = false;
        int highestNegativeAge = 0;
        foreach (Builder builder in dungeonBuilders) {
            if (builder != null) {
                thereAreBuilders = true;
                if (builder.Generation == activeGeneration) {
                    int a = builder.Age;
                    if (a >= 0)
                        return true;
                    else if (highestNegativeAge == 0 || highestNegativeAge < a)
                        highestNegativeAge = a;
                }
            }
        }
        if (highestNegativeAge == 0) {
            activeGeneration++;
            return thereAreBuilders;
        } else {
            Debug.Assert(highestNegativeAge < 0);
            foreach (Builder builder in dungeonBuilders) {
                if (builder != null) {
                    if (builder.Generation == activeGeneration)
                        builder.AdvanceAge(-highestNegativeAge);
                }
            }
            return thereAreBuilders;
        }
    }

    public bool MakeIteration() {
        bool stillActive = false;
        List<Builder> currGeneration = new List<Builder>();
        foreach (Builder builder in dungeonBuilders)
            currGeneration.Add(builder);

        foreach (Builder builder in currGeneration) {
            if (builder.Generation == ActiveGeneration && builder.Age <= builder.MaxAge)
                stillActive = true;
            if (!builder.StepAhead()) 
                dungeonBuilders.Remove(builder);
        }
    
        currGeneration.Clear();
        return stillActive;
    }

    public void SeedCrawlersInTunnels() {
        int numberFound = 0;
        int tries = 0;
        while ((numberFound < seedCrawlersInTunnels) && (tries < sizeX*sizeY)) {
            tries++;
            int startX = 1 + random.Next(sizeX-4);
            int startY = 1 + random.Next(sizeY-4);
            Vector2Int test = new Vector2Int(startX, startY);

            if (random.Next(100) < 50)
                startX = 0;
            else
                startY = 0;
            if (startX == 0) {
                if (random.Next(100) < 50)
                    startY = -1;
                else 
                    startY = 1;
            } else {
                Debug.Assert(startY == 0);
                if (random.Next(100) < 50)
                    startX = -1;
                else
                    startX = 1;
            }

            Vector2Int dir = new Vector2Int(startX, startY);
            Vector2Int ortho = Vector2Int.zero;

            if (dir.x == 0)
                ortho = new Vector2Int(dir.y, 0);
            else if (dir.y == 0)
                ortho = new Vector2Int(0, -dir.x);
                
            bool notFound = true;
            while (notFound) {
                test = test + dir;
                if (test.x < 2 || test.y < 2 || test.x > sizeX - 3 || test.y > sizeY - 3)
                    break;
                
                if (GetMapData(test) != SquareData.IT_OPEN)
                    continue;
                
                if ((GetMapData(test + dir) != SquareData.IT_OPEN) || (GetMapData(test - dir) != SquareData.IT_OPEN) ||
                    (GetMapData(test + ortho) != SquareData.IT_OPEN) || (GetMapData(test - ortho) != SquareData.IT_OPEN) ||
                    (GetMapData(test + dir + ortho) != SquareData.IT_OPEN) || (GetMapData(test - dir + ortho) != SquareData.IT_OPEN) ||
                    (GetMapData(test + dir - ortho) != SquareData.IT_OPEN) || (GetMapData(test - dir - ortho) != SquareData.IT_OPEN))
                    continue;
                
                SetMapData(test, SquareData.CLOSED);

                CreateCrawler(test, dir, 0, tunnelCrawlerStats.maxAge, activeGeneration+1, dir, tunnelCrawlerStats.stepLength, 1, 1, tunnelCrawlerStats.straightSingleSpawnProb, tunnelCrawlerStats.straightDoubleSpawnProb, tunnelCrawlerStats.turnSingleSpawnProb, tunnelCrawlerStats.turnDoubleSpawnProb, tunnelCrawlerStats.changeDirectionProb);
                CreateCrawler(test, ortho, 0, tunnelCrawlerStats.maxAge, activeGeneration+1, dir, tunnelCrawlerStats.stepLength, 1, 1, tunnelCrawlerStats.straightSingleSpawnProb, tunnelCrawlerStats.straightDoubleSpawnProb, tunnelCrawlerStats.turnSingleSpawnProb, tunnelCrawlerStats.turnDoubleSpawnProb, tunnelCrawlerStats.changeDirectionProb);
                CreateCrawler(test, -ortho, 0, tunnelCrawlerStats.maxAge, activeGeneration+1, dir, tunnelCrawlerStats.stepLength, 1, 1, tunnelCrawlerStats.straightSingleSpawnProb, tunnelCrawlerStats.straightDoubleSpawnProb, tunnelCrawlerStats.turnSingleSpawnProb, tunnelCrawlerStats.turnDoubleSpawnProb, tunnelCrawlerStats.changeDirectionProb);

                if (random.Next(100) < tunnelCrawlerClosedProb)
                    CreateCrawler(test, -dir, 0, tunnelCrawlerStats.maxAge, activeGeneration+1, dir, tunnelCrawlerStats.stepLength, 0, 1, tunnelCrawlerStats.straightSingleSpawnProb, tunnelCrawlerStats.straightDoubleSpawnProb, tunnelCrawlerStats.turnSingleSpawnProb, tunnelCrawlerStats.turnDoubleSpawnProb, tunnelCrawlerStats.changeDirectionProb);
                else
                    CreateCrawler(test, ortho, 0, tunnelCrawlerStats.maxAge, activeGeneration+1, dir, tunnelCrawlerStats.stepLength, 1, 1, tunnelCrawlerStats.straightSingleSpawnProb, tunnelCrawlerStats.straightDoubleSpawnProb, tunnelCrawlerStats.turnSingleSpawnProb, tunnelCrawlerStats.turnDoubleSpawnProb, tunnelCrawlerStats.changeDirectionProb);
                
                notFound = false;
                numberFound++;
            }
        }
    }

    public bool CreateRoom(RectFill room) {
        if (sizeX < 10 || sizeY < 10)
            return false;
        if (room.endX - room.startX <= 5)
            return false;
        if (room.endY - room.startY <= 5)
            return false;
        
        int startX = room.startX + 1 + random.Next(room.endX - room.startX - 3);
        int startY = room.startY + 1 + random.Next(room.endY - room.startY - 3);
        Vector2Int start = new Vector2Int(startX, startY);

        if (!IsOpen(GetMapData(start)))
            return false;
        if (IsChecked(start))
            return false;
        
        int maxRS = maxRoomSz;
        if (!WantsMoreRoomsL(RoomSize.LARGE))
            maxRS = larRoomSz;
        if (!WantsMoreRoomsL(RoomSize.LARGE) && !WantsMoreRoomsL(RoomSize.MEDIUM))
            maxRS = medRoomSz;
        if (!WantsMoreRoomsL())
            return false;

        Vector2Int NO = new Vector2Int(-1, 0);
        Vector2Int SO = new Vector2Int(1, 0);
        Vector2Int WE = new Vector2Int(0, -1);
        Vector2Int EA = new Vector2Int(0, 1);
        Vector2Int NE = new Vector2Int(-1, 1);
        Vector2Int SE = new Vector2Int(1, 1);
        Vector2Int SW = new Vector2Int(1, -1);
        Vector2Int NW = new Vector2Int(-1, -1);

        List<Vector2Int> dirs = new List<Vector2Int>();
        dirs.Add(NO);
        dirs.Add(SO);
        dirs.Add(WE);
        dirs.Add(EA);
        dirs.Add(NE);
        dirs.Add(SE);
        dirs.Add(SW);
        dirs.Add(NW);

        List<Vector2Int> absDir = new List<Vector2Int>();
        absDir.Add(NO);
        absDir.Add(SO);
        absDir.Add(WE);
        absDir.Add(EA);

        List<Vector2Int> interDir = new List<Vector2Int>();
        interDir.Add(NE);
        interDir.Add(SE);
        interDir.Add(SW);
        interDir.Add(NW);


        bool stillFindingMultiples = true;
        List<Vector2Int> roomSquaresChecked = new List<Vector2Int>();
        List<Vector2Int> roomSquaresActive = new List<Vector2Int>();
        List<Vector2Int> activeFoundThisTurn = new List<Vector2Int>();
        roomSquaresActive.Add(start);

        int numberFound = 0;
        Vector2Int current = Vector2Int.zero;
        while (stillFindingMultiples) {
            stillFindingMultiples = false;
            for (int i = 0; i < roomSquaresActive.Count;) {
                current = roomSquaresActive[i];
                numberFound = 0;

                foreach (Vector2Int direction in dirs)
                    if (IsOpen(GetMapData(current + direction)) && !IsChecked(current + direction, roomSquaresChecked) && !IsActive(current + direction, roomSquaresActive) && !IsActive(current + direction, activeFoundThisTurn))
                        numberFound++;
                
                if (numberFound > 2) {
                    stillFindingMultiples = true;
                    foreach (Vector2Int direction in dirs)
                        if (IsOpen(GetMapData(current + direction)) && !IsChecked(current + direction, roomSquaresChecked) && !IsActive(current + direction, roomSquaresActive) && !IsActive(current + direction, activeFoundThisTurn))
                            activeFoundThisTurn.Add(current + direction);
                    
                    if (!IsChecked(current, roomSquaresChecked)) {
                        roomSquaresChecked.Add(current);
                        SetChecked(current);
                    }

                    roomSquaresActive.RemoveAt(i++);

                }   else if (numberFound == 2) {
                    int found = 0;
                    foreach (Vector2Int direction in absDir) {
                        if (IsOpen(GetMapData(current + direction)) && !IsChecked(current + direction, roomSquaresChecked) && !IsActive(current + direction, roomSquaresActive) && !IsActive(current + direction, activeFoundThisTurn)) {
                            activeFoundThisTurn.Add(current + direction);
                            found++;
                        }
                    }

                    if (found == 1) {
                        i++;
                        continue;
                    }
                    
                    foreach (Vector2Int direction in interDir) {
                        if (IsOpen(GetMapData(current + direction)) && !IsChecked(current + direction, roomSquaresChecked) && !IsActive(current + direction, roomSquaresActive) && !IsActive(current + direction, activeFoundThisTurn))
                            activeFoundThisTurn.Add(current + direction);
                    }
                        
                        if (!IsChecked(current, roomSquaresChecked)) {
                            roomSquaresChecked.Add(current);
                            SetChecked(current);
                        }
                    roomSquaresActive.RemoveAt(i++);
                } else if (numberFound == 1) {
                    i++;
                } else {
                    Debug.Assert(numberFound == 0);
                    if (!IsChecked(current, roomSquaresChecked)) {
                        roomSquaresChecked.Add(current);
                        SetChecked(current);
                    }
                    roomSquaresActive.RemoveAt(i++);
                }
                if (roomSquaresChecked.Count > maxRS)
                    return false;
            }

            foreach (Vector2Int activeFound in activeFoundThisTurn) {
                if (GetMapData(activeFound) == SquareData.G_OPEN || GetMapData(activeFound) == SquareData.NJ_G_OPEN)
                    return false;
                if (!IsChecked(activeFound, roomSquaresChecked) && !IsActive(activeFound, roomSquaresActive))
                    roomSquaresActive.Add(activeFound);
            }
            activeFoundThisTurn.Clear();
        }  

        bool proceeding = true;
        int squaresFindingMultiples = 0;

        while (proceeding) {
            squaresFindingMultiples = 0;
            proceeding = false;
            for (int i = 0; i < roomSquaresActive.Count;) {
                current = roomSquaresActive[i];
                numberFound = 0;
                foreach (Vector2Int direction in dirs) {
                    if (IsOpen(GetMapData(current + direction)) && !IsChecked(current + direction, roomSquaresChecked) && !IsActive(current + direction, roomSquaresActive) && !IsActive(current + direction, activeFoundThisTurn))
                        numberFound++;
                }

                if (numberFound > 1) {
                    squaresFindingMultiples++;
                    i++;
                } else if (numberFound == 1) {
                    proceeding = true;
                    foreach (Vector2Int direction in dirs) {
                        if (IsOpen(GetMapData(current + direction)) && !IsChecked(current + direction, roomSquaresChecked) && !IsActive(current + direction, roomSquaresActive) && !IsActive(current + direction, activeFoundThisTurn))
                            activeFoundThisTurn.Add(current + direction);
                    }
                    if (!IsChecked(current, roomSquaresChecked)) {
                        roomSquaresChecked.Add(current);
                        SetChecked(current);
                    }
                    roomSquaresActive.RemoveAt(i++);
                } else {
                    Debug.Assert(numberFound == 0);
                    if (!IsChecked(current, roomSquaresChecked)) {
                        roomSquaresChecked.Add(current);
                        SetChecked(current);
                    }
                    roomSquaresActive.RemoveAt(i++);
                }
            }
            foreach (Vector2Int activeFound in activeFoundThisTurn) {
                if (GetMapData(activeFound) == SquareData.G_OPEN || GetMapData(activeFound) == SquareData.NJ_G_OPEN)
                    return false;
                if (!IsChecked(activeFound, roomSquaresChecked) && !IsActive(activeFound, roomSquaresActive))
                    roomSquaresActive.Add(activeFound);
            }
            activeFoundThisTurn.Clear();
        }

        if (squaresFindingMultiples > 1) 
            return false;
        else if (squaresFindingMultiples == 0) {
            Debug.Assert(roomSquaresChecked.Count > 0);
            foreach (Vector2Int squareChecked in roomSquaresChecked) {
                Debug.Assert(GetMapData(squareChecked) == SquareData.OPEN || GetMapData(squareChecked) == SquareData.NJ_OPEN || GetMapData(squareChecked) == SquareData.IT_OPEN || GetMapData(squareChecked) == SquareData.IA_OPEN);
                SetMapData(squareChecked, SquareData.CLOSED);
            }
        } else {
            Debug.Assert(squaresFindingMultiples == 1);
            if (roomSquaresChecked.Count < minRoomSz)
                return false;
            
            bool diffX = false;
            bool diffY = false;
            int sX = roomSquaresChecked[0].x;
            int sY = roomSquaresChecked[0].y;

            foreach (Vector2Int squareChecked in roomSquaresChecked) {
                if (squareChecked.x != sX)
                    diffX = true;
                if (squareChecked.y != sY)
                    diffY = true;
            }

            if (!diffX || !diffY)
                return false;
            
            if ((GetMapData(current + WE) == SquareData.V_DOOR) || (GetMapData(current + EA) == SquareData.V_DOOR) || (GetMapData(current + WE) == SquareData.H_DOOR) || (GetMapData(current + EA) == SquareData.H_DOOR) ||
                (GetMapData(current + NO) == SquareData.V_DOOR) || (GetMapData(current + SO) == SquareData.V_DOOR) || (GetMapData(current + NO) == SquareData.H_DOOR) || (GetMapData(current + SO) == SquareData.H_DOOR))
                return false;

            if (roomSquaresChecked.Count < medRoomSz) {
                if (!WantsMoreRoomsL(RoomSize.SMALL))
                    return false;
                else
                    currSmallRoomsL++;
            } else if (roomSquaresChecked.Count < larRoomSz) {
                if (!WantsMoreRoomsL(RoomSize.MEDIUM))
                    return false;
                else
                    currMediumRoomsL++;
            } else if (roomSquaresChecked.Count < maxRoomSz) {
                if (!WantsMoreRoomsL(RoomSize.LARGE))
                    return false;
                else
                    currLargeRoomsL++;
            } else
                return false;

            Debug.Assert(roomSquaresActive.Count == 1);
            current = roomSquaresActive[0];
            if (IsOpen(GetMapData(current + NO))) {
                Debug.Assert(IsOpen(GetMapData(current + SO)));
                SetMapData(current, SquareData.H_DOOR);
            } else if (IsOpen(GetMapData(current + WE))) {
                Debug.Assert(IsOpen(GetMapData(current + EA)));
                SetMapData(current, SquareData.V_DOOR);
            }

            Room newRoom = new Room();

            foreach (Vector2Int squareChecked in roomSquaresChecked) {
                Debug.Assert(GetMapData(squareChecked) == SquareData.OPEN || GetMapData(squareChecked) == SquareData.NJ_OPEN || GetMapData(squareChecked) == SquareData.IT_OPEN || GetMapData(squareChecked) == SquareData.IA_OPEN);
                SetMapData(squareChecked, SquareData.IR_OPEN);
                newRoom.AddTile(squareChecked, Vector2Int.zero, Structure.Type.FLOOR);
            }

            newRoom.InDungeon = false;
            rooms.Add(newRoom);        
        }
        return true;
    }

    
    public bool IsOpen(SquareData square) {if (square == SquareData.OPEN || square == SquareData.NJ_OPEN || square == SquareData.IT_OPEN || square == SquareData.IA_OPEN || square == SquareData.G_OPEN || square == SquareData.NJ_G_OPEN) return true; else return false;}
    public bool IsChecked(Vector2Int pos) {Debug.Assert((pos.x < sizeX) && (pos.y < sizeY) && (pos.x >= 0) && (pos.y >= 0)); return MapFlagsDirs[pos.x, pos.y]._checked;}
    public bool IsChecked(Vector2Int pos, List<Vector2Int> check) {
        foreach (Vector2Int square in check) 
            if (pos.x == square.x && pos.y == square.y)
                return true;
        return false;
        }

    public bool IsActive(Vector2Int pos, List<Vector2Int> active) {
        foreach (Vector2Int square in active)
            if (pos.x == square.x && pos.y == square.y)
                return true;
        return false;
    }
    public void SetChecked(Vector2Int pos) {Debug.Assert((pos.x < sizeX) && (pos.y < sizeY) && (pos.x >= 0) && (pos.y >= 0));  MapFlagsDirs[pos.x, pos.y]._checked = true;}
    public void SetUnchecked(Vector2Int pos) {Debug.Assert((pos.x < sizeX) && (pos.y < sizeY) && (pos.x >= 0) && (pos.y >= 0));  MapFlagsDirs[pos.x, pos.y]._checked = false;}
    

    public SquareData GetMapData(int x, int y) {
        Debug.Assert((x >= 0)  && (y >= 0) && (x < sizeX) && (y < sizeY));
        return map[x, y];
    }

    public SquareData GetMapData(Vector2Int pos) {
        Debug.Assert((pos.x >= 0)  && (pos.y >= 0) && (pos.x < sizeX) && (pos.y < sizeY));
        return map[pos.x, pos.y];
    }

    public void SetMapData(int x, int y, SquareData data) {
        Debug.Assert((x >= 0 && y >= 0) && (x < sizeX && y < sizeY));
        map[x, y] = data;
    }

    public void SetMapData(Vector2Int pos, SquareData data) {
        Debug.Assert((pos != Vector2Int.zero) && (pos.x < sizeX && pos.y < sizeY));
        map[pos.x, pos.y] = data;
    }

    public void SetLastChanceTunneler(Tunneler tunneler) {
        this.lastChanceTunneler = tunneler;
         int diceRoll = DungeonGenerator.random.Next(101);
        int gen = tunneler.Generation;
        int summedProbs = 0;
        for (int ind = 0; ind <= 10; ind++) {
            summedProbs = summedProbs + GetBabyDelayProbsForGenerationT(ind);
            if (diceRoll < summedProbs) {
                gen = gen + ind;
                break;
            }
        }
        this.genDelayLastChance = gen;
    }

    public int LastChanceRoomsRightProb {get => lastChanceTunneler.MakeRoomsRightProb;}
    public int LastChanceRoomsLeftProb {get => lastChanceTunneler.MakeRoomsLeftProb;}
    public int LastChanceChangeDirProb {get => lastChanceTunneler.ChangeDirProb;}
    public int LastChanceStraightSpawnProb {get => lastChanceTunneler.StraightDoubleSpawnProb;}
    public int LastChanceTurnSpawnProb {get => lastChanceTunneler.TurnDoubleSpawnProb;}
    public int LastChanceGenDelay {get => genDelayLastChance;}
    public int TunnelCrawlerGeneration {get => tunnelCrawlerGeneration;}
    public int Patiente {get => patience;}
    public int SizeUpGenDelay {get => sizeUpGenDelay;}
    public int GenSpeedUpOnAnteRoom {get => genSpeedUpOnAnteRooms;}
    public int NoHeadProb {get => noHeadProb;}
    public int ActiveGeneration {get => activeGeneration;}
    public int JoinDistance {get => joinDist;}
    public int TunnelJoinDistance {get => tunnelJoinDistance;}
    public int SizeX {get => sizeX;}
    public int SizeY {get => sizeY;}
    public double RoomAspectRatio {get => roomAspectRatio;}
    public bool CrawlersInTunnels {get => crawlersInTunnels;}
    public bool CrawlersInAnterooms {get => crawlersInAnterooms;}
    public bool ColumnsInTunnels {get => columnsInTunnels;}
    public List<RectFill> Design {get => design;}
    public SquareData Background {get => background;}

    public int GetBabyDelayProbsForGenerationC(int gen) {if((gen >= 0) && (gen <= 10)) return babyDelayProbsForGenerationC[gen]; else return 0;}
    public int GetBabyDelayProbsForGenerationT(int gen) {if((gen >= 0) && (gen <= 10)) return babyDelayProbsForGenerationT[gen]; else return 0;}
    public int GetBabyDelayProbsForGenerationR(int gen) {if((gen >= 0) && (gen <= 10)) return babyDelayProbsForGenerationR[gen]; else return 0;}

    public int GetAnteRoomProb(int tunnelWidth) {if (tunnelWidth >= anteRoomProbs.Count) return 100; else return anteRoomProbs[tunnelWidth];}
    public int GetJoinPref(int gen) {if (gen >= joinPref.Count) return joinPref[joinPref.Count-1]; else return joinPref[gen];}
    public int GetSizeUpProb(int gen) {if (gen >= sizeUpProbs.Count) return sizeUpProbs[sizeUpProbs.Count-1]; else return sizeUpProbs[gen];}
    public int GetSizeDownProb(int gen) {if (gen >= sizeDownProbs.Count) return sizeDownProbs[sizeDownProbs.Count-1]; else return sizeDownProbs[gen];}
    public int GetStepLength(int gen) {if(gen >= stepLengths.Count) return stepLengths[stepLengths.Count-1]; else return stepLengths[gen];}
    public int GetCorrWidth(int gen) {if(gen >= corrWidths.Count) return corrWidths[corrWidths.Count-1]; else return corrWidths[gen];}
    public int GetMaxAgeC(int gen) {if (gen >= maxAgesC.Count) return maxAgesC[maxAgesC.Count-1]; else return maxAgesC[gen];}
    public int GetMaxAgeT(int gen) {if (gen >= maxAgesT.Count) return maxAgesT[maxAgesT.Count-1]; else return maxAgesT[gen];}
    public void CreateCrawler(Vector2Int _location, Vector2Int _forward, int _age, int _maxAge, int _generation,
                        Vector2Int desiredDir, int sL, int op, int cW, int sSS, int sDS, int tSS, int tDS, int cDP) {
        dungeonBuilders.Add(new WallCrawler(ref instance, _location, _forward, _age, _maxAge, _generation, desiredDir, sL, op, cW, sSS, sDS, tSS, tDS, cDP));
    }

    public void CreateTunneler(Vector2Int _location, Vector2Int _forward, int _age, int _maxAge, int _generation,
                        Vector2Int desiredDir, int sL, int tW, int sDSP, int tDSP, int cDP, int mRRP, int mRLP, int jP) {
        dungeonBuilders.Add(new Tunneler(ref instance, _location, _forward, _age, _maxAge, _generation, desiredDir, sL, tW, sDSP, tDSP, cDP, mRRP, mRLP, jP));
    }

    public void CreateRoomie(Vector2Int _location, Vector2Int _forward, int _age, int _maxAge, int _generation,
        int dW, RoomSize size, int cat) {
        dungeonBuilders.Add(new Roomie(ref instance, _location, _forward, _age, _maxAge, _generation, dW, size, cat));        
    }

    public bool HasTunnelerAt(Vector2Int location, Vector2Int facing) {
        Tunneler tunneler = dungeonBuilders.Find(x => x.Location == location) as Tunneler;
        if (tunneler != null)
            return true;
        return false;
    }

    public bool WantsMoreRoomsL(RoomSize sz) {if (sz == RoomSize.SMALL) return (numSmallRoomsL > currSmallRoomsL); else if (sz == RoomSize.MEDIUM) return (numMediumRoomsL > currMediumRoomsL); else if (sz == RoomSize.LARGE) return (numLargeRoomsL > currLargeRoomsL); else Debug.Assert(false); return false;}
    public bool WantsMoreRoomsL() {return (WantsMoreRoomsL(RoomSize.SMALL) || WantsMoreRoomsL(RoomSize.MEDIUM) || WantsMoreRoomsL(RoomSize.LARGE));}
    public bool WantsMoreRoomsD(RoomSize sz) {if (sz == RoomSize.SMALL) return (numSmallRoomsD > currSmallRoomsD); else if (sz == RoomSize.MEDIUM) return (numMediumRoomsD > currMediumRoomsD); else if (sz == RoomSize.LARGE) return (numLargeRoomsD > currLargeRoomsD); else Debug.Assert(false); return false;}
    public bool WantsMoreRoomsD() {return (WantsMoreRoomsD(RoomSize.SMALL) || WantsMoreRoomsD(RoomSize.MEDIUM) || WantsMoreRoomsD(RoomSize.LARGE));}
    public void BuiltRoomD(RoomSize sz) {if(RoomSize.SMALL == sz) currSmallRoomsD++; else if(RoomSize.MEDIUM == sz) currMediumRoomsD++; else if(RoomSize.LARGE == sz) currLargeRoomsD++;}

    public void PlaceStructure<T>(List<T> structures, string name) where T : Structure {
        foreach (T s in structures) {
            Transform structure = new GameObject(name).transform;
            structure.transform.position = new Vector3(s.Center.x, 0f, s.Center.y);
            structure.transform.parent = dungeon;
            Quaternion rotation = Quaternion.identity;
            foreach (Structure.Tile square in s.GetTiles(Type.FLOOR)) {
                GameObject floor = Instantiate(floorObjs[random.Next(floorObjs.Count-1)], new Vector3(square.position.x*4, 0f, square.position.y*4), rotation);
                floor.transform.parent = structure;
            }

            foreach (Structure.Tile square in s.GetTiles(Type.EDGE)) {
                rotation = Quaternion.LookRotation(new Vector3(square.facing.x, 0f, square.facing.y));
                GameObject floor = Instantiate(floorObjs[random.Next(floorObjs.Count)], new Vector3(square.position.x*4, 0f, square.position.y*4), rotation);
                floor.transform.parent = structure;
            }
            
            foreach (Structure.Tile square in s.GetTiles(Type.WALL)) {
                rotation = Quaternion.LookRotation(new Vector3(square.facing.x, 0f, square.facing.y));
                GameObject wall = Instantiate(wallObjs[random.Next(wallObjs.Count)], new Vector3((square.position.x*4)+square.facing.x*2, 0f, (square.position.y*4)+square.facing.y*2), rotation);
                wall.transform.parent = structure;
            }

            foreach (Structure.Tile square in s.GetTiles(Type.CEILING)) {
                rotation = Quaternion.Euler(0f, 0f, -180f);
                GameObject ceiling = Instantiate(ceilingObj, new Vector3((square.position.x*4), 4f, (square.position.y*4)), rotation);
                ceiling.transform.parent = structure;
            }

            foreach (Structure.Tile square in s.GetTiles(Type.CORNER)) {
                rotation = Quaternion.LookRotation(new Vector3(square.facing.x, 0f, square.facing.y));
                GameObject floor = Instantiate(floorObjs[random.Next(floorObjs.Count)], new Vector3(square.position.x*4, 0f, square.position.y*4), rotation);
                floor.transform.parent = structure;
            }

            foreach (Structure.Tile square in s.GetTiles(Type.DOOR)) {
                rotation = Quaternion.LookRotation(new Vector3(square.facing.x, 0f, square.facing.y));
                GameObject door = Instantiate(doorObjs[random.Next(doorObjs.Count)], new Vector3(square.position.x*4, 0f, square.position.y*4), rotation);
                door.transform.parent = structure;
            }

            foreach (Structure.Tile square in s.GetTiles(Type.COLUMN)) {
                //rotation = Quaternion.LookRotation(new Vector3(square.facing.x, 0f, square.facing.y));
                GameObject columnObj = Instantiate(column, new Vector3(square.position.x*4+(square.facing.x*2.3f), 0f, square.position.y*4+(square.facing.y*2.3f)), Quaternion.identity);
                columnObj.transform.parent = structure;
            }

            foreach (Structure.Tile square in s.GetTiles(Type.LIGHT)) {
                float offset = 0f;
                if (GetMapData(square.position) == SquareData.H_DOOR || GetMapData(square.position) == SquareData.V_DOOR)
                    offset = -2f;
                else if (GetMapData(square.position) == SquareData.COLUMN)
                    offset = -1.88f;
                rotation = Quaternion.LookRotation(new Vector3(square.facing.x, 0f, square.facing.y));
                GameObject light = Instantiate(lightObj, new Vector3(square.position.x*4+(square.facing.x*offset), 0f, square.position.y*4+(square.facing.y*offset)), rotation);
                light.transform.parent = structure;
            }
        }
    }
    public void AddRoom(Room r) {
        rooms.Add(r);
    }

    public Room FindRoom(Vector2Int at) {
        return rooms.Find(x => x.Contact(at));
    }

    public void AddTunnel(Tunnel t) {
        tunnels.Add(t);
    }

    public void AddAnteRoom(AnteRoom aR) {
        anteRooms.Add(aR);
    }

    public void PlaceRooms() {
        PlaceStructure<Room>(rooms, "Room");
    }
    public void PlaceTunnels() {
        PlaceStructure<Tunnel>(tunnels, "Tunnel");
    }

    public void PlaceAnteRooms() {
        PlaceStructure<AnteRoom>(anteRooms, "AnteRoom");
    }

    public bool HasLightAt(Vector2Int square) {
        if (square.x < 0 || square.x >= sizeX || square.y < 0 || square.y >= sizeY)
            return false;
        return hasLightAt[square.x, square.y];
    }

    public bool HasLightAt(int x, int y) {
        if (x < 0 || x > sizeX || y < 0 || y > sizeY)
            return false;
        return hasLightAt[x, y];
    }

    public void SetLightAt(Vector2Int square) {
        hasLightAt[square.x, square.y] = true;
    }

    public void SetLightAt(int x, int y) {
        hasLightAt[x, y] = true;
    }

    public void SetEdges() {
        foreach(Tunnel t in tunnels)
           t.SetEdges(this);

        foreach(AnteRoom aR in anteRooms)
            aR.SetEdges(this);

        foreach(Room r in rooms)
            r.SetEdges(this);
    }

    public void SetCeiling() {
        foreach(Tunnel t in tunnels)
           t.SetCeiling(this);

        foreach(AnteRoom aR in anteRooms)
            aR.SetCeiling(this);

        foreach(Room r in rooms)
            r.SetCeiling(this);
    }

    public void SetLights() {
        foreach(Tunnel t in tunnels)
           t.SetLights(this);

       // foreach(AnteRoom aR in anteRooms)
           // aR.SetLights(this);

        foreach(Room r in rooms)
            r.SetLights(this);
    }

    public int GetTunnelerNum() {
        int count = 0;
        foreach (Builder builder in dungeonBuilders) {
            Tunneler tunneler = builder as Tunneler;
            if (tunneler != null)
                count++;
        }
        return count;
    }

    public int GetRoomieNum() {
        int count = 0;
        foreach (Builder builder in dungeonBuilders) {
            Roomie roomie = builder as Roomie;
            if (roomie != null)
                count++;
        }
        return count;
    }

    public int GetMinRoomSize(RoomSize sz) {
        if (sz == RoomSize.SMALL) 
            return minRoomSz;
        else if (sz == RoomSize.MEDIUM)
            return medRoomSz;
        else {
            Debug.Assert(sz == RoomSize.LARGE);
            return larRoomSz;
        }
    }

    public int GetMaxRoomSize(RoomSize sz) {
        if (sz == RoomSize.SMALL) 
            return medRoomSz-1;
        else if (sz == RoomSize.MEDIUM)
            return maxRoomSz-1;
        else {
            Debug.Assert(sz == RoomSize.LARGE);
            return maxRoomSz-1;
        }
    }

    public int GetRoomSizeProbSideways(int tunnelWidth, RoomSize size) {
        if (tunnelWidth >= roomSizeProbSideways.Count) {
            if (size == RoomSize.LARGE)
                return 100;
            else
                return 0;
        } else if (size == RoomSize.LARGE)
            return roomSizeProbSideways[tunnelWidth].large;
          else if (size == RoomSize.MEDIUM)
            return roomSizeProbSideways[tunnelWidth].medium;
          else if (size == RoomSize.SMALL) {
            Debug.Assert(size == RoomSize.SMALL);
            return roomSizeProbSideways[tunnelWidth].small;
          }
        return 0;
    }

    public int GetRoomSizeProbBranching(int tunnelWidth, RoomSize size) {
        if (tunnelWidth >= roomSizeProbBranching.Count) {
            if (size == RoomSize.LARGE)
                return 100;
            else
                return 0;
        } else if (size == RoomSize.LARGE)
            return roomSizeProbBranching[tunnelWidth].large;
          else if (size == RoomSize.MEDIUM)
            return roomSizeProbBranching[tunnelWidth].medium;
          else if (size == RoomSize.SMALL) {
            Debug.Assert(size == RoomSize.SMALL);
            return roomSizeProbBranching[tunnelWidth].small;
          }
        return 0;
    }

    public int Mutate(int input) {
        int output = input - mutator + (DungeonGenerator.random.Next(2*mutator+1)); 
        if(output < 0)
            return 0;
        else
            return output;
    }

    public int Mutate2(int input) {
        if(input <= 50) {
            if(input < 0)
	            return 0;
            else
	            return DungeonGenerator.random.Next(2*input+1); 
        } else  {
            if(input > 100)
                return 100;
            else
                return (2 * input - 100 + DungeonGenerator.random.Next(200-2*input+1));
        }
    }
}
