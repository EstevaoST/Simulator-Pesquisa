using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ClassroomObstacleGenerator : ObstacleGenerator
{
    [Header("Classroom Settings (Neufert - p214 - 2)")]
    public float sidesWallMargin = 0.5f;
    public float backWallMargin = 0.8f;    
    public float frontProfessorMargin = 1.7f;
    
    [Header("Classroom Table Settings (Neufert - p212 - 2)")]
    public Vector2 classTableSize = new Vector2(1.25f, 0.6f);
    public Vector2 classTableSpacing = new Vector2(0.6f, 0.65f);   

    public override void Generate()
    {
        //base.Generate();
        Vector3 origin = new Vector3(-roomWidth * 0.5f, 0, -roomLength * 0.5f);
        Rect source = new Rect(origin.x, origin.z, roomWidth, roomLength);

        // define areas
        Rect professorArea = new Rect(source);
        professorArea.x += sidesWallMargin;
        professorArea.y += backWallMargin;
        professorArea.width -= sidesWallMargin * 2;
        professorArea.height = frontProfessorMargin;

        Rect studentArea = new Rect(source);
        studentArea.y += frontProfessorMargin + backWallMargin;
        studentArea.height -= frontProfessorMargin + backWallMargin * 2;
        studentArea.width -= sidesWallMargin * 2;
        studentArea.x += sidesWallMargin;

        // define number of students columns and rows
        Vector3 classSize = ToVec3XZ(classTableSize, 1); // min values
        Vector3 classSpacing = ToVec3XZ(Vector2.Lerp(Vector2.Max((studentArea.size - classTableSize) * 0.5f, classTableSpacing), classTableSpacing, obstacularity)); // min values
        int columns = DivideArea(studentArea.width , classTableSize.x, classSpacing.x);
        int rows    = DivideArea(studentArea.height, classTableSize.y, classSpacing.z);
        Vector3 actualSpacing = Vector3.zero;
        actualSpacing.x = (studentArea.width  - classTableSize.x * columns) / (float)(columns + 1);
        actualSpacing.z = (studentArea.height - classTableSize.y * rows   ) / (float)(rows + 1);

        // place obstacles
        Vector3 halfClassSize = classSize / 2.0f;
        halfClassSize.y = 0;

        // Professor
        Transform professorTable = CreateObstacle();
        professorTable.localScale = classSize;
        professorTable.position = new Vector3(professorArea.x + halfClassSize.x, 0, professorArea.center.y);

        // Student
        Vector3 next = ToVec3XZ(studentArea.position) + actualSpacing + halfClassSize;
        for (int y = 0; y < rows; y++)
        {            
            for (int x = 0; x < columns; x++)
            {
                Transform table = CreateObstacle();
                table.localScale = classSize;
                table.position = next;

                // advance next
                next.x += classSize.x + actualSpacing.x;                
            }
            // advance next
            next.x = studentArea.position.x + actualSpacing.x + halfClassSize.x;
            next.z += classSize.z + actualSpacing.z;
        }
    }


    private Vector3 ToVec3XZ(Vector2 vec2, float y = 0)
    {
        return new Vector3(vec2.x, y, vec2.y);
    }
    private int DivideArea(float area, float size, float spacing)
    {
        if (area < size)
            return 0;

        return 1 + (int)((area - size) / (spacing + size));        
    }
}
