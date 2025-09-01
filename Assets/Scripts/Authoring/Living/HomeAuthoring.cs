using UnityEngine;
using Unity.Entities;
using System;

namespace Living
{
    public class HomeAuthoring : MonoBehaviour
    {
        public int startingFood;
        public class Baker : Baker<HomeAuthoring>
        {
            public override void Bake(HomeAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ResourcesComponent
                {
                    food = authoring.startingFood
                });

                AddComponent(entity, new AddResourcesComponent
                {
                    food = 0
                });
            }
        }
    }

    // We need to add every type of resource in the component.
    public struct AddResourcesComponent : IComponentData
    {
        public int food;

        
        public void Reset()
        {
            food = 0;
        }
    }
    public struct ResourcesComponent : IComponentData
    {
        public int food;
    }
}
