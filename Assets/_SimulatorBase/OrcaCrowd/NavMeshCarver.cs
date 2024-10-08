using UnityEngine;
using System.Collections;

public class NavMeshCarver : MonoBehaviour {

    public Mesh toCarve;

	// Use this for initialization
	void Start () {
        var carve = new GameObject();
        var obstacle = carve.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        obstacle.carving = true;
        obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
        

	}
	
}
