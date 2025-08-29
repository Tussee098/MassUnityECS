using Unity.Entities;
using Unity.Mathematics;

namespace Living
{
    public partial struct TargetPositionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetPositionComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {

        }
    }
    public struct TargetPositionComponent : IComponentData
    {
        public bool active;
        public float2 position;
    }
}
