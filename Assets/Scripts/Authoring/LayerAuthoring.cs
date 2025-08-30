using Unity.Entities;
using UnityEngine;

namespace Fundamental
{
    public class LayerAuthoring : MonoBehaviour
    {
        public LayerMask layer;

        public class Baker : Baker<LayerAuthoring>
        {
            public override void Bake(LayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new LayerComponent
                {
                    layer = (int)authoring.layer,
                });
            }
        }
    }

    public struct LayerComponent : IComponentData
    {
        public int layer;
    }
}
