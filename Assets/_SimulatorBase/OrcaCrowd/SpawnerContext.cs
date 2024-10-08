using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OrcaSimulator.Core
{
    public class SpawnerContext : DecisionContext
    {

        static Color GIZMO_COLOR = new Color(0, 1, 0.5f, 0.4f);
        public int spawnTotal;
        public float spawnSpeed;
        private float cumulative = 0;
        private float size;
        private int spawnCount = 0;
        public bool StartSpawnAll = false;

        public override bool IsFinished => spawnTotal <= 0;

        public void SetSizeForFlow(float flow)
        {
            this.size = Mathf.CeilToInt(flow) * 2 * SimulationManager.manager.defaultAgentPrefab.radius;
            Vector3 s = transform.localScale;
            s.x = this.size;
            transform.localScale = s;
        }
        public override void Step(float time)
        {
            if (SimulationManager.manager.IsFirstFrame() && StartSpawnAll)
            {
                SpawnAll();
            }

            if (spawnTotal > 0)
            {
                cumulative += Mathf.Min(time * spawnSpeed, spawnTotal);
                while (cumulative >= 1)
                {
                    cumulative -= 1;
                    spawnTotal -= 1;
                    Spawn();
                }
            }

        }
        void Spawn()
        {
            var prefab = SimulationManager.manager.defaultAgentPrefab;
            var agents = GameObject.Find("Agents");

            Vector3 offset = Vector3.zero;

            if (size > 1)
            {
                float leftMax = -size * 0.5f + prefab.radius;
                float rightMax = size * 0.5f - prefab.radius;
                float current = (spawnCount % Mathf.CeilToInt(spawnSpeed)) * SimulationManager.manager.defaultAgentPrefab.radius;
                offset = transform.right * Mathf.Lerp(leftMax, rightMax, current / (size - 1));
            }


            Agent instance = Instantiate(prefab.gameObject, transform.position + offset, Quaternion.identity, agents.transform).GetComponent<Agent>();
            instance.name = "Agent_" + this.name;
            ChangeAgentTarget(instance);


            spawnCount++;
        }
        void SpawnAll()
        {
            float x = transform.localScale.x;
            float z = transform.localScale.z;
            float xrate = x / (x + z);
            float zrate = 1 - xrate;

            x = (int)(spawnTotal * xrate);
            z = (int)(spawnTotal * zrate);

            if (z > x)
            {
                x = (int)(Mathf.Sqrt(spawnTotal));
                z = (spawnTotal / x);
            }
            else
            {
                z = (int)(Mathf.Sqrt(spawnTotal));
                x = (spawnTotal / z);
            }

            var prefab = SimulationManager.manager.defaultAgentPrefab;
            var agents = GameObject.Find("Agents");
            Vector3 min = transform.position - transform.localScale / 2;
            Vector3 max = transform.position + transform.localScale / 2;
            for (int i = 0; i < z; i++)
            {
                float posz = Mathf.Lerp(min.z, max.z, ((float)i + 1) / (z + 1));
                for (int j = 0; j < x; j++)
                {
                    float posx = Mathf.Lerp(min.x, max.x, ((float)j + 1) / (x + 1));

                    //Vector3 pos = new Vector3(posx, transform.position.y, posz);
                    Vector3 pos = new Vector3(0, transform.position.y, 0) + transform.right * posx + transform.forward * posz;

                    Agent instance = Instantiate(prefab.gameObject, pos, Quaternion.identity, agents.transform).GetComponent<Agent>();
                    instance.name = "Agent_" + this.name;
                    ChangeAgentTarget(instance);


                    spawnCount++;
                    spawnTotal--;
                    if (spawnTotal <= 0)
                        return;
                }
            }

        }
        protected override void OnDrawGizmosSelected()
        {
            Gizmos.color = GIZMO_COLOR;
            //Gizmos.(transform.position, transform.position + transform.up * height, Gizmos.color, radius);
            foreach (var item in targets)
            {
                if (item.target != null)
                    Gizmos.DrawLine(transform.position + Vector3.up * height * 0.5f, item.target.transform.position + Vector3.up * item.target.height * 0.5f);
            }
        }

    }
}
