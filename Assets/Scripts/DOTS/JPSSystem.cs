using Algorithm;
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using static Algorithm.PathAlgorithmHelper;
using Unity.Rendering;

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

    #region connect nodes between
    private void ConnectNodesBetween(PathNode node1, PathNode node2, ref NativeArray<PathNode> pathNodes, ref NativeList<int> pathes)
    {
        if (node1.x == node2.x)
            ConnectHorizontal(node1, node2, ref pathNodes, ref pathes);
        else if (node1.y == node2.y)
            ConnectVertical(node1, node2, ref pathNodes, ref pathes);
        else
            ConnectNodesDiagonal(node1, node2, ref pathNodes, ref pathes);
    }

    private void ConnectNodesDiagonal(PathNode node1, PathNode node2, ref NativeArray<PathNode> pathNodes, ref NativeList<int> pathes)
    {
        int size = System.Math.Abs(node1.x - node2.x) - 1;
        int addH = node1.x < node2.x ? -1 : 1;
        int addV = node1.y < node2.y ? -1 : 1;
        int x = node2.x;
        int y = node2.y;
        int count = 0;

        while (count < size)
        {
            count++;

            PathNode newNode = pathNodes[GetIndex(x + addH, y + addV, xSize)];
            newNode.g = CalcG(x, y, newNode.x, newNode.y);
            newNode.beforeIndex = node1.index;
            newNode.UpdateF();
            pathNodes[newNode.index] = newNode;
            pathes.Add(newNode.index);

            x += addH;
            y += addV;
        }
    }

    private void ConnectVertical(PathNode node1, PathNode node2, ref NativeArray<PathNode> pathNodes, ref NativeList<int> pathes)
    {
        int pivot = node2.x;
        int size = System.Math.Abs(node1.x - node2.x) - 1;
        int add = node1.x < node2.x ? -1 : 1;
        int count = 0;

        while (count < size)
        {
            int beforeIndex = GetIndex(pivot, node1.y, xSize);

            PathNode newNode = pathNodes[GetIndex(pivot + add, node1.y, xSize)];
            newNode.g = CalcG(pivot, node1.y, newNode.x, newNode.y);
            newNode.beforeIndex = beforeIndex;
            newNode.UpdateF();
            pathNodes[newNode.index] = newNode;
            pathes.Add(newNode.index);

            pivot = newNode.x;
            count++;
        }
    }

    private void ConnectHorizontal(PathNode node1, PathNode node2, ref NativeArray<PathNode> pathNodes, ref NativeList<int> pathes)
    {
        int pivot = node2.y;
        int size = System.Math.Abs(node1.y - node2.y) - 1;
        int add = node1.y < node2.y ? -1 : 1;
        int count = 0;

        while (count < size)
        {
            int beforeIndex = GetIndex(node1.x, pivot, xSize);

            PathNode newNode = pathNodes[GetIndex(node1.x, pivot + add, xSize)];
            newNode.g = CalcG(node1.x, pivot, newNode.x, newNode.y);
            newNode.beforeIndex = beforeIndex;
            newNode.UpdateF();
            pathNodes[newNode.index] = newNode;
            pathes.Add(newNode.index);

            pivot = newNode.y;
            count++;
        }
    }
    #endregion

    private void FindPath(ref SystemState state, int2 startPosition, int2 endPosition)
    {
        if (startPosition.x == endPosition.x && startPosition.y == endPosition.y)
            return;

        int2 gridSize = new int2(xSize, ySize);
        NativeArray<PathNode> pathNodes = new NativeArray<PathNode>(xSize * ySize, Allocator.Temp);

        for (int x = 0; x < xSize; x++)
        {
            for (int y = 0; y < ySize; y++)
            {
                PathNode node = new PathNode();
                node.index = GetIndex(x, y, xSize);
                node.x = x;
                node.y = y;
                node.g = int.MaxValue;
                node.h = CalcH(x, y, endPosition.x, endPosition.y);
                node.isObstacle = false;
                node.beforeIndex = -1;
                node.UpdateF();

                pathNodes[node.index] = node;
            }
        }

        foreach (var (grid, color, entity) in SystemAPI.Query<GridComponent, RefRW<URPMaterialPropertyBaseColor>>().WithEntityAccess())
            color.ValueRW.Value = new float4(1, 1, 1, 1);

        NativeList<int> openNodes = new NativeList<int>(Allocator.Temp);
        NativeList<int> closedNodes = new NativeList<int>(Allocator.Temp);
        NativeList<int> tempPathes = new NativeList<int>(Allocator.Temp);
        NativeList<int> pathes = new NativeList<int>(Allocator.Temp);

        PathNode startNode = pathNodes[GetIndex(startPosition.x, startPosition.y, xSize)];
        startNode.g = 0;
        startNode.UpdateF();
        pathNodes[startNode.index] = startNode;

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

                found = Search(pivotIndex, endPosition.x, endPosition.y, ref pathNodes, ref openNodes, ref closedNodes, ref tempPathes);

                if (found)
                    break;
            }

            if (found)
                break;
        }

        if (found)
        {
            PathNode pathNode = pathNodes[GetIndex(endPosition.x, endPosition.y, xSize)];

            while (pathNode.x >= 0 && pathNode.y >= 0)
            {
                pathes.Add(pathNode.index);

                if (pathNode.x == startPosition.x && pathNode.y == startPosition.y)
                    break;

                if (pathNode.beforeIndex < 0)
                    break;

                PathNode connectNode = pathNodes[pathNode.beforeIndex];

                if (pathNode.x >= 0 && pathNode.y >= 0)
                    ConnectNodesBetween(connectNode, pathNode, ref pathNodes, ref pathes);

                pathNode = connectNode;
            }

            foreach (var (grid, color, entity) in SystemAPI.Query<GridComponent, RefRW<URPMaterialPropertyBaseColor>>().WithEntityAccess())
            {
                foreach (var path in pathes)
                {
                    if (grid.index == path)
                    {
                        color.ValueRW.Value = new float4(1, 0, 0, 1);
                        break;
                    }
                }
            }

            foreach (var path in pathes)
            {
                PathNode node = pathNodes[path];
                UnityEngine.Debug.Log(node.x + ", " + node.y);
            }
        }

        pathes.Dispose();
        tempPathes.Dispose();
        openNodes.Dispose();
        closedNodes.Dispose();
        pathNodes.Dispose();
    }

    private bool Search(int index, int toX, int toY, ref NativeArray<PathNode> pathNodes, ref NativeList<int> openNodes, ref NativeList<int> closedNodes, ref NativeList<int> pathes)
    {
        closedNodes.Add(index);
        pathes.Add(index);

        for (int d = 0; d < DiagonalArounds.Length; d++)
        {
            int nextX = pathNodes[index].x;
            int nextY = pathNodes[index].y;
            int beforeX = nextX;
            int beforeY = nextY;

            while (true)
            {
                if (IsOutOfRange(nextX, nextY, xSize, ySize))
                    break;

                PathNode neighborNode = pathNodes[GetIndex(nextX, nextY, xSize)];

                if (neighborNode.isObstacle)
                    break;

                if (nextX == toX && nextY == toY)
                {
                    pathes.Add(neighborNode.index);
                    return true;
                }

                SearchDir diagonalDir = DiagonalArounds[d].dir;
                CoordinatePair stepPair = GetDiagonalCoordPair(diagonalDir);

                neighborNode.g = CalcG(beforeX, beforeY, nextY, nextY);
                neighborNode.beforeIndex = index;
                neighborNode.UpdateF();
                pathNodes[neighborNode.index] = neighborNode;

                bool found = SearchByStep(neighborNode.index, toX, toY, nextX, nextY, stepPair.horizontal, diagonalDir, true, ref pathNodes, ref openNodes, ref closedNodes, ref pathes);

                if (found == false)
                    found = SearchByStep(neighborNode.index, toX, toY, nextX, nextY, stepPair.vertical, diagonalDir, false, ref pathNodes, ref openNodes, ref closedNodes, ref pathes);

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

    private bool SearchByStep(int parentIndex, int toX, int toY, int x, int y, Coordinate stepCoordinate, SearchDir diagonalDir, bool isHorizon, ref NativeArray<PathNode> pathNodes, ref NativeList<int> openNodes, ref NativeList<int> closedNodes, ref NativeList<int> pathes)
    {
        int nextX = x;
        int nextY = y;
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

            if (nextX == toX && nextY == toY)
            {
                pathes.Add(parentIndex);

                dest.beforeIndex = parentIndex;
                dest.g = CalcG(x, y, nextY, nextY);
                dest.UpdateF();
                pathNodes[dest.index] = dest;

                pathes.Add(dest.index);

                return true;
            }

            if (dest.isObstacle)
                return false;

            if (CheckForceNeighbor(nextX, nextY, out int neighborX, out int neighborY, diagonalDir, isHorizon, pathNodes))
            {
                PathNode currentNode = pathNodes[GetIndex(nextX, nextY, xSize)];
                currentNode.beforeIndex = parentIndex;
                currentNode.g = CalcG(x, y, nextX, nextY);
                currentNode.UpdateF();
                pathNodes[currentNode.index] = currentNode;

                PathNode neighbor = pathNodes[GetIndex(neighborX, neighborY, xSize)];
                neighbor.beforeIndex = currentNode.index;
                neighbor.g = CalcG(nextX, nextY, neighborX, neighborY);
                neighbor.UpdateF();
                pathNodes[neighbor.index] = neighbor;

                if (!closedNodes.Contains(neighbor.index) && !openNodes.Contains(neighbor.index))
                    openNodes.Add(neighbor.index);

                pathes.Add(parentIndex);
                pathes.Add(currentNode.index);
                pathes.Add(neighbor.index);
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

        FindPath(ref state, new int2(startX, startY), new int2(endX, endY));
        state.Enabled = false;

        //JPSJob job = new JPSJob();
        //job.Schedule();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
