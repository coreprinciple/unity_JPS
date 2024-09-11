using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;

partial struct GridSpawnSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridPrefabComponent>();
        state.RequireForUpdate<JPSTagComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        GridPrefabComponent gridComponent = SystemAPI.GetSingleton<GridPrefabComponent>();

        float gridWidth = JPSDotsDataLinker.Instance().gridWidth;
        float gridHeight = JPSDotsDataLinker.Instance().gridHeight;
        int xSize = JPSDotsDataLinker.Instance().xSize;
        int ySize = JPSDotsDataLinker.Instance().ySize;

        float xOffset = gridWidth * 0.5f - xSize * gridWidth * 0.5f;
        float yOffset = gridHeight * 0.5f - ySize * gridHeight * 0.5f;
        int size = xSize * ySize;

        var instances = state.EntityManager.Instantiate(gridComponent.prefab, size, Allocator.Temp);
        int index = 0;

        foreach (var entity in instances)
        {
            state.EntityManager.AddComponent<GridComponent>(entity);
            state.EntityManager.AddComponent<URPMaterialPropertyBaseColor>(entity);

            int y = index / xSize;
            int x = index - y * xSize;

            var transform = SystemAPI.GetComponentRW<LocalTransform>(entity);
            transform.ValueRW.Scale = 0.08f;
            transform.ValueRW.Position = new float3(x * gridWidth + xOffset, 0.0f, y * gridHeight + yOffset);
            transform.ValueRW.RotateX(90.0f);

            var gridComp = SystemAPI.GetComponentRW<GridComponent>(entity);
            gridComp.ValueRW.index = index;

            index++;
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
