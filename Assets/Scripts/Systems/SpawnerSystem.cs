using Unity.Entities;
using Life;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;

namespace Mover
{
    [BurstCompile]
    public partial struct SpawnerSystem : ISystem
    {
        EntityCommandBuffer ecb;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnerComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            new SpawnerJob
            {
                ECB = ecb,
            }.Schedule();
        }
    }

    [BurstCompile]
    public partial struct SpawnerJob : IJobEntity
    {
        public EntityCommandBuffer ECB;
        public void Execute(
            Entity entity,
            LocalTransform transform,
            SpawnerComponent spawner, 
            IndividualRandomValue randomValue
            )
        {
            for (int i = 0; i < spawner.amount; i++)
            {
                var inst = ECB.Instantiate(spawner.prefab);
                var rng = randomValue.value;

                // Pick a random 2D position in [-10,10] range
                float2 rand2D = rng.NextFloat2(new float2(-spawner.contraints.x, -spawner.contraints.y), new float2(spawner.contraints.x, spawner.contraints.y));

                // Make it a float3 (z = 0 for 2D)
                float3 pos = new float3(transform.Position.x + rand2D.x, transform.Position.y + rand2D.y, 0);

                // Apply transform
                ECB.SetComponent(inst, LocalTransform.FromPositionRotationScale(
                    pos,
                    quaternion.identity,
                    spawner.scale
                ));
                ECB.AddComponent(inst, new Home
                {
                    entity = entity,
                    position = transform.Position.xy
                });
                ECB.SetComponent(inst, new MoverComponent
                {
                    speed = rng.NextFloat(spawner.speedRange.x, spawner.speedRange.y),
                    turnRate = rng.NextFloat(spawner.turnRateRange.x, spawner.turnRateRange.y),
                    leashRadius = rng.NextFloat(spawner.leashRadiusRange.x, spawner.leashRadiusRange.y),
                    homePull = rng.NextFloat(spawner.homePullRange.x, spawner.homePullRange.y),
                    jitterStrength = rng.NextFloat(spawner.jitterStrengthRange.x, spawner.jitterStrengthRange.y),
                    minJitterPeriod = rng.NextFloat(spawner.minJitterPeriodRange.x, spawner.minJitterPeriodRange.y),
                    maxJitterPeriod = rng.NextFloat(spawner.maxJitterPeriodRange.x, spawner.maxJitterPeriodRange.y),
                });
                randomValue.value = rng;
            }
            ECB.RemoveComponent<SpawnerComponent>(entity);
        }
    }

    public struct Home : IComponentData
    {
        public Entity entity;
        public float2 position;
    }
}
