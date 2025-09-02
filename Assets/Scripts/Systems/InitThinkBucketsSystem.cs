using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Optimizing {

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct InitThinkBucketsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // only run if something still needs init
            state.RequireForUpdate<NeedsBucketInitTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            const int K = 6;

            new InitBucketsJob { K = K }.ScheduleParallel();

            // After assignment, remove the tag so it never runs again for this entity
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            new RemoveTagJob { ECB = ecb.AsParallelWriter() }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(NeedsBucketInitTag))]
        public partial struct InitBucketsJob : IJobEntity
        {
            public int K;

            void Execute(ref ThinkBucket bucket, in LocalTransform tf)
            {
                uint h = (uint)math.hash(tf.Position.xy);
                bucket.bucket = (byte)(h % (uint)K);
            }
        }

        [BurstCompile]
        public partial struct RemoveTagJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            void Execute([EntityIndexInQuery] int idx, Entity e, in NeedsBucketInitTag tag)
            {
                ECB.RemoveComponent<NeedsBucketInitTag>(idx, e);
            }
        }
    }
    public struct ThinkBucket : IComponentData
    {
        public byte bucket;
    }
    public struct NeedsBucketInitTag : IComponentData { }
}
