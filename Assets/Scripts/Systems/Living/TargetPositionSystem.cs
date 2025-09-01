using Cleanup;
using Items;
using Life;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Living
{
    [BurstCompile]
    [UpdateBefore(typeof(EntityDestroyerSystem))]
    public partial struct TargetPositionSystem : ISystem
    {
        private ComponentLookup<CarryingItemComponent> _carryingItemComponentLookup;
        private ComponentLookup<DestroyEntityTag> _destroyEntityTagLookup;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetEntityComponent>();
            _destroyEntityTagLookup = SystemAPI.GetComponentLookup<DestroyEntityTag>();
            _carryingItemComponentLookup = SystemAPI.GetComponentLookup<CarryingItemComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = SystemAPI.
                GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            _destroyEntityTagLookup.Update(ref state);
            _carryingItemComponentLookup.Update(ref state);
            var job = new TargetPositionJob
            {
                ECB = ecb.AsParallelWriter(),
                DestroyEntityTagLookup = _destroyEntityTagLookup,
                CarryingItemComponentLookup = _carryingItemComponentLookup,
            }.ScheduleParallel(state.Dependency);
            state.Dependency = job;
        }

        [BurstCompile]
        [WithAll(typeof(TargetEntityComponent))]
        public partial struct TargetPositionJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<DestroyEntityTag> DestroyEntityTagLookup;
            [ReadOnly] public ComponentLookup<CarryingItemComponent> CarryingItemComponentLookup;
            public void Execute(
                [EntityIndexInQuery] int index,
                Entity entity,
                in LocalToWorld tf,
                ref TargetEntityComponent targetEntityComponent,
                in MoverComponent mover,
                EnabledRefRW<TargetEntityComponent> enabled
                )
            {
                if (math.distance(tf.Position.xy, targetEntityComponent.position) > 0.1f) return;

                ECB.SetComponentEnabled<TargetEntityComponent>(index, entity, false);
                if(CarryingItemComponentLookup.TryGetComponent(targetEntityComponent.entity, out CarryingItemComponent item))
                {
                    ECB.SetComponent(index, entity, new CarryingComponent
                    {
                        carryingEnum = item.itemType,
                        amount = item.amount
                    });
                    ECB.SetComponentEnabled<CarryingComponent>(index, entity, true);
                }
                
                //ECB.SetComponentEnabled<DestroyEntityTag>(index, targetEntityComponent.entity, true);
                
            }
        }
    }
    public struct TargetEntityComponent : IComponentData, IEnableableComponent
    {
        public float2 position;
        public Entity entity;
    }
}
