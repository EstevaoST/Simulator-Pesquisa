using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LabyrinthObstacleGenerator : ObstacleGenerator
{
    [Header("Generator Settings")]
    public bool useWalkingMargin = true;
    public float minMargin = 0.8f;

    public override void Generate()
    {
        // base
        float n = roomLength - minMargin;
        n = n / (obstaclePrefab.localScale.z + minMargin);
        int oQty = (int)n;
        float oDist = roomLength / (n + 1);

        for (int i = 1; i <= oQty; i++)
        {
            Transform o = CreateObstacle();

            o.localScale = new Vector3(Mathf.Lerp(0, roomWidth - minMargin, obstacularity), 1, 1);
            float xPos = 0;
            if (i % 2 == 0)
                xPos = (+roomWidth - o.localScale.x) * 0.5f;
            else
                xPos = (-roomWidth + o.localScale.x) * 0.5f;
            o.localPosition = new Vector3(xPos, 0, oDist * i - roomLength * 0.5f);
        }
    }
}
