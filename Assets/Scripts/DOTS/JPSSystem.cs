using Unity.Jobs;
using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[CreateAfter(typeof(GridSpawnSystem))]
partial struct JPSSystem : ISystem
{
    private NativeList<int> pathes;
    private bool _search;

    private int GetIndex(int x, int y, int xSize) => y * xSize + x;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridPrefabComponent>();
        state.RequireForUpdate<JPSTagComponent>();
        pathes = new NativeList<int>(Allocator.Persistent);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (JPSManaged.Instance().searchOrder)
        {
            JPSManaged.Instance().searchOrder = false;
            _search = true;
        }

        if (_search == false)
            return;

        _search = false;

        int xSize = JPSDotsDataLinker.Instance().xSize;
        int ySize = JPSDotsDataLinker.Instance().ySize;
        int startX = JPSDotsDataLinker.Instance().startX;
        int startY = JPSDotsDataLinker.Instance().startY;
        int endX = JPSDotsDataLinker.Instance().endX;
        int endY = JPSDotsDataLinker.Instance().endY;

        NativeArray<bool> obstacles = new NativeArray<bool>(xSize * ySize, Allocator.Temp);

        foreach (var obstacle in JPSDotsDataLinker.Instance().obstacles)
            obstacles[GetIndex(obstacle.x, obstacle.y, xSize)] = true;

        foreach (var (grid, color, entity) in SystemAPI.Query<GridComponent, RefRW<URPMaterialPropertyBaseColor>>().WithEntityAccess())
        {
            if (obstacles[grid.index])
                color.ValueRW.Value = new float4(1, 0, 0, 1);
            else if (grid.index == GetIndex(startX, startY, xSize))
                color.ValueRW.Value = new float4(0, 1, 0, 1);
            else if (grid.index == GetIndex(endX, endY, xSize))
                color.ValueRW.Value = new float4(0, 0.5f, 0.5f, 1);
            else
                color.ValueRW.Value = new float4(1, 1, 1, 1);
        }

        pathes.Clear();

        JPSJob job = new JPSJob();
        job.pathes = pathes;
        job.xSize = xSize;
        job.ySize = ySize;
        job.startX = startX;
        job.startY = startY;
        job.endX = endX;
        job.endY = endY;
        job.Run();

        foreach (var (grid, color, entity) in SystemAPI.Query<GridComponent, RefRW<URPMaterialPropertyBaseColor>>().WithEntityAccess())
        {
            if (grid.index == GetIndex(endX, endY, xSize))
                color.ValueRW.Value = new float4(0, 0.5f, 0.5f, 1);
            else
            {
                foreach (var path in job.pathes)
                {
                    if (grid.index == path)
                    {
                        color.ValueRW.Value = new float4(0, 0, 1, 1);
                        break;
                    }
                }
            }
        }
        obstacles.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        pathes.Dispose();
    }
}
