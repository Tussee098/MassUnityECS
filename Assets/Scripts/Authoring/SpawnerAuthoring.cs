using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace Life
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public GameObject objectToSpawn;
        public int amount;
        public float scale;

        [SerializeField]
        public float2 speedRange;
        [SerializeField]
        public float2 turnRateRange;
        [SerializeField]
        public float2 leashRadiusRange;
        [SerializeField]
        public float2 homePullRange;
        [SerializeField]
        public float2 jitterStrengthRange;
        [SerializeField]
        public float2 minJitterPeriodRange;
        [SerializeField]
        public float2 maxJitterPeriodRange;
        [SerializeField]
        public float2 constraints;
        public class Baker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SpawnerComponent
                {
                    prefab = GetEntity(authoring.objectToSpawn, TransformUsageFlags.Dynamic),
                    amount = authoring.amount,
                    scale = authoring.scale,

                    speedRange = authoring.speedRange,
                    turnRateRange = authoring.turnRateRange,
                    leashRadiusRange = authoring.leashRadiusRange,
                    homePullRange = authoring.homePullRange,
                    minJitterPeriodRange = authoring.jitterStrengthRange,
                    maxJitterPeriodRange = authoring.minJitterPeriodRange,

                    contraints = authoring.constraints
                });
            }
        }
    }
    public struct SpawnerComponent : IComponentData, IEnableableComponent
    {
        public Entity prefab;
        public int amount;
        public float scale;
        public float2 speedRange;
        public float2 turnRateRange;
        public float2 leashRadiusRange;
        public float2 homePullRange;
        public float2 jitterStrengthRange;
        public float2 minJitterPeriodRange;
        public float2 maxJitterPeriodRange;

        public float2 contraints;
    }
}
