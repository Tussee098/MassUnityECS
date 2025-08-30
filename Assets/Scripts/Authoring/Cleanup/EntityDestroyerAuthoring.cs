using Unity.Entities;
using UnityEngine;

namespace Cleanup
{
    public class EntityDestroyerAuthoring : MonoBehaviour
    {
        public class Baker : Baker<EntityDestroyerAuthoring>
        {
            public override void Bake(EntityDestroyerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent<DestroyEntityTag>(entity);
                SetComponentEnabled<DestroyEntityTag>(entity, false);
            }
        }
    }

}
