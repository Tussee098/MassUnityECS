using Unity.Entities;
using Life;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Living;

namespace Mover
{
    [BurstCompile]
    public partial struct MoverSystem : ISystem
    {

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MoverComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var dt = SystemAPI.Time.DeltaTime;

            new MoverJob
            {
                ECB = ecb.AsParallelWriter(),
                dt = dt
            }.ScheduleParallel();
        }


    }

    [BurstCompile]
    public partial struct MoverJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        public float dt;

        // Tunables (good defaults)
        const float DecisionRate = 1.2f;          // avg decisions per second (Poisson)
        const float SmallTurnMinDeg = 6f;
        const float SmallTurnMaxDeg = 20f;
        const float LargeTurnMinDeg = 60f;
        const float LargeTurnMaxDeg = 140f;
        const float PauseProb = 0.08f;      // chance the decision is “pause briefly”
        const float PauseMin = 0.25f, PauseMax = 0.6f;
        const float SprintProb = 0.06f;      // brief speed boost
        const float SprintMulMin = 1.25f, SprintMulMax = 1.6f;
        const float SprintDurMin = 0.35f, SprintDurMax = 0.8f;

        const float HomesickResampleEveryMin = 3f, HomesickResampleEveryMax = 7f;
        const float HomesickMin = 0.10f, HomesickMax = 0.55f; // scales homePull softly

        const float PrefRadiusSigma = 0.9f;       // random walk strength for preferredRadius

        static float ExpSample(ref Unity.Mathematics.Random rng, float rate)
        {
            // exponential inter-arrival time: mean 1/rate
            float u = math.max(1e-6f, rng.NextFloat());
            return -math.log(u) / math.max(1e-5f, rate);
        }

        static float2 Rotate(float2 v, float degrees)
        {
            float rad = math.radians(degrees);
            float s = math.sin(rad), c = math.cos(rad);
            return new float2(c * v.x - s * v.y, s * v.x + c * v.y);
        }
        static void MoveTowardsPosition(float2 position, ref LocalTransform tf, float dt, WanderState wander, MoverComponent mover)
        {
            var newDir = math.normalize(position.xy - tf.Position.xy);
            float speed = mover.speed * math.max(0f, wander.speedMul);
            float2 step = newDir * (speed * dt);
            tf.Position += new float3(step.x, step.y, 0f);
        }

        void Execute([EntityIndexInQuery] int index,
                    Entity entity,
                    ref LocalTransform tf,
                    ref IndividualRandomValue randomValue,
                    ref WanderState wander,
                    ref TargetPositionComponent targetPositionComponent,
                    in Home home,
                    in MoverComponent mover)
        {
            var rng = randomValue.value;

            if (targetPositionComponent.active)
            {
                MoveTowardsPosition(targetPositionComponent.position, ref tf, dt, wander, mover);
                if (math.distance(tf.Position.xy, targetPositionComponent.position) < 0.01f) return;
                targetPositionComponent.active = false;
                
            }
            // --- Init once ---
            if (!math.any(wander.dir != 0))
            {
                wander.dir = math.normalizesafe(rng.NextFloat2Direction(), new float2(1, 0));
                wander.jitterVec = rng.NextFloat2Direction() * mover.jitterStrength;
                wander.jitterTimer = rng.NextFloat(mover.minJitterPeriod, mover.maxJitterPeriod);

                // Start with a slightly perturbed preferred radius
                wander.preferredRadius = mover.leashRadius * rng.NextFloat(0.8f, 1.2f);
                wander.speedMul = 1f;
                wander.homesick = rng.NextFloat(HomesickMin, HomesickMax);
                wander.homesickTimer = rng.NextFloat(HomesickResampleEveryMin, HomesickResampleEveryMax);
                wander.decisionTimer = ExpSample(ref rng, DecisionRate);
            }
            

            // --- Low-frequency jitter (as you had) ---
            wander.jitterTimer -= dt;
            if (wander.jitterTimer <= 0f)
            {
                wander.jitterVec = rng.NextFloat2Direction() * mover.jitterStrength;
                wander.jitterTimer = rng.NextFloat(mover.minJitterPeriod, mover.maxJitterPeriod);
            }

            // --- Preferred radius: gentle mean-reverting random walk ---
            // drift toward leashRadius with noise so they don't stick to a single ring
            float toward = (mover.leashRadius - wander.preferredRadius) * 0.5f; // reversion speed
            float noise = (rng.NextFloat() * 2f - 1f) * PrefRadiusSigma * math.sqrt(dt);
            wander.preferredRadius += (toward * dt) + noise;

            // --- Homesick “breathing” (vary home bias strength over time) ---
            wander.homesickTimer -= dt;
            if (wander.homesickTimer <= 0f)
            {
                wander.homesick = rng.NextFloat(HomesickMin, HomesickMax);
                wander.homesickTimer = rng.NextFloat(HomesickResampleEveryMin, HomesickResampleEveryMax);
            }

            // --- Decision events (Poisson) ---
            wander.decisionTimer -= dt;
            if (wander.decisionTimer <= 0f)
            {
                float roll = rng.NextFloat();

                // Occasionally pause
                if (roll < PauseProb)
                {
                    wander.speedMul = 0f;
                    // next decision happens after pause ends
                    wander.decisionTimer = rng.NextFloat(PauseMin, PauseMax);
                }
                else
                {
                    // Resume normal speed and possibly sprint
                    wander.speedMul = 1f;
                    float turnRoll = rng.NextFloat();

                    if (turnRoll < 0.45f)
                    {
                        // Small veer left/right
                        float ang = rng.NextFloat(SmallTurnMinDeg, SmallTurnMaxDeg) * (rng.NextBool() ? 1f : -1f);
                        wander.dir = math.normalizesafe(Rotate(wander.dir, ang), wander.dir);
                    }
                    else if (turnRoll < 0.45f + 0.22f)
                    {
                        // Big turn / reorient
                        float ang = rng.NextFloat(LargeTurnMinDeg, LargeTurnMaxDeg) * (rng.NextBool() ? 1f : -1f);
                        wander.dir = math.normalizesafe(Rotate(wander.dir, ang), wander.dir);
                    }
                    // else: keep heading

                    // Optional brief sprint
                    if (rng.NextFloat() < SprintProb)
                    {
                        wander.speedMul = rng.NextFloat(SprintMulMin, SprintMulMax);
                        // lock sprint duration inside decision timer
                        wander.decisionTimer = rng.NextFloat(SprintDurMin, SprintDurMax);
                    }
                    else
                    {
                        // schedule next decision with exponential gap
                        wander.decisionTimer = ExpSample(ref rng, DecisionRate);
                    }
                }
            }

            // --- Home “suggestion” bias (soft, not enforcing) ---
            float2 pos = new float2(tf.Position.x, tf.Position.y);
            float2 fromHome = pos - home.pos;
            float dist = math.length(fromHome);
            float2 outward = dist > 1e-5f ? fromHome / dist : float2.zero;

            float error = dist - wander.preferredRadius;     // >0 too far, <0 too close (around drifting radius)
            float2 ringBias = outward * (-error) * mover.homePull * wander.homesick;

            // --- Compose desired direction & turn smoothly ---
            float2 desired = wander.dir + wander.jitterVec + ringBias;
            desired = math.normalizesafe(desired, wander.dir);

            float turnLerp = 1f - math.exp(-mover.turnRate * dt);
            float2 newDir = math.normalizesafe(math.lerp(wander.dir, desired, turnLerp), desired);

            // --- Move on XY; keep Z unchanged ---
            float speed = mover.speed * math.max(0f, wander.speedMul);
            float2 step = newDir * (speed * dt);
            tf.Position += new float3(step.x, step.y, 0f);

            //ECB.SetComponentEnabled<UpdateCellTag>(index, entity, true);

            // --- Persist ---
            wander.dir = newDir;
            randomValue.value = rng;
        }
        
    }

}
