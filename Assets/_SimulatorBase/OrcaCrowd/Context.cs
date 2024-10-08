using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace OrcaSimulator.Core
{
    public abstract class Context : MonoBehaviour
    {
        static Color GIZMO_COLOR = new Color(0, 0, 1, 0.4f);
        public float radius = 1, height = 1;
        public List<Agent> agents;
        public Collider col;

        public virtual bool IsFinished => true;

        private void Awake()
        {
            if (col == null)
                col = GetComponent<Collider>();
        }
        private void OnEnable()
        {
            SimulationManager.manager?.RegisterContext(this);
        }

        public virtual void Step(float time)
        {
            // do nothing
        }

        public virtual void EnterContext(Agent ag)
        {

        }
        public virtual void ExitContext(Agent ag)
        {

        }
        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = GIZMO_COLOR;
            Gizmos.DrawCube(transform.position, new Vector3(radius * 2, height, radius * 2));
        }

        public bool IsInside(Agent agent)
        {
            if (col == null)
            {
                Vector3 targetDist = agent.transform.position - transform.position;
                if (targetDist.y >= 0 && targetDist.y <= height) // agent is on the right height
                {
                    targetDist.y = 0;
                    if (targetDist.magnitude <= radius + agent.radius) // agent is inside
                        return true;
                }
                return false;
            }
            else
            {
                bool b = col.bounds.Contains(agent.transform.position);
                return b;
            }
        }

        public void GatherData(float time)
        {
            throw new NotImplementedException();
        }
        public virtual void Reset()
        {

        }
    }
}
