using UnityEngine;
using Unity.Entities;
using Unity.Burst;

namespace Living
{
    [BurstCompile]
    public partial struct HomeAddResourceSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AddResourcesComponent>();
            state.RequireForUpdate<ResourcesComponent>();
        }
    
        public void OnUpdate(ref SystemState state)
        {
            var job = new HomeAddResourcesJob
            {

            }.ScheduleParallel(state.Dependency);
            state.Dependency = job;
        }
    }

    [BurstCompile]
    [WithAll(typeof(ResourcesComponent))]
    public partial struct HomeAddResourcesJob : IJobEntity
    {
        public void Execute(
            [EntityIndexInQuery] int index,
            ref ResourcesComponent resourcesComponent,
            ref AddResourcesComponent addResourcesComponent
            )
        {
            resourcesComponent.food += addResourcesComponent.food;


            addResourcesComponent.Reset();
        }
    }
}
