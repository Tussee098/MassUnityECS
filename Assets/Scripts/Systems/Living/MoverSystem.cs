using Unity.Entities;
using Life;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Living;
using Unity.Collections;

namespace Mover
{

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))] // just integrates LocalTransform
    public partial struct MoverSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Steering>();
            state.RequireForUpdate<MoverComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            new MoverJob { dt = dt }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct MoverJob : IJobEntity
        {
            public float dt;

            void Execute(ref LocalTransform tf, in MoverComponent mover, in Steering steering)
            {
                float s = mover.speed * math.max(0f, steering.speedMul) * dt;
                float2 step = steering.desiredDir * s;
                tf.Position += new float3(step.x, step.y, 0f);
            }
        }
    }
}
