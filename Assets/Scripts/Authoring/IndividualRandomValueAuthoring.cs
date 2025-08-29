using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Mover
{
    public class IndividualRandomValueAuthoring : MonoBehaviour
    {
        public class Baker : Baker<IndividualRandomValueAuthoring>
        {
            public override void Bake(IndividualRandomValueAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SetIndividualRandomValueTag());
            }
        }
    }

    public struct IndividualRandomValue : IComponentData
    {
        public Unity.Mathematics.Random value;
    }
}
