using Algorithm;
using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Collections;
using static Algorithm.PathAlgorithmHelper;
using UnityEditor.Experimental.GraphView;
using System;

[CreateAfter(typeof(GridSpawnSystem))]
partial struct JPSSystem : ISystem, ISystemStartStop
{
    public int xSize, ySize;
    public int startX, startY, endX, endY;

    private int GetIndex(int x, int y, int xSize) => y * xSize + x;

    public void OnStartRunning(ref SystemState state)
    {
    }

    public void OnStopRunning(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridPrefabComponent>();
        state.RequireForUpdate<JPSTagComponent>();
    }

    private void AddPath(int index, ref NativeList<int> pathes)
    {
        if (pathes.Contains(index))
            return;

        int y = index / xSize;
        int x = index - y * xSize;
        
        pathes.Add(index);
    }

    #region connect nodes between
    private void ConnectNodesBetween(PathNode current, PathNode before, ref NativeArray<PathNode> pathNodes, ref NativeList<int> pathes)
    {
        if (current.x == before.x)
            ConnectHorizontal(current, before, ref pathNodes, ref pathes);
        else if (current.y == before.y)
            ConnectVertical(current, before, ref pathNodes, ref pathes);
        else
            ConnectNodesDiagonal(current, before, ref pathNodes, ref pathes);
    }

    private void ConnectNodesDiagonal(PathNode current, PathNode before, ref NativeArray<PathNode> pathNodes, ref NativeList<int> pathes)
    {
        int size = System.Math.Abs(current.x - before.x) - 1;
        int addH = current.x < before.x ? 1 : -1;
        int addV = current.y < before.y ? 1 : -1;
        int x = current.x;
        int y = current.y;
        int count = 0;

        while (count < size)
        {
            count++;

            PathNode newNode = pathNodes[GetIndex(x + addH, y + addV, xSize)];
            newNode.beforeIndex = current.index;
            newNode.g = CalcG(x, y, newNode.x, newNode.y);
            newNode.UpdateF();
            pathNodes[newNode.index] = newNode;
            AddPath(newNode.index, ref pathes);

            x += addH;
            y += addV;
        }
    }

    private void ConnectVertical(PathNode current, PathNode before, ref NativeArray<PathNode> pathNodes, ref NativeList<int> pathes)
    {
        int pivot = current.x;
        int size = System.Math.Abs(current.x - before.x) - 1;
        int add = current.x < before.x ? 1 : -1;
        int count = 0;

        while (count < size)
        {
            int beforeIndex = GetIndex(pivot, current.y, xSize);

            PathNode newNode = pathNodes[GetIndex(pivot + add, current.y, xSize)];
            newNode.beforeIndex = current.index;
            newNode.g = CalcG(pivot, current.y, newNode.x, newNode.y);
            newNode.UpdateF();
            pathNodes[newNode.index] = newNode;
            AddPath(newNode.index, ref pathes);

            pivot = newNode.x;
            count++;
        }
    }

    private void ConnectHorizontal(PathNode current, PathNode before, ref NativeArray<PathNode> pathNodes, ref NativeList<int> pathes)
    {
        int pivot = current.y;
        int size = System.Math.Abs(current.y - before.y) - 1;
        int add = current.y < before.y ? 1 : -1;
        int count = 0;

        while (count < size)
        {
            PathNode newNode = pathNodes[GetIndex(current.x, pivot + add, xSize)];
            newNode.beforeIndex = current.index;
            newNode.g = CalcG(current.x, pivot, newNode.x, newNode.y);
            newNode.UpdateF();
            pathNodes[newNode.index] = newNode;
            AddPath(newNode.index, ref pathes);

            pivot = newNode.y;
            count++;
        }
    }
    #endregion

