using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChurchHallObstacleGenerator : ObstacleGenerator
{
    [Header("Generator Settings")]

    public float altarLength = 1.0f;
    public float entranceLength = 1.0f;
    public float minWallMargin = 1.0f; // Neufert 409:3~6
    public float maxWallMargin = 1.2f; // Neufert 409:3~6
    public float minMiddleGap  = 1.6f; // Neufert 409:3~6
    public float maxMiddleGap  = 1.8f; // Neufert 409:3~6
    public float minBenchWidth = 5.0f; // Neufert 409:3~6
    public float minBenchLength = 0.55f; // Neufert 409:1~2
    public float minBenchGap   = 0.3f; // Neufert 409:1~2

    public override void Generate()
    {
        Rect room = new Rect(-roomWidth * 0.5f, -roomLength * 0.5f, roomWidth, roomLength);
        room.height -= altarLength + entranceLength;
        room.y += entranceLength;
        //if(room.width <= 6)
        //{
        //    // bench on side, one margin
        //}
        //else if(room.width <= 11)
        //{
        //    // bench on middle, two margins
        //}
        //else if(room.width <= 20)
        //{
        //    // two benches, one gap
        //}
        //else // if (room.width > 20)
        {
            // two benches, one gap, two margins
            float margin = Mathf.Lerp(maxWallMargin, minWallMargin, obstacularity);
            room.width -= 2 * margin;
            room.x += margin;

            float gap = Mathf.Lerp(maxMiddleGap, minMiddleGap, obstacularity);
            Rect lBenches = new Rect(room);
            lBenches.width = (lBenches.width - gap) * 0.5f;
            lBenches.x = lBenches.x;
            Rect rBenches = new Rect(room);
            rBenches.width = (rBenches.width - gap) * 0.5f;
            rBenches.x = rBenches.xMax + gap;
            
            // create left benches
            int nBenches = 1 + Mathf.FloorToInt((lBenches.height - minBenchLength) / (minBenchLength + minBenchGap));
            nBenches = Mathf.FloorToInt(Mathf.Lerp(0, nBenches, obstacularity));
            for(int i = 0; i < nBenches; i++)
            {
                var o = CreateObstacle();
                o.localScale = new Vector3(lBenches.width, 1, minBenchLength);

                o.position = lBenches.position.ToVec3XZ();
                o.position += new Vector3(o.localScale.x, 0, o.localScale.z) * 0.5f;
                o.position += Vector3.forward * i * (minBenchLength + minBenchGap);
            }

            // create right benches
            nBenches = 1 + Mathf.FloorToInt((rBenches.height - minBenchLength) / (minBenchLength + minBenchGap));
            nBenches = Mathf.FloorToInt(Mathf.Lerp(0, nBenches, obstacularity));
            for (int i = 0; i < nBenches; i++)
            {
                var o = CreateObstacle();
                o.localScale = new Vector3(rBenches.width, 1, minBenchLength);

                o.position = rBenches.position.ToVec3XZ();
                o.position += new Vector3(o.localScale.x, 0, o.localScale.z) * 0.5f;
                o.position += Vector3.forward * i * (minBenchLength + minBenchGap);
            }
        }


    }
}
