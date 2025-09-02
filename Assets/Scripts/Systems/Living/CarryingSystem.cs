using Life;
using Mover;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Living
{

    public enum CarryingEnum
    {
        Food = 0,
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WanderThinkSystem))]
    [WithAll(typeof(CarryingComponent))]
    public partial struct CarryingSteerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CarryingComponent>();
            state.RequireForUpdate<Steering>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var job = new CarryingSteerJob().ScheduleParallel(state.Dependency);
            state.Dependency = job;
        }

        [BurstCompile]
        public partial struct CarryingSteerJob : IJobEntity
        {
            void Execute(ref Steering steering, in LocalTransform tf, in Home home)
            {
                float2 dir = math.normalizesafe(home.position - tf.Position.xy);
                steering.desiredDir = dir;
                steering.speedMul = math.max(steering.speedMul, 1f);
            }
        }
    }

    public struct CarryingComponent : IComponentData, IEnableableComponent
    {
        public CarryingEnum carryingEnum;
        public int amount;
    }
    
}
