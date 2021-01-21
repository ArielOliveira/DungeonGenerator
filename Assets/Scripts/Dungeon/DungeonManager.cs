using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using Resources;

public class DungeonManager : MonoBehaviour
{   
    public static DungeonManager instance;
    bool placeDesigns;
    bool stillActive1;
    bool stillActive2;

    bool setEdges;
    bool backGround;
    bool firstSeed;
    bool secondSeed;
    bool iteration = false;
    int number, count, index;
    RectFill rect;

    [Header("Design")]
    [SerializeField] Text text;
    [SerializeField] Text text2;
    [SerializeField] string designFileName;
    public DungeonGenerator generator;
    // Start is called before the first frame update
    void Awake() {   
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
        
        XmlSerializer serializer = new XmlSerializer(typeof(Setup));

        FileStream file = new FileStream(designFileName, FileMode.Open);
        Setup setup = (Setup)serializer.Deserialize(file);
        placeDesigns = false;
        stillActive1 = true;
        stillActive2 = false;
        setEdges = false;
        backGround = false;
        firstSeed = true;
        secondSeed = true;
        
        count = 0;
        index = 0;
        generator = GetComponent<DungeonGenerator>();
        generator.InitFromSetup(setup, setup.map.Seed);
        rect = new RectFill(0, 0, generator.SizeX, generator.SizeY, generator.Background);
        number = generator.SizeX * generator.SizeY;
    }

    // Update is called once per frame
    void Update()
    {
        bool input = Input.GetButtonDown("Jump");
        //if (input) {
        if (stillActive1) {
            if (firstSeed && generator.ActiveGeneration == generator.TunnelCrawlerGeneration) {
                generator.SeedCrawlersInTunnels();
                firstSeed = false;
            }

            iteration = generator.MakeIteration();
            if (!iteration) { 
                stillActive1 = generator.AdvanceGeneration();
            }
            if (!stillActive1)
                stillActive2 = true;
        }

        if (stillActive2) {
            if (secondSeed && (generator.TunnelCrawlerGeneration < 0 || generator.ActiveGeneration < generator.TunnelCrawlerGeneration)) {
                generator.SeedCrawlersInTunnels();
                secondSeed = false;
            }

            iteration = generator.MakeIteration();
            if (!iteration) { 
                stillActive2 = generator.AdvanceGeneration();
            }
            if (!stillActive2)
                    backGround = true;
        }
        
        if (generator.Background == SquareData.OPEN && backGround) {
            if (generator.WantsMoreRoomsL()) {
                Debug.Log("wants more rooms L");
               if (!generator.CreateRoom(rect)) {
                    Debug.Log("Count = " + count + " number = " + number);
                    count++;
               }
            }
            if (count > number) {
                backGround = false;
                placeDesigns = true;
                count = 0;
            }
        } else if (backGround) {
            setEdges = true;
            backGround = false;
        }

        if (placeDesigns) {
            if (index < generator.Design.Count) {
                if (generator.Design[index].type != SquareData.OPEN) {
                    index++;
                }

                number = (generator.Design[index].endX - generator.Design[index].startX) * (generator.Design[index].endY - generator.Design[index].startY);
                if (generator.WantsMoreRoomsL()) {
                    if (!generator.CreateRoom(generator.Design[index]))
                        count++;
                    if (count > number)
                        index++;
                }
            } else  {
                placeDesigns = false;
                setEdges = true;
            }
        }
        //}
        
        if (setEdges) {
            setEdges = false;
            generator.SetEdges();
            generator.SetCeiling();
            //generator.SetLights();
            generator.PlaceTunnels();
            generator.PlaceAnteRooms();
            generator.PlaceRooms();
        }
        
        text.text = "Active Generation: " + generator.ActiveGeneration;
        text2.text = "Active Tunnelers: " + generator.GetTunnelerNum() + " Active Roomies: " + generator.GetRoomieNum();
    }
}