    private void SearchPath(ref SystemState state, int2 start, int2 end)
    {
        if (start.x == end.x && start.y == end.y)
            return;

        int2 gridSize = new int2(xSize, ySize);
        NativeArray<PathNode> pathNodes = new NativeArray<PathNode>(xSize * ySize, Allocator.Temp);
        NativeArray<bool> obstacles = new NativeArray<bool>(xSize * ySize, Allocator.Temp);

        foreach (var obstacle in JPSDotsDataLinker.Instance().obstacles)
            obstacles[GetIndex(obstacle.x, obstacle.y, xSize)] = true;

        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                PathNode node = new PathNode();
                node.beforeIndex = -1;
                node.index = GetIndex(x, y, xSize);
                node.x = x;
                node.y = y;
                node.g = 100000;
                node.h = CalcH(x, y, end.x, end.y);
                node.isObstacle = obstacles[node.index];
                node.UpdateF();

                pathNodes[node.index] = node;
            }
        }

        foreach (var (grid, color, entity) in SystemAPI.Query<GridComponent, RefRW<URPMaterialPropertyBaseColor>>().WithEntityAccess())
        {
            if (pathNodes[grid.index].isObstacle)
                color.ValueRW.Value = new float4(1, 0, 0, 1);
            else if (grid.index == GetIndex(start.x, start.y, xSize))
                color.ValueRW.Value = new float4(0, 1, 0, 1);
            else if (grid.index == GetIndex(end.x, end.y, xSize))
                color.ValueRW.Value = new float4(0, 0.5f, 0.5f, 1);
            else
                color.ValueRW.Value = new float4(1, 1, 1, 1);
        }

        NativeList<int> openNodes = new NativeList<int>(Allocator.Temp);
        NativeList<int> closedNodes = new NativeList<int>(Allocator.Temp);
        NativeList<int> tempPathes = new NativeList<int>(Allocator.Temp);
        NativeList<int> pathes = new NativeList<int>(Allocator.Temp);

        PathNode startNode = pathNodes[GetIndex(start.x, start.y, xSize)];
        openNodes.Add(startNode.index);

        bool found = false;

        while (openNodes.Length > 0)
        {
            for (int o = 0; o < openNodes.Length; o++)
            {
                int pivotF = int.MaxValue;
                int pivotIndex = openNodes[o];

                if (pathNodes[openNodes[o]].f < pivotF)
                {
                    pivotF = pathNodes[openNodes[o]].f;
                    pivotIndex = openNodes[o];
                }

                found = Search(pivotIndex, start, end, ref pathNodes, ref openNodes, ref closedNodes, ref tempPathes);

                if (found)
                    break;
            }

            if (found)
                break;
        }

        if (found)
        {
            PathNode debugRouteNode = pathNodes[GetIndex(end.x, end.y, xSize)];

            while (debugRouteNode.beforeIndex >= 0)
            {
                UnityEngine.Debug.Log(debugRouteNode.x + ", " + debugRouteNode.y);

                if (debugRouteNode.beforeIndex < 0)
                    break;

                debugRouteNode = pathNodes[debugRouteNode.beforeIndex];
            }

            PathNode pathNode = pathNodes[GetIndex(end.x, end.y, xSize)];

            while (pathNode.x >= 0 && pathNode.y >= 0)
            {
                pathes.Add(pathNode.index);

                if (pathNode.x == start.x && pathNode.y == start.y)
                    break;

                if (pathNode.beforeIndex < 0)
                    break;

                pathNode = pathNodes[pathNode.beforeIndex];

                if (pathNode.x >= 0 && pathNode.y >= 0)
                    ConnectNodesBetween(pathNodes[pathes[pathes.Length - 1]], pathNode, ref pathNodes, ref pathes);
            }

            foreach (var (grid, color, entity) in SystemAPI.Query<GridComponent, RefRW<URPMaterialPropertyBaseColor>>().WithEntityAccess())
            {
                if (grid.index == GetIndex(end.x, end.y, xSize))
                    color.ValueRW.Value = new float4(0, 0.5f, 0.5f, 1);
                else
                {
                    foreach (var path in pathes)
                    {
                        if (grid.index == path)
                        {
                            color.ValueRW.Value = new float4(0, 0, 1, 1);
                            break;
                        }
                    }
                }
            }
        }

        obstacles.Dispose();
        pathes.Dispose();
        tempPathes.Dispose();
        openNodes.Dispose();
        closedNodes.Dispose();
        pathNodes.Dispose();
    }

    private bool Search(int index, int2 start, int2 end, ref NativeArray<PathNode> pathNodes, ref NativeList<int> openNodes, ref NativeList<int> closedNodes, ref NativeList<int> pathes)
    {
        closedNodes.Add(index);
        AddPath(index, ref pathes);

        for (int d = 0; d < DiagonalArounds.Length; d++)
        {
            int nextX = pathNodes[index].x;
            int nextY = pathNodes[index].y;
            int beforeX = nextX;
            int beforeY = nextY;

            while (true)
            {
                if (nextX == end.x && nextY == end.y)
                    return true;

                if (IsOutOfRange(nextX, nextY, xSize, ySize))
                    break;

                PathNode neighborNode = pathNodes[GetIndex(nextX, nextY, xSize)];

                if (neighborNode.isObstacle)
                    break;

                SearchDir diagonalDir = DiagonalArounds[d].dir;
                CoordinatePair stepPair = GetDiagonalCoordPair(diagonalDir);

                neighborNode.beforeIndex = index;
                neighborNode.g = CalcG(beforeX, beforeY, nextX, nextY);
                neighborNode.UpdateF();

                //neighborNode.beforeIndex = index;
                //neighborNode.g = CalcG(beforeX, beforeY, nextX, nextY);
                //neighborNode.UpdateF();
                //pathNodes[neighborNode.index] = neighborNode;

                bool found = SearchByStep(neighborNode, start, end, stepPair.horizontal, diagonalDir, true, ref pathNodes, ref openNodes, ref closedNodes, ref pathes);

                if (found == false)
                    found = SearchByStep(neighborNode, start, end, stepPair.vertical, diagonalDir, false, ref pathNodes, ref openNodes, ref closedNodes, ref pathes);

                if (found)
                    pathNodes[neighborNode.index] = neighborNode;

                beforeX = nextX;
                beforeY = nextY;
                nextX = beforeX + DiagonalArounds[d].x;
                nextY = beforeY + DiagonalArounds[d].y;

                if (found)
                    return true;
            }
        }

        return false;
    }

    private bool SearchByStep(PathNode parentNode, int2 start, int2 end, Coordinate stepCoordinate, SearchDir diagonalDir, bool isHorizon, ref NativeArray<PathNode> pathNodes, ref NativeList<int> openNodes, ref NativeList<int> closedNodes, ref NativeList<int> pathes)
    {
        int nextX = parentNode.x;
        int nextY = parentNode.y;
        int beforeX;
        int beforeY;

        while (true)
        {
            beforeX = nextX;
            beforeY = nextY;
            nextX = nextX + stepCoordinate.x;
            nextY = nextY + stepCoordinate.y;

            if (IsOutOfRange(nextX, nextY, xSize, ySize))
                return false;

            PathNode dest = pathNodes[GetIndex(nextX, nextY, xSize)];

            if (dest.isObstacle)
                return false;

            if (nextX == end.x && nextY == end.y)
            {
                AddPath(parentNode.index, ref pathes);

                dest.beforeIndex = parentNode.index;
                dest.g = CalcG(parentNode.x, parentNode.y, nextY, nextY);
                dest.UpdateF();
                pathNodes[dest.index] = dest;

                AddPath(dest.index, ref pathes);

                return true;
            }

            if (CheckForceNeighbor(nextX, nextY, out int neighborX, out int neighborY, diagonalDir, isHorizon, pathNodes))
            {
                PathNode currentNode = pathNodes[GetIndex(nextX, nextY, xSize)];
                currentNode.beforeIndex = parentNode.index;
                currentNode.g = CalcG(parentNode.x, parentNode.y, nextX, nextY);
                currentNode.UpdateF();
                pathNodes[currentNode.index] = currentNode;

                PathNode neighbor = pathNodes[GetIndex(neighborX, neighborY, xSize)];
                neighbor.beforeIndex = currentNode.index;
                neighbor.g = CalcG(nextX, nextY, neighborX, neighborY);
                neighbor.UpdateF();
                pathNodes[neighbor.index] = neighbor;

                if (!closedNodes.Contains(neighbor.index) && !openNodes.Contains(neighbor.index))
                    openNodes.Add(neighbor.index);

                pathNodes[parentNode.index] = parentNode;

                AddPath(parentNode.index, ref pathes);
                AddPath(currentNode.index, ref pathes);
                AddPath(neighbor.index, ref pathes);
            }
        }
    }

    private bool CheckForceNeighbor(int x, int y, out int neighborX, out int neighborY, SearchDir diagonalDir, bool isHorizon, NativeArray<PathNode> pathNodes)
    {
        bool isUpper = (diagonalDir & SearchDir.Upper) == SearchDir.Upper;
        bool isRight = (diagonalDir & SearchDir.Right) == SearchDir.Right;
        int obstaclePointX = isHorizon ? x : (isRight ? x + 1 : x - 1);
        int obstaclePointY = isHorizon ? (isUpper ? y + 1 : y - 1) : y;

        neighborX = -1;
        neighborY = -1;

        if (IsOutOfRange(obstaclePointX, obstaclePointY, xSize, ySize))
            return false;

        PathNode obstacleCheckNode = pathNodes[GetIndex(obstaclePointX, obstaclePointY, xSize)];

        if (obstacleCheckNode.isObstacle == false)
            return false;

        int neighborPointX = isRight ? x + 1 : x - 1;
        int neighborPointY = isUpper ? y + 1 : y - 1;

        if (IsOutOfRange(neighborPointX, neighborPointY, xSize, ySize))
            return false;

        PathNode neighborNode = pathNodes[GetIndex(neighborPointX, neighborPointY, xSize)];

        if (neighborNode.isObstacle)
            return false;

        neighborX = neighborPointX;
        neighborY = neighborPointY;

        return true;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        xSize = JPSDotsDataLinker.Instance().xSize;
        ySize = JPSDotsDataLinker.Instance().ySize;
        startX = JPSDotsDataLinker.Instance().startX;
        startY = JPSDotsDataLinker.Instance().startY;
        endX = JPSDotsDataLinker.Instance().endX;
        endY = JPSDotsDataLinker.Instance().endY;

        SearchPath(ref state, new int2(startX, startY), new int2(endX, endY));
        state.Enabled = false;

        //JPSJob job = new JPSJob();
        //job.Schedule();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
