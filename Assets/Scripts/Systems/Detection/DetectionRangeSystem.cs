using System;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Fundamental;
using Cleanup;

namespace Living {
    [BurstCompile]
    [UpdateBefore(typeof(EntityDestroyerSystem))]
    public partial struct DetectionRangeSystem : ISystem
    {

        private EntityCommandBuffer _ecb;
        private ComponentLookup<LocalToWorld> _localToWorldLookup;
        private ComponentLookup<LayerComponent> _layerComponentLookup;

        private int _cellSize;
        private NativeParallelMultiHashMap<Cell, Entity> _cellEntityMap;
        public void OnCreate(ref SystemState state)
        {
            
            _cellSize = 1;
            _cellEntityMap = new NativeParallelMultiHashMap<Cell, Entity>(capacity: 16384, allocator: Allocator.Persistent);
            _layerComponentLookup = SystemAPI.GetComponentLookup<LayerComponent>();
            _localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>();
        }

        public void OnUpdate(ref SystemState state)
        {
            _localToWorldLookup.Update(ref state);
            _layerComponentLookup.Update(ref state);

            _ecb = SystemAPI
                .GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var clearCellsJob = new ClearCellsJob
            {
                Writer = _cellEntityMap
            }.Schedule(state.Dependency);

            var populateCellsJob = new PopulateCellsJob
            {
                CellSize = _cellSize,
                Writer = _cellEntityMap.AsParallelWriter(),
            }.ScheduleParallel(clearCellsJob);

            var inRangeCheckJob = new InRangeCheckJob
            {
                ECB = _ecb.AsParallelWriter(),
                CellEntityMapReference = _cellEntityMap,
                LocalToWorldLookup = _localToWorldLookup,
                LayerComponentLookup = _layerComponentLookup
            }.ScheduleParallel(populateCellsJob);

            state.Dependency = inRangeCheckJob;


        }
        public void OnDestroy(ref SystemState state)
        {
            if (_cellEntityMap.IsCreated) _cellEntityMap.Dispose();
        }
    }

    [BurstCompile]
    public partial struct ClearCellsJob : IJobEntity
    {
        public NativeParallelMultiHashMap<Cell, Entity> Writer;

        void Execute(Entity entity, in LocalToWorld transform /*, in YourTargetTag _optional */)
        {
            Writer.Clear();
        }
    }

    [BurstCompile]
    public partial struct PopulateCellsJob : IJobEntity
    {
        public float CellSize;
        public NativeParallelMultiHashMap<Cell, Entity>.ParallelWriter Writer;

        void Execute(Entity entity, in LocalToWorld transform /*, in YourTargetTag _optional */)
        {
            // If you only want some entities, add a tag parameter and require it in the query
            Cell cell = new Cell(transform.Position.xy);
            Writer.Add(cell, entity);
        }
    }

    [BurstCompile]
    [WithDisabled(typeof(CarryingComponent))]
    [WithDisabled(typeof(TargetEntityComponent))]
    public partial struct InRangeCheckJob : IJobEntity
    {
        public float CellSize;
        [ReadOnly] public NativeParallelMultiHashMap<Cell, Entity> CellEntityMapReference;

        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        [ReadOnly] public ComponentLookup<LayerComponent> LayerComponentLookup;

        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute([EntityIndexInQuery] int index,
                     Entity entity,
                     in LocalToWorld transform,
                     in SightRangeComponent sightRange,      // has .range and/or .rangeSq
                     in TargetMaskComponent targetMask)      // has .mask (int)
        {
            float2 myPos = transform.Position.xy;
            float r = sightRange.range;
            float r2 = r * r;

            Cell baseCell = new Cell(myPos);
            bool found = false;
            float2 foundPos = default;

            Entity targetEntity = entity;

            // Scan 3×3 neighbors
            // -1 and 1 should be -x and x 
            int x = (int)math.ceil(sightRange.range);

            for (int dy = -x; dy <= x && !found; dy++)
                for (int dx = -x; dx <= x && !found; dx++)
                {
                    var key = new Cell { x = baseCell.x + dx, y = baseCell.y + dy };
                    if (CellEntityMapReference.TryGetFirstValue(key, out var e, out var it))
                    {
                        do
                        {
                            // 1) Has transform?
                            if (!LocalToWorldLookup.TryGetComponent(e, out var eTx))
                                continue;
                            // 2) Range check (squared)
                            float2 d = eTx.Position.xy - myPos;
                            if (math.lengthsq(d) > r2)
                                continue;
                            // 3) Layer filter
                            if (!LayerComponentLookup.TryGetComponent(e, out var layerC))
                                continue;
                            if (((1u << layerC.layer) & 1u << targetMask.mask) == 0u)
                                continue;
                            // Passed all filters — keep it
                            found = true;
                            foundPos = eTx.Position.xy;
                            targetEntity = e;
                        } while (!found && CellEntityMapReference.TryGetNextValue(out e, ref it));
                    }
                }

            if (!found) return;

            // Write result
            ECB.SetComponent(index, entity, new TargetEntityComponent
            {
                position = foundPos,
                entity = targetEntity
            });

            ECB.SetComponentEnabled<TargetEntityComponent>(index, entity, true);

            
        }
    }
    public struct Cell : IEquatable<Cell>
    {
        public int x;
        public int y;
        public Cell(float2 f)
        {
            x = (int)f.x;
            y = (int)f.y;
        }
        public bool Equals(Cell other)
        {
            if (other.x == this.x && other.y == this.y)
            {
                return true;
            }
            return false;
        }
    }

    public struct UpdateCellTag : IComponentData, IEnableableComponent { }
}