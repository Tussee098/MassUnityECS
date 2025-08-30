using UnityEngine;
using Unity.Burst;
using Unity.Entities;

namespace Cleanup
{
    [BurstCompile]
    public partial struct EntityDestroyerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DestroyEntityTag>();
        }
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var job = new EntityDestroyerJob
            {
                ECB = ecb,
            }.Schedule(state.Dependency);
            job.Complete();
        }

        [BurstCompile]
        [WithAll(typeof(DestroyEntityTag))]
        public partial struct EntityDestroyerJob : IJobEntity
        {
            public EntityCommandBuffer ECB;

            public void Execute(Entity entity, EnabledRefRO<DestroyEntityTag> tag)
            {
                ECB.DestroyEntity(entity);
            }
        }
    }
    public struct DestroyEntityTag : IComponentData, IEnableableComponent{ }
}
