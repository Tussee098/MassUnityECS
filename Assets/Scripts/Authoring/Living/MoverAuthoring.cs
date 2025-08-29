using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Life
{
    public class MoverAuthoring : MonoBehaviour
    {
        public float speed;
        public float turnRate;         // how fast they can rotate (radians/sec-ish; try 4–8)
        public float leashRadius;      // comfy distance from home before bias kicks in (e.g., 12)
        public float homePull;         // strength of home bias when beyond leash (e.g., 2)
        public float jitterStrength;   // how “curious” they are (0.1–0.5)
        public float minJitterPeriod;  // e.g., 0.4
        public float maxJitterPeriod;
        public class Baker : Baker<MoverAuthoring>
        {
            public override void Bake(MoverAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MoverComponent
                {
                    speed = authoring.speed,
                    turnRate = authoring.turnRate,
                    leashRadius = authoring.leashRadius,
                    homePull = authoring.homePull,
                    jitterStrength = authoring.jitterStrength,
                    minJitterPeriod = authoring.minJitterPeriod,
                    maxJitterPeriod = authoring.maxJitterPeriod,
                });

                AddComponent(entity, new WanderState());
            }
        }
    }
    public struct WanderState : IComponentData
    {
        public float2 dir;            // current heading (normalized)
        public float jitterTimer;    // (kept for your existing jitter)
        public float2 jitterVec;

        // New:
        public float decisionTimer;  // time until next “what to do” choice
        public float speedMul;       // 0 = pause, 1 = normal, >1 = brief sprint
        public float preferredRadius;// drifting target radius around home
        public float homesick;       // current home-bias weight (breathes)
        public float homesickTimer;  // when to resample homesick
    }


    public struct MoverComponent : IComponentData
    {
        public float speed;
        public float turnRate;         // how fast they can rotate (radians/sec-ish; try 4–8)
        public float leashRadius;      // comfy distance from home before bias kicks in (e.g., 12)
        public float homePull;         // strength of home bias when beyond leash (e.g., 2)
        public float jitterStrength;   // how “curious” they are (0.1–0.5)
        public float minJitterPeriod;  // e.g., 0.4
        public float maxJitterPeriod;  // e.g., 1.2
    }
}
