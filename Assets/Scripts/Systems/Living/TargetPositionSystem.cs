using Cleanup;
using Items;
using Life;
using Mover;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Living
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CarryingSteerSystem))]
    [WithAll(typeof(TargetEntityComponent))]
    public partial struct TargetSteerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetEntityComponent>();
            state.RequireForUpdate<Steering>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var job = new TargetSteerJob().ScheduleParallel(state.Dependency);
            state.Dependency = job;
        }

        [BurstCompile]
        public partial struct TargetSteerJob : IJobEntity
        {
            void Execute(ref Steering steering, in LocalTransform tf, in TargetEntityComponent target)
            {
                float2 dir = math.normalizesafe(target.position - tf.Position.xy);
                steering.desiredDir = dir;
                steering.speedMul = math.max(steering.speedMul, 1f);
            }
        }
    }
    public struct TargetEntityComponent : IComponentData, IEnableableComponent
    {
        public float2 position;
        public Entity entity;
    }
}
