using UnityEngine;
using Unity.Entities;
using Living;
using System.ComponentModel;

namespace Items
{
    
    public class CarryingItemAuthoring : MonoBehaviour
    {
        public CarryingEnum carryingType;
        public int amount;

        public class Baker : Baker<CarryingItemAuthoring>
        {
            public override void Bake(CarryingItemAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CarryingItemComponent
                {
                    itemType = authoring.carryingType,
                    amount = authoring.amount
                });
            }
        }
    }
    public struct CarryingItemComponent : IComponentData
    {
        public CarryingEnum itemType { get; set; }
        public int amount;
    }
}

