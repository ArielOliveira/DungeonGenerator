using Resources;
using UnityEngine;
public class CrawlerData {
    public Vector2Int location;
    public Direction direction, desiredDirection;
    public int age, maxAge, generation, stepLength, opening, corridorWidth, straightSingleSpawnProb, straightDoubleSpawnProb, turnSingleSpawnProb, turnDoubleSpawnProb, changeDirectionProb;
    public CrawlerData() {
        location = Vector2Int.zero;
        direction = Direction.XX;
        desiredDirection = Direction.XX;
    }
}
