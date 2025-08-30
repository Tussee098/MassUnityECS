using Unity.Mathematics;
using Unity.Entities;

namespace Mover
{
    public partial struct SetRandomValueSystem : ISystem
    {
        EntityCommandBuffer ecb;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SetIndividualRandomValueTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var job = new SetRandomValueJob
            {
                ECB = ecb
            }.Schedule(state.Dependency);
            state.Dependency = job;

        }
    }

    public partial struct SetRandomValueJob : IJobEntity
    {
        public EntityCommandBuffer ECB;
        public void Execute(Entity entity, SetIndividualRandomValueTag tag)
        {
            Random newValue = Random.CreateFromIndex((uint)entity.Index + 1u);

            ECB.AddComponent(entity, new IndividualRandomValue
            {
                value = newValue,
            });
            ECB.RemoveComponent<SetIndividualRandomValueTag>(entity);
        }
    }

    public struct SetIndividualRandomValueTag : IComponentData { }
}
