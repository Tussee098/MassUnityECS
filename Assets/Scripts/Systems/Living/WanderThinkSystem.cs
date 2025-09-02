using Life;
using Mover;
using Optimizing;
using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Unity.Mathematics.math;

namespace Living { 
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))] // read current positions if needed
    public partial struct WanderThinkSystem : ISystem
    {
        private uint _frame;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WanderState>();
            state.RequireForUpdate<Steering>();
            state.RequireForUpdate<ThinkBucket>();
        }

        public void OnUpdate(ref SystemState state)
        {
            _frame++;

            // Bucketing: only update 1/K each frame
            const int K = 6; // must match initializer (or move to a shared const)
            byte activeBucket = (byte)(_frame % K);

            var dt = SystemAPI.Time.DeltaTime;
            var now = (float)SystemAPI.Time.ElapsedTime;

            new ThinkJob
            {
                dt = dt,
                activeBucket = activeBucket
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ThinkJob : IJobEntity
        {
            public float dt;
            public byte activeBucket;

            // Minimal knobs
            const float TangentialWeight   = 0.72f;
            const float RadialWeight       = 0.26f;
            const float JitterStrength     = 0.12f;
            const float FallbackTurnRate   = 6f;   // used if mover.turnRate <= 0
            const float MinLeash           = 6f;   // fallback if authoring gave 0

            static float2 RandDir(ref Unity.Mathematics.Random rng)
            {
                return math.normalizesafe(rng.NextFloat2Direction(), new float2(1f, 0f));
            }

            void Execute(
                ref Steering steering,
                ref WanderState wander,
                ref IndividualRandomValue randomValue,
                in LocalTransform tf,
                in Home home,
                in MoverComponent mover,
                in ThinkBucket bucket
            )
            {
                if (bucket.bucket != activeBucket) return;

                var rng = randomValue.value;

                // --- Ring-follow steering ---
                float2 pos      = tf.Position.xy;
                float2 fromHome = pos - home.position;
                float  dist     = math.length(fromHome);

                // outward; if at home, use current heading
                float2 outward  = math.normalizesafe(fromHome, math.normalizesafe(wander.dir, new float2(1f, 0f)));

                // target point ON the ring
                float  leash    = math.max(mover.leashRadius, MinLeash);
                float2 targetOnRing = home.position + outward * leash;

                // radial dir toward ring (inward if too far, outward if too close)
                float2 radialDir = math.normalizesafe(targetOnRing - pos, outward);

                // tangential = current heading with outward component removed (perpendicular to outward)
                float  d        = math.dot(wander.dir, outward);
                float2 tangential = math.normalizesafe(wander.dir - d * outward, new float2(-outward.y, outward.x));

                // small jitter
                float2 jitter = RandDir(ref rng) * JitterStrength;

                // blend: mostly tangential (orbit), some radial (hold ring), plus jitter
                float2 desiredBase = tangential * TangentialWeight + radialDir * RadialWeight;
                float2 desired     = math.normalizesafe(desiredBase + jitter, desiredBase);

                // smooth turn (cheap)
                float turnRate = mover.turnRate > 0f ? mover.turnRate : FallbackTurnRate;
                float a = turnRate * dt;
                float turnLerp = a / (1f + a);

                float2 newDir = math.normalizesafe(math.lerp(wander.dir, desired, turnLerp), desired);

                // output & persist
                steering.desiredDir = newDir;
                steering.speedMul   = 1f;
                wander.dir          = newDir;
                randomValue.value   = rng;
            }
        }
    }
}