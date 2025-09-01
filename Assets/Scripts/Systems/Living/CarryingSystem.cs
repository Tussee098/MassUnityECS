using Life;
using Mover;
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
    public partial struct CarryingSystem : ISystem
    {
        private ComponentLookup<AddResourcesComponent> _addResourcesComponentLookup;
        private EntityCommandBuffer ecb;
        public void OnCreate(ref SystemState state) 
        {
            state.RequireForUpdate<CarryingComponent>();
            _addResourcesComponentLookup = SystemAPI.GetComponentLookup<AddResourcesComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            _addResourcesComponentLookup.Update(ref state);

            var job = new CarryingJob
            {
                ECB = ecb.AsParallelWriter(),
                AddResourcesComponentLookup = _addResourcesComponentLookup
            }.ScheduleParallel(state.Dependency);
            state.Dependency = job;

        }
    }

    [BurstCompile]
    [WithAll(typeof(CarryingComponent))]
    public partial struct CarryingJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<AddResourcesComponent> AddResourcesComponentLookup;
        
        public void Execute(
            [EntityIndexInQuery] int index,
            Entity entity,
            EnabledRefRW<CarryingComponent> enabledCarryingComponent,
            ref CarryingComponent carryingComponent,
            in MoverComponent mover,
            in LocalToWorld tf,
            in Home home
            )
        {
            if (math.distance(tf.Position.xy, home.position) > 0.1f) return;

            int amount = carryingComponent.amount;
            carryingComponent.amount = 0;
            enabledCarryingComponent.ValueRW = false;

            if (!AddResourcesComponentLookup.TryGetComponent(home.entity, out AddResourcesComponent addResourcesComponent)) return;

            ECB.SetComponent(index, home.entity, new AddResourcesComponent
            {
                food = addResourcesComponent.food = amount
            });
            



        }
    }

    public struct CarryingComponent : IComponentData, IEnableableComponent
    {
        public CarryingEnum carryingEnum;
        public int amount;
    }
}
