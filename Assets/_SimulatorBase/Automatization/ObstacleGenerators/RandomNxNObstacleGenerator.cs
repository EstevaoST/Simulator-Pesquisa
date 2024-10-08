using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomNxNObstacleGenerator : ObstacleGenerator
{
    [Space(10)]
    public float avgSize = 1;
    public int quantity = 1;
    [Space(10)]
    public float randomizationVariance = 0.6f;
    public float randomizationStep = 0.2f;

    [Space(10)]
    private int retries = 100;

    public override void Generate()
    {
        float totalSize = roomLength * roomWidth * 0.8f * obstacularity;
        quantity = Mathf.RoundToInt(Mathf.Sqrt(totalSize));
        avgSize = Mathf.Sqrt(totalSize / (float)quantity);

        float minSize = Mathf.Max(1, avgSize - randomizationVariance);
        float maxSize = avgSize + randomizationVariance;

        float[] arrSizes = new float[quantity];
        for (int i = 0; i < quantity; i++)
            arrSizes[i] = avgSize;

        for (int i = 0; i < quantity * 3; i++)
        {
            int x = Random.Range(0, quantity);
            int y = Random.Range(0, quantity);

            if (arrSizes[x] + randomizationStep <= maxSize &&
               arrSizes[y] - randomizationStep >= minSize)
            {
                arrSizes[x] += randomizationStep;
                arrSizes[y] -= randomizationStep;
            }
        }

        List<Transform> obsts = new List<Transform>();
        foreach (float size in arrSizes)
        {
            Transform t = this.CreateObstacle();
            float side = Mathf.Sqrt(size);
            t.localScale = new Vector3(side, 1, side);
            obsts.Add(t);
        }

        PlaceObstaclesRecursive(obsts, 0);
    }

    private bool PlaceObstaclesRecursive(List<Transform> obstacles, int depth)
    {
        if (depth >= obstacles.Count)
            return true;

        Transform t = obstacles[depth];
        
        Vector3 halfSize = t.localScale;
        Vector2 halfRoomSize = new Vector2(roomWidth, roomLength) * 0.5f;
        Vector2 min = new Vector2(halfSize.x, halfSize.z) - halfRoomSize;
        Vector2 max = new Vector2(roomWidth - halfSize.x, roomLength - halfSize.z) - halfRoomSize;
        
        int tries = 0;
        while (tries++ < retries)
        {
            t.position = new Vector3(Random.Range(min.x, max.x), 
                                     0, 
                                     Random.Range(min.y, max.y));
            bool collide = false;
            for(int i = depth - 1; i >= 0; i--)
            {
                if(SimpleCollisionCheck2D_XZ(t, obstacles[i]))
                {
                    collide = true;
                    break;
                }
            }

            if (!collide && PlaceObstaclesRecursive(obstacles, depth + 1))
                return true;                        
        }

        return false;
    }

    private bool SimpleCollisionCheck2D_XZ(Transform a, Transform b)
    {
        Vector2 aux = a.localScale.ToVec2XZ();
        Rect ar = new Rect(a.position.ToVec2XZ() - aux * 0.5f, aux);
        aux = b.localScale.ToVec2XZ();
        Rect br = new Rect(b.position.ToVec2XZ() - aux * 0.5f, aux);

        return ar.Overlaps(br);
    }


    // saving
    public static readonly string[] RandomNxNObstacleDataNames = { "Obst.Qty", "Obst.Size" };
    public override string[] ObstacleDataNames => RandomNxNObstacleDataNames;
    public override float[] GetObstacleData()
    {
        return new float[] { quantity, avgSize };
    }
}
