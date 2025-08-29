using Unity.Entities;
using UnityEngine;

namespace Living
{
    public class SightRangeAuthoring : MonoBehaviour
    {
        public float range;
        public LayerMask TargetLayers;
        public class Baker : Baker<SightRangeAuthoring>
        {
            public override void Bake(SightRangeAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SightRangeComponent
                {
                    range = authoring.range
                });

                AddComponent(entity, new TargetPositionComponent
                {
                    active = false,
                });

                AddComponent(entity, new TargetMaskComponent
                {
                    mask = (int)authoring.TargetLayers
                });
            }
        }
    }
    public struct SightRangeComponent : IComponentData
    {
        public float range;
    }
    public struct TargetMaskComponent : IComponentData
    {
        public int mask;
    }
}
