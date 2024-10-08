using UnityEngine;
using System.Collections;
using System;

namespace OrcaSimulator.Core
{
    public class Agent : MonoBehaviour
    {
        public static int collisionCount = 0;
        public const float density_side = 2;
        public const float density_area = density_side * density_side;

        public float radius = 0.2f;
        public float height = 1;
        public float speed = 0.8f;

        public float currentSpeed = 0;
        public float currentDensity = 0;
        public float walkedDistance = 0;
        public Context target;
        protected bool doCollision = true;

        // data
        public int simulatedFrames = 0;
        public float meanSpeed = 0;
        public float meanDensity = 0;
        static Vector3 aux;// so it doesn't need to create a new one each time

        public int simulatedFrames_temp = 0;
        public float meanSpeed_temp = 0;
        public float meanDensity_temp = 0;
        public float startTime_temp = 0;
        //

        protected UnityEngine.AI.NavMeshPath path;
        protected UnityEngine.AI.NavMeshPath Path
        {
            get
            {
                if (path == null)
                    path = new UnityEngine.AI.NavMeshPath();
                return path;
            }
        }
        protected int path_index;
        public Vector3 intendedPos;
        public Vector3 intendedVel;
        protected UnityEngine.AI.NavMeshHit nmh;

        // unity events
        private void OnEnable()
        {
            SimulationManager.manager?.UpdateAgentState(this);
        }
        private void OnDisable()
        {
            SimulationManager.manager?.UpdateAgentState(this);
        }
        protected virtual void Start()
        {
            //path = new NavMeshPath();
            intendedPos = Vector3.zero;

            nmh = new UnityEngine.AI.NavMeshHit();
            UnityEngine.AI.NavMesh.SamplePosition(transform.position, out nmh, float.PositiveInfinity, 0xFFFF);
            transform.position = nmh.position;

            SimulationManager.manager?.RegisterAgent(this);
        }


        public virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawRay(transform.position, intendedVel);


            if (Path != null && Path.status != UnityEngine.AI.NavMeshPathStatus.PathInvalid)
            {
                Gizmos.color = Color.red;
                //Gizmos.DrawLine(transform.position, Path.corners[0]);
                for (int i = 1; i < Path.corners.Length; i++)
                {
                    Gizmos.DrawLine(Path.corners[i - 1], Path.corners[i]);
                }
            }
            else if (target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, target.transform.position);
            }
        }

        // gets & sets
        public virtual void SetTarget(Context target)
        {
            this.target = target;
            if (target != null)
            {
                var col = target.GetComponent<Collider>();
                if (col != null)
                {


                    Vector3 tpos = target.GetComponent<Collider>().ClosestPointOnBounds(transform.position);
                    //Debug.DrawRay(tpos, Vector3.up, Color.green, 10);
                    //UnityEngine.AI.NavMesh.SamplePosition(tpos, out nmh, float.PositiveInfinity, 0xFFFF);
                    //UnityEngine.AI.NavMesh.SamplePosition(tpos, out nmh, Mathf.Infinity, 0xFFFF);
                    UnityEngine.AI.NavMesh.SamplePosition(tpos, out nmh, target.transform.localScale.magnitude * 4, 0xFFFF);

                    if (nmh.hit)
                    {
                        tpos = nmh.position;
                        //Debug.DrawRay(tpos, Vector3.up, Color.yellow, 10);
                        UnityEngine.AI.NavMesh.CalculatePath(transform.position, tpos, 0xFFFF, Path);
                    }

                }
                else
                    UnityEngine.AI.NavMesh.CalculatePath(transform.position, target.transform.position, 0xFFFF, Path);
            }
            else
                path = null;
            path_index = 1; // straight to the next point
        }

        // simulation events
        public virtual void IntentionStep(float time)
        {
            if (target != null)
            {
                SetTarget(target);
                if (Path.corners.Length < 2) // arrived at target
                {
                    target.EnterContext(this);
                    //SetTarget(null);
                    return;
                }

                intendedPos = Vector3.MoveTowards(transform.position, Path.corners[path_index], speed * time);
                intendedVel = (intendedPos - transform.position) / time;
            }

        }

        public virtual void Step(float time)
        {
            Vector3 result = intendedPos;

            //if (doCollision)
            //{
            //    aux = result;
            //    foreach (var other in Component.FindObjectsOfType<Agent>())
            //    {
            //        if (other == this)
            //            continue;

            //        if (Mathf.Abs(intendedPos.z - other.intendedPos.z) < height)
            //        {
            //            aux.z = other.intendedPos.z; // put them on the same Z just for comparison
            //            Vector3 repulsion = other.intendedPos - aux;
            //            if (repulsion.magnitude < (radius + other.radius))
            //            {
            //                result -= 0.5f * repulsion * (radius + other.radius - repulsion.magnitude);
            //                collisionCount++;
            //            }
            //        }
            //    }
            //    intendedPos = result;
            //}


            // sample result position on the navmesh
            //UnityEngine.AI.NavMesh.SamplePosition(result, out nmh, float.PositiveInfinity, 0xFFFF);
            if (UnityEngine.AI.NavMesh.SamplePosition(result, out nmh, intendedVel.magnitude * time * 4, 0xFFFF))
            {
                //result = 0.5f * result + 0.5f * nmh.position;
                result = nmh.position;


                if (float.IsInfinity(result.x))
                    result = transform.position;
                else
                    transform.LookAt(result);
            }
            else
                result = transform.position;

            // division space
            //SimulationManager.manager.MoveAgentDivisionSpace(this, transform.position, result);

            float dist = (result - transform.position).magnitude;
            walkedDistance += dist;
            currentSpeed = dist / time;
            transform.position = result;

            currentDensity = 1; // me
            foreach (Agent other in SimulationManager.manager.activeAgentList)
            {
                if (other == this)
                    continue;
                aux = transform.position - other.transform.position;
                if (Mathf.Abs(aux.x) < density_side * 0.5f && Mathf.Abs(aux.z) < density_side * 0.5f)
                    currentDensity++;
            }
            currentDensity /= density_area;


            // gather data
            meanSpeed = ((meanSpeed * simulatedFrames) + currentSpeed) / (simulatedFrames + 1);
            meanDensity = ((meanDensity * simulatedFrames) + currentDensity) / (simulatedFrames + 1);
            simulatedFrames++;

            meanSpeed_temp = ((meanSpeed_temp * simulatedFrames_temp) + currentSpeed) / (simulatedFrames_temp + 1);
            meanDensity_temp = ((meanDensity_temp * simulatedFrames_temp) + currentDensity) / (simulatedFrames_temp + 1);
            simulatedFrames_temp++;
            //

            if (target != null && target.IsInside(this))
                target.EnterContext(this);
        }

        public virtual void ResetTempData()
        {
            simulatedFrames_temp = 0;
            meanSpeed_temp = meanDensity_temp = 0;
            startTime_temp = SimulationManager.manager.simulatedTime;
        }

    }
}
