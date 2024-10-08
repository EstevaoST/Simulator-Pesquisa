using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OrcaSimulator.Core
{
    public class AgentDF : AgentORCA
    {

        public float minSpeed = 0.2f, maxSpeed = 1.2f;
        public float minDensity = 0.5f, maxDensity = 3;

        public override void Step(float time)
        {
            base.Step(time);


            float factor = Mathf.InverseLerp(maxDensity, minDensity, currentDensity);

            factor = 1.0f - ((currentDensity - minDensity) / (maxDensity - minDensity));

            speed = minSpeed + (maxSpeed - minSpeed) * factor;
            if (speed < 0)
                speed = 0;

        }
    }
}
