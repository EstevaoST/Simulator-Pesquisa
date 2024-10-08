using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace OrcaSimulator.Core
{
    public class AgentORCA : Agent
    {
        static float global_radius;
        const float RVO_EPSILON = 0.00001f;
        const int n_directions = 8;
        const int n_speeds = 1;
        public static float inverseORCAtime = 2;
        public float impatiance;
        public static int count = 0;

        public Vector3 updatedVel = Vector3.zero;

        // unity events
        protected void Awake()
        {
            global_radius = radius;

            //if (orcaDirections == null) {
            //    PopulateORCAdirections();
            //}
        }

        // simulation events
        public void AvoidanceStep(float time)
        {
            //avoidanceRotation = Quaternion.Euler(nmh.normal);
            //if (impatiance > 1)
            //{
            //    updatedVel = intendedVel;
            //    //doCollision = true;
            //    return;
            //}

            var ORCAlines = new List<Ray2D>();
            int numObstLines = 0;

            //ORCAlines.AddRange(CreateObstacleOrcaRays(time, inverseORCAtime));
            //numObstLines = ORCAlines.Count; // num of orca lines which are from obstacles, testing yet

            ORCAlines.AddRange(CreateAgentORCArays(time, inverseORCAtime));

            //foreach (var v in ORCAlines)
            //    Debug.DrawRay(Vec2to3XZ(v.origin) + transform.position, Vec2to3XZ(v.direction) * 20, Color.blue);

            Vector2 newVel = Vector3.zero;//Vec3to2XZ(intendedVel);
            int lineFail = FindOptimalVelocity(ORCAlines, speed, Vec3to2XZ(intendedVel), false, ref newVel);

            if (lineFail < ORCAlines.Count)
                RedoRays(ORCAlines, numObstLines, lineFail, speed, ref newVel);


            //foreach (var v in ORCAlines)
            //    Debug.DrawRay(Vec2to3XZ(v.origin) + transform.position, v.direction * 20, Color.blue);

            updatedVel = Vec2to3XZ(newVel); // TO DO: converter de 2D no plano da normal pro mundo 3D

            if (float.IsNaN(intendedPos.x))
                intendedPos = transform.position;

            //Debug.DrawRay(transform.position, intendedVel * inverseORCAtime, Color.black);
            //Debug.DrawRay(transform.position, updatedVel * inverseORCAtime, Color.yellow);
        }
        public override void Step(float time)
        {
            intendedVel = updatedVel;
            intendedPos = transform.position + intendedVel * time;
            base.Step(time);

            if (currentSpeed < speed * 0.05f)
            {

                impatiance += ((speed - currentSpeed) / speed) * time;
                //radius = global_radius * 0.5f + ((currentSpeed) / speed) * global_radius * 0.5f;
            }
            else
                impatiance = (1 - time) * impatiance;


            if (impatiance > 1)
            {
                count++;
                impatiance = 0;


                transform.position += transform.forward * time * Mathf.PI * Mathf.PI;

                // with all powers granted to me, I invoke the power of random to solve this matter
                //transform.position += Random.onUnitSphere * time;

            }
        }

        // ORCA methods

        // possível problema do ORCA atual: ele tá pra 2D
        // TO DO: projetar a posição dos outros agentes no plano da normal que consegue da navmesh ou no plano da direção inicial
        private List<Ray2D> CreateAgentORCArays(float time, float inverseORCAtime)
        {
            List<Ray2D> rays = new List<Ray2D>();

            foreach (var other in SimulationManager.manager.activeAgentList)
            //foreach (var other in SimulationManager.manager.GetNeighboursEnumerator(transform.position))
            {
                if (other == this)
                    continue; // ignore itself
                Ray2D ray = new Ray2D();// = GetORCAray(other, time);
                if (CreateAgentORCArray(other, time, ref ray, inverseORCAtime))
                    rays.Add(ray);
            }
            return rays;
        }
        private bool CreateAgentORCArray(Agent other, float time, ref Ray2D r, float inverseORCAtime)
        {
            Vector2 relativePosition = Vec3to2XZ(other.transform.position - transform.position);
            Vector2 relativeVelocity = Vec3to2XZ(intendedVel - other.intendedVel * SimulationManager.manager.ORCAOtherPercent);
            //float distSq = Mathf.Abs(Vector2.Dot(relativePosition, relativePosition));
            float distSq = relativePosition.sqrMagnitude;

            float combinedRadius = radius + other.radius;
            float combinedRadiusSq = Mathf.Pow(combinedRadius, 2);

            if (distSq > (relativePosition + relativeVelocity * inverseORCAtime).SqrMagnitude())
                return false; // too far away
            float ymin = Mathf.Min(transform.position.y, other.transform.position.y);
            float ymax = Mathf.Max(transform.position.y, other.transform.position.y);
            float height = ymin == transform.position.y ? this.height : other.height;
            if (ymax - ymin > height)
                return false; // too high away

            //Ray2D r = new Ray2D();
            Vector2 u;

            if (distSq > combinedRadiusSq) // if no collision
            {
                Vector2 w = relativeVelocity - inverseORCAtime * relativePosition;
                float wLengthSq = w.sqrMagnitude;// Mathf.Abs(Vector2.Dot(w, w));
                float dotProduct1 = Vector2.Dot(w, relativePosition);


                // project into a 2D cutoff circle/cone
                if (dotProduct1 < 0.0f && Mathf.Pow(dotProduct1, 2) > combinedRadiusSq * wLengthSq) // project on circle
                {
                    float wLength = Mathf.Sqrt(wLengthSq);
                    Vector2 unitW = w / wLength;

                    r.direction = new Vector2(unitW.y, -unitW.x);
                    u = (combinedRadius * inverseORCAtime - wLength) * unitW;
                }
                else // project on cone
                {
                    float leg = Mathf.Sqrt(distSq - combinedRadiusSq);

                    if (Determinant(relativePosition, w) > 0.0f) // Project on left side of cone.
                        r.direction = new Vector2(relativePosition.x * leg - relativePosition.y * combinedRadius,
                            relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                    else // Project on right side of cone
                        r.direction = -new Vector2(relativePosition.x * leg + relativePosition.y * combinedRadius,
                            -relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;

                    float dotProduct2 = Vector2.Dot(relativeVelocity, r.direction);
                    u = dotProduct2 * (Vector2)r.direction - relativeVelocity;
                }

            }
            else // collided
            {
                collisionCount++;
                /* Collision. Project on cut-off circle of time timeStep. */
                float invTimeStep = 1.0f / time;

                /* Vector from cutoff center to relative velocity. */
                Vector2 w = relativeVelocity - invTimeStep * relativePosition;

                float wLength = w.magnitude;
                Vector2 unitW = w / wLength;

                r.direction = new Vector2(unitW.y, -unitW.x);
                u = (combinedRadius * invTimeStep - wLength) * unitW;
            }

            if (other is AgentORCA && (other as AgentORCA).target != null)
                r.origin = Vec3to2XZ(intendedVel) + 0.5f * u;
            else
                r.origin = Vec3to2XZ(intendedVel) + u;
            //orcaLines_.Add(line);
            //r.direction = new Vector3(r.direction.x, 0, r.direction.y);
            return true;// r;
        }
        private bool CreateORCArray2(Agent other, float time, ref Ray2D r, float inverseORCAtime)
        {
            var angleAmount = Vector3.Angle(nmh.normal, Vector3.up);
            var angleDir = (nmh.normal - Vector3.up).normalized;
            var rotation = Quaternion.Euler(angleAmount * angleDir);

            Vector2 relativePosition = Vec3to2XZ(other.transform.position - transform.position);
            Vector2 relativeVelocity = Vec3to2XZ(intendedVel - other.intendedVel);
            float distSq = Mathf.Abs(Vector2.Dot(relativePosition, relativePosition));

            float combinedRadius = radius + other.radius;
            float combinedRadiusSq = Mathf.Pow(combinedRadius, 2);

            if (distSq > (relativePosition + relativeVelocity * inverseORCAtime).SqrMagnitude())
                return false;

            //Ray2D r = new Ray2D();
            Vector2 u;

            if (distSq > combinedRadiusSq) // if no collision
            {
                Vector2 w = relativeVelocity - inverseORCAtime * relativePosition;
                float wLengthSq = Mathf.Abs(Vector2.Dot(w, w));
                float dotProduct1 = Vector2.Dot(w, relativePosition);


                // project into a 2D cutoff circle/cone
                if (dotProduct1 < 0.0f && Mathf.Pow(dotProduct1, 2) > combinedRadiusSq * wLengthSq) // project on circle
                {
                    float wLength = Mathf.Sqrt(wLengthSq);
                    Vector2 unitW = w / wLength;

                    r.direction = new Vector2(unitW.y, -unitW.x);
                    u = (combinedRadius * inverseORCAtime - wLength) * unitW;
                }
                else // project on cone
                {
                    float leg = Mathf.Sqrt(distSq - combinedRadiusSq);

                    if (Determinant(relativePosition, w) > 0.0f) // Project on left side of cone.
                        r.direction = new Vector2(relativePosition.x * leg - relativePosition.y * combinedRadius,
                            relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                    else // Project on right side of cone
                        r.direction = -new Vector2(relativePosition.x * leg + relativePosition.y * combinedRadius,
                            -relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;

                    float dotProduct2 = Vector2.Dot(relativeVelocity, r.direction);
                    u = dotProduct2 * (Vector2)r.direction - relativeVelocity;
                }

            }
            else // collided
            {
                /* Collision. Project on cut-off circle of time timeStep. */
                float invTimeStep = 1.0f / time;

                /* Vector from cutoff center to relative velocity. */
                Vector2 w = relativeVelocity - invTimeStep * relativePosition;

                float wLength = w.magnitude;
                Vector2 unitW = w / wLength;

                r.direction = new Vector2(unitW.y, -unitW.x);
                u = (combinedRadius * invTimeStep - wLength) * unitW;
            }

            if (other is AgentORCA)
                r.origin = (Vector2)intendedVel + 0.5f * u;
            else
                r.origin = (Vector2)intendedVel + u;
            //orcaLines_.Add(line);
            //r.direction = new Vector3(r.direction.x, 0, r.direction.y);
            return true;// r;
        }
        private List<Ray2D> CreateObstacleOrcaRays(float time, float inverseORCAtime)
        {
            inverseORCAtime = 1.0f / inverseORCAtime;
            var orcaLines_ = new List<Ray2D>();
            var radius_ = 0.001f;
            for (int i = 0; i < SimulationManager.navmeshBorders.Count; ++i)
            {

                //Obstacle obstacle1 = obstacleNeighbors_[i].Value;
                //Obstacle obstacle2 = obstacle1.next_;
                var obstacle1 = SimulationManager.navmeshBorders[i].start;
                var obstacle2 = SimulationManager.navmeshBorders[i].end;

                Vector2 relativePosition1 = Vec3to2XZ(obstacle1 - transform.position);
                Vector2 relativePosition2 = Vec3to2XZ(obstacle2 - transform.position);

                /* Not yet covered. Check for collisions. */
                float distSq1 = relativePosition1.sqrMagnitude;
                float distSq2 = relativePosition2.sqrMagnitude;

                float radiusSq = Mathf.Sqrt(radius_);

                Vector2 obstacleVector = obstacle2 - obstacle1;
                float s = Vector2.Dot(-relativePosition1, obstacleVector) / obstacleVector.sqrMagnitude;
                float distSqLine = (-relativePosition1 - s * obstacleVector).sqrMagnitude;

                Ray2D line = new Ray2D();

                if (s < 0.0f && distSq1 <= radiusSq)
                {
                    /* Collision with left vertex. Ignore if non-convex. */
                    //if (obstacle1.convex_)
                    {
                        line.origin = new Vector2(0.0f, 0.0f);
                        line.direction = new Vector2(-relativePosition1.y, relativePosition1.x).normalized;
                        orcaLines_.Add(line);
                    }

                    continue;
                }
                else if (s > 1.0f && distSq2 <= radiusSq)
                    //{
                    //    /*
                    //     * Collision with right vertex. Ignore if non-convex or if
                    //     * it will be taken care of by neighboring obstacle.
                    //     */
                    //    //if (obstacle2.convex_ && RVOMath.det(relativePosition2, obstacle2.direction_) >= 0.0f)
                    if (Determinant(relativePosition2, obstacleVector.normalized) >= 0.0f)
                    {
                        line.origin = new Vector2(0.0f, 0.0f);
                        line.direction = new Vector2(-relativePosition2.y, relativePosition2.x).normalized;
                        orcaLines_.Add(line);
                    }

                    //    continue;
                    //}
                    else
                if (s >= 0.0f && s < 1.0f && distSqLine <= radiusSq)
                    {
                        /* Collision with obstacle segment. */
                        line.origin = new Vector2(0.0f, 0.0f);
                        line.direction = -obstacleVector.normalized;
                        orcaLines_.Add(line);

                        continue;
                    }

                /*
                 * No collision. Compute legs. When obliquely viewed, both legs
                 * can come from a single vertex. Legs extend cut-off line when
                 * non-convex vertex.
                 */

                Vector2 leftLegDirection, rightLegDirection;

                if (s < 0.0f && distSqLine <= radiusSq)
                {
                    /*
                     * Obstacle viewed obliquely so that left vertex
                     * defines velocity obstacle.
                     */
                    //if (!obstacle1.convex_)
                    //{
                    //    /* Ignore obstacle. */
                    //    continue;
                    //}

                    obstacle2 = obstacle1;

                    float leg1 = Mathf.Sqrt(distSq1 - radiusSq);
                    leftLegDirection = new Vector2(relativePosition1.x * leg1 - relativePosition1.y * radius_, relativePosition1.x * radius_ + relativePosition1.y * leg1) / distSq1;
                    rightLegDirection = new Vector2(relativePosition1.x * leg1 + relativePosition1.y * radius_, -relativePosition1.x * radius_ + relativePosition1.y * leg1) / distSq1;
                }
                else if (s > 1.0f && distSqLine <= radiusSq)
                {
                    /*
                     * Obstacle viewed obliquely so that
                     * right vertex defines velocity obstacle.
                     */
                    //if (!obstacle2.convex_)
                    //{
                    //    /* Ignore obstacle. */
                    //    continue;
                    //}

                    obstacle1 = obstacle2;

                    float leg2 = Mathf.Sqrt(distSq2 - radiusSq);
                    leftLegDirection = new Vector2(relativePosition2.x * leg2 - relativePosition2.y * radius_, relativePosition2.x * radius_ + relativePosition2.y * leg2) / distSq2;
                    rightLegDirection = new Vector2(relativePosition2.x * leg2 + relativePosition2.y * radius_, -relativePosition2.x * radius_ + relativePosition2.y * leg2) / distSq2;
                }
                else
                {
                    /* Usual situation. */
                    //if (obstacle1.convex_)
                    {
                        float leg1 = Mathf.Sqrt(distSq1 - radiusSq);
                        leftLegDirection = new Vector2(relativePosition1.x * leg1 - relativePosition1.y * radius_, relativePosition1.x * radius_ + relativePosition1.y * leg1) / distSq1;
                    }
                    //else
                    //{
                    //    /* Left vertex non-convex; left leg extends cut-off line. */
                    //    leftLegDirection = -obstacle1.direction_;
                    //}

                    //if (obstacle2.convex_)
                    {
                        float leg2 = Mathf.Sqrt(distSq2 - radiusSq);
                        rightLegDirection = new Vector2(relativePosition2.x * leg2 + relativePosition2.y * radius_, -relativePosition2.x * radius_ + relativePosition2.y * leg2) / distSq2;
                    }
                    //else
                    //{
                    //    /* Right vertex non-convex; right leg extends cut-off line. */
                    //    rightLegDirection = obstacle1.direction_;
                    //}
                }

                /*
                 * Legs can never point into neighboring edge when convex
                 * vertex, take cutoff-line of neighboring edge instead. If
                 * velocity projected on "foreign" leg, no constraint is added.
                 */

                //Obstacle leftNeighbor = obstacle1.previous_;

                //bool isLeftLegForeign = false;
                //bool isRightLegForeign = false;

                ////if (obstacle1.convex_ && Determinant(leftLegDirection, -leftNeighbor.direction_) >= 0.0f)
                //if (obstacle1.convex_ && Determinant(leftLegDirection, -leftNeighbor.direction_) >= 0.0f)
                //{
                //    /* Left leg points into obstacle. */
                //    leftLegDirection = -leftNeighbor.direction_;
                //    isLeftLegForeign = true;
                //}

                //if (obstacle2.convex_ && Determinant(rightLegDirection, obstacle2.direction_) <= 0.0f)
                //{
                //    /* Right leg points into obstacle. */
                //    rightLegDirection = obstacle2.direction_;
                //    isRightLegForeign = true;
                //}

                /* Compute cut-off centers. */

                Vector2 leftCutOff = inverseORCAtime * Vec3to2XZ(obstacle1 - transform.position);
                Vector2 rightCutOff = inverseORCAtime * Vec3to2XZ(obstacle2 - transform.position);
                Vector2 cutOffVector = rightCutOff - leftCutOff;

                /* Project current velocity on velocity obstacle. */

                /* Check if current velocity is projected on cutoff circles. */
                var velocity_ = Vec3to2XZ(this.intendedVel);

                float t = obstacle1 == obstacle2 ? 0.5f : Vector2.Dot((velocity_ - leftCutOff), cutOffVector) / cutOffVector.sqrMagnitude;
                float tLeft = Vector2.Dot(velocity_ - leftCutOff, leftLegDirection);
                float tRight = Vector2.Dot(velocity_ - rightCutOff, rightLegDirection);

                if ((t < 0.0f && tLeft < 0.0f) || (obstacle1 == obstacle2 && tLeft < 0.0f && tRight < 0.0f))
                {
                    /* Project on left cut-off circle. */
                    Vector2 unitW = (velocity_ - leftCutOff).normalized;

                    line.direction = new Vector2(unitW.y, -unitW.x);
                    line.origin = leftCutOff + radius_ * inverseORCAtime * unitW;
                    orcaLines_.Add(line);

                    continue;
                }
                else if (t > 1.0f && tRight < 0.0f)
                {
                    /* Project on right cut-off circle. */
                    Vector2 unitW = (velocity_ - rightCutOff).normalized;

                    line.direction = new Vector2(unitW.y, -unitW.x);
                    line.origin = rightCutOff + radius_ * inverseORCAtime * unitW;
                    orcaLines_.Add(line);

                    continue;
                }

                /*
                 * Project on left leg, right leg, or cut-off line, whichever is
                 * closest to velocity.
                 */
                float distSqCutoff = (t < 0.0f || t > 1.0f || obstacle1 == obstacle2) ? float.PositiveInfinity : (velocity_ - (leftCutOff + t * cutOffVector)).sqrMagnitude;
                float distSqLeft = tLeft < 0.0f ? float.PositiveInfinity : (velocity_ - (leftCutOff + tLeft * leftLegDirection)).sqrMagnitude;
                float distSqRight = tRight < 0.0f ? float.PositiveInfinity : (velocity_ - (rightCutOff + tRight * rightLegDirection)).sqrMagnitude;

                if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
                {
                    /* Project on cut-off line. */
                    line.direction = -obstacleVector.normalized;
                    line.origin = leftCutOff + radius_ * inverseORCAtime * new Vector2(-line.direction.y, line.direction.x);
                    orcaLines_.Add(line);

                    continue;
                }

                if (distSqLeft <= distSqRight)
                {
                    /* Project on left leg. */
                    //if (isLeftLegForeign)
                    //{
                    //    continue;
                    //}

                    line.direction = leftLegDirection;
                    line.origin = leftCutOff + radius_ * inverseORCAtime * new Vector2(-line.direction.y, line.direction.x);
                    orcaLines_.Add(line);

                    continue;
                }

                /* Project on right leg. */
                //if (isRightLegForeign)
                //{
                //    continue;
                //}

                line.direction = -rightLegDirection;
                line.origin = rightCutOff + radius_ * inverseORCAtime * new Vector2(-line.direction.y, line.direction.x);
                orcaLines_.Add(line);
            }
            return orcaLines_;
        }



        private int FindOptimalVelocity(IList<Ray2D> rays, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
        {
            if (directionOpt)
            {
                /*
                 * Optimize direction. Note that the optimization velocity is of
                 * unit length in this case.
                 */
                result = optVelocity * radius;
            }
            else if (Mathf.Abs(optVelocity.sqrMagnitude) > Mathf.Pow(radius, 2))
            {
                /* Optimize closest point and outside circle. */
                result = optVelocity.normalized * radius;
            }
            else
            {
                /* Optimize closest point and inside circle. */
                result = optVelocity;
            }

            for (int i = 0; i < rays.Count; ++i)
            {
                if (Determinant(rays[i].direction, rays[i].origin - result) > 0.0f)
                {
                    /* Result does not satisfy constraint i. Compute new optimal result. */
                    Vector2 tempResult = result;
                    if (!VerifyVelocity(rays, i, radius, optVelocity, directionOpt, ref result))
                    {
                        result = tempResult;

                        return i;
                    }
                }
            }

            return rays.Count;
        }
        private bool VerifyVelocity(IList<Ray2D> rays, int lineNo, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
        {
            float dotProduct = Vector2.Dot(rays[lineNo].origin, rays[lineNo].direction);
            float discriminant = Mathf.Pow(dotProduct, 2) + Mathf.Pow(radius, 2) - Mathf.Abs(rays[lineNo].origin.sqrMagnitude);

            if (discriminant < 0.0f)
            {
                /* Max speed circle fully invalidates line lineNo. */
                return false;
            }

            float sqrtDiscriminant = Mathf.Sqrt(discriminant);
            float tLeft = -dotProduct - sqrtDiscriminant;
            float tRight = -dotProduct + sqrtDiscriminant;

            for (int i = 0; i < lineNo; ++i)
            {
                float denominator = Determinant(rays[lineNo].direction, rays[i].direction);
                float numerator = Determinant(rays[i].direction, rays[lineNo].origin - rays[i].origin);

                if (Mathf.Abs(denominator) <= RVO_EPSILON)
                {
                    /* Lines lineNo and i are (almost) parallel. */
                    if (numerator < 0.0f)
                    {
                        return false;
                    }

                    continue;
                }

                float t = numerator / denominator;

                if (denominator >= 0.0f)
                {
                    /* Line i bounds line lineNo on the right. */
                    tRight = Mathf.Min(tRight, t);
                }
                else
                {
                    /* Line i bounds line lineNo on the left. */
                    tLeft = Mathf.Max(tLeft, t);
                }

                if (tLeft > tRight)
                {
                    return false;
                }
            }

            if (directionOpt)
            {
                /* Optimize direction. */
                if (Vector2.Dot(optVelocity, rays[lineNo].direction) > 0.0f)
                {
                    /* Take right extreme. */
                    result = rays[lineNo].origin + tRight * rays[lineNo].direction;
                }
                else
                {
                    /* Take left extreme. */
                    result = rays[lineNo].origin + tLeft * rays[lineNo].direction;
                }
            }
            else
            {
                /* Optimize closest point. */
                float t = Vector2.Dot(rays[lineNo].direction, (optVelocity - rays[lineNo].origin));

                if (t < tLeft)
                {
                    result = rays[lineNo].origin + tLeft * rays[lineNo].direction;
                }
                else if (t > tRight)
                {
                    result = rays[lineNo].origin + tRight * rays[lineNo].direction;
                }
                else
                {
                    result = rays[lineNo].origin + t * rays[lineNo].direction;
                }
            }

            return true;
        }



        private void RedoRays(List<Ray2D> rays, int numObstLines, int beginRay, float radius, ref Vector2 result)
        {
            float distance = 0.0f;

            for (int i = beginRay; i < rays.Count; ++i)
            {
                if (Determinant(rays[i].direction, rays[i].origin - result) > distance)
                {
                    /* Result does not satisfy constraint of line i. */
                    //List<Ray2D> projLines = new List<Ray2D>();
                    List<Ray2D> projLines = rays.GetRange(0, numObstLines);

                    //for (int ii = 0; ii < numObstLines; ++ii)
                    //{
                    //    projLines.Add(rays[ii]);
                    //}

                    for (int j = numObstLines; j < i; ++j)
                    {
                        Ray2D ray = new Ray2D();

                        float determinant = Determinant(rays[i].direction, rays[j].direction);

                        if (Mathf.Abs(determinant) <= RVO_EPSILON)
                        {
                            /* Line i and line j are parallel. */
                            if (Vector2.Dot(rays[i].direction, rays[j].direction) > 0.0f)
                            {
                                /* Line i and line j point in the same direction. */
                                continue;
                            }
                            else
                            {
                                /* Line i and line j point in opposite direction. */
                                ray.origin = 0.5f * (rays[i].origin + rays[j].origin);
                            }
                        }
                        else
                        {
                            ray.origin = rays[i].origin + (Determinant(rays[j].direction, rays[i].origin - rays[j].origin) / determinant) * rays[i].direction;
                        }

                        ray.direction = (rays[j].direction - rays[i].direction).normalized;
                        projLines.Add(ray);
                    }

                    Vector2 tempResult = result;
                    if (FindOptimalVelocity(projLines, radius, new Vector2(-rays[i].direction.y, rays[i].direction.x), true, ref result) < projLines.Count)
                    {
                        /*
                         * This should in principle not happen. The result is by
                         * definition already in the feasible region of this
                         * linear program. If it fails, it is due to small
                         * floating point error, and the current result is kept.
                         */
                        result = tempResult;
                    }

                    distance = Determinant(rays[i].direction, rays[i].origin - result);
                }
            }
        }

        internal static float Determinant(Vector2 vector1, Vector2 vector2)
        {
            return vector1.x * vector2.y - vector1.y * vector2.x;
        }
        internal static Vector2 Vec3to2XZ(Vector3 v3)
        {
            return new Vector2(v3.x, v3.z);
        }
        internal static Vector3 Vec2to3XZ(Vector2 v2)
        {
            return new Vector3(v2.x, 0, v2.y);
        }
    }
}