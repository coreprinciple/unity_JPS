using System;
using System.Collections.Generic;
using static Algorithm.PathAlgorithmHelper;

namespace Algorithm
{
    public class JPS : PathFinderBase
    {
        private readonly List<Node> _tempPathes = new List<Node>(100);

        private bool UnavailableNode(int x, int y)
        {
            return IsOutOfRange(x, y, xCount, yCount) || IsObstacle(x, y);
        }

        private bool CheckForceNeighbor(int x, int y, out int neighborX, out int neighborY, SearchDir diagonalDir, bool isHorizon)
        {
            bool isUpper = (diagonalDir & SearchDir.Upper) == SearchDir.Upper;
            bool isRight = (diagonalDir & SearchDir.Right) == SearchDir.Right;
            int obstaclePointX = isHorizon ? x : (isRight ? x + 1 : x - 1);
            int obstaclePointY = isHorizon ? (isUpper ? y + 1 : y - 1) : y;

            neighborX = -1;
            neighborY = -1;

            if (IsOutOfRange(obstaclePointX, obstaclePointY, xCount, yCount))
                return false;

            if (IsObstacle(obstaclePointX, obstaclePointY) == false)
                return false;

            int neighborPointX = isRight ? x + 1 : x - 1;
            int neighborPointY = isUpper ? y + 1 : y - 1;

            if (IsOutOfRange(neighborPointX, neighborPointY, xCount, yCount))
                return false;

            if (IsObstacle(neighborPointX, neighborPointY))
                return false;

            neighborX = neighborPointX;
            neighborY = neighborPointY;

            return true;
        }

        private Node GetPathNode(int x, int y)
        {
            foreach (var path in _tempPathes)
            {
                if (path.x == x && path.y == y)
                    return path;
            }
            Node node = new Node();
            node.x = -1;
            node.y = -1;
            return node;
        }

        private void ConnectNodesBetween(Node current, Node before)
        {
            if (current.x == before.x)
                ConnectHorizontal(current, before);
            else if (current.y == before.y)
                ConnectVertical(current, before);
            else
                ConnectNodesDiagonal(current, before);
        }

        private void ConnectNodesDiagonal(Node current, Node before)
        {
            int size = Math.Abs(current.x - before.x) - 1;
            int addH = current.x < before.x ? 1 : -1;
            int addV = current.y < before.y ? 1 : -1;
            int x = current.x;
            int y = current.y;
            int count = 0;

            while (count < size)
            {
                count++;
                Node newNode = MakeNode(x + addH, y + addV, x, y, _endX, _endY);
                _pathes.Add(newNode);
                x += addH;
                y += addV;
            }
        }

        private void ConnectVertical(Node current, Node before)
        {
            int pivot = current.x;
            int size = Math.Abs(current.x - before.x) - 1;
            int add = current.x < before.x ? 1 : -1;
            int count = 0;

            while (count < size)
            {
                Node newNode = MakeNode(pivot + add, current.y, pivot, current.y, _endX, _endY);
                _pathes.Add(newNode);
                pivot = newNode.x;
                count++;
            }
        }

        private void ConnectHorizontal(Node current, Node before)
        {
            int pivot = current.y;
            int size = Math.Abs(current.y - before.y) - 1;
            int add = current.y < before.y ? 1 : -1;
            int count = 0;

            while (count < size)
            {
                Node newNode = MakeNode(current.x, pivot + add, current.x, pivot, _endX, _endY);
                _pathes.Add(newNode);
                pivot = newNode.y;
                count++;
            }
        }

        protected override bool SearchPath(int fromX, int fromY, int toX, int toY)
        {
            _openNodes.Clear();
            _closedNodes.Clear();

            if (fromX == toX && fromY == toY)
                return false;

            Node firstNode = MakeNode(fromX, fromY, fromX, fromY, toX, toY);
            AddToOpenNode(firstNode);

            bool found = SearchPathInner(firstNode, toX, toY);

            if (found == false)
                return false;

            Node pathNode = GetPathNode(toX, toY);
            
            while (pathNode.x >= 0 && pathNode.y >= 0)
            {
                _pathes.Add(pathNode);

                if (pathNode.x == fromX && pathNode.y == fromY)
                    break;

                pathNode = GetPathNode(pathNode.parentX, pathNode.parentY);

                if (pathNode.x >= 0 && pathNode.y >= 0)
                    ConnectNodesBetween(_pathes[_pathes.Count - 1], pathNode);
            }

            foreach (var path in _pathes)
                UnityEngine.Debug.Log(path.x + ", " + path.y + " :: " + path.parentX + ", " + path.parentY);

            return true;
        }

        private bool SearchPathInner(Node startNode, int toX, int toY)
        {
            while (_openNodes.Count > 0)
            {
                Node pivot = new Node();
                pivot.f = int.MaxValue;

                for (int i = 0; i < _openNodes.Count; i++)
                {
                    if (_openNodes[i].f < pivot.f)
                        pivot = _openNodes[i];
                }
                bool found = Search(pivot, toX, toY);

                if (found)
                    return true;
            }
            return false;
        }

        private bool Search(Node node, int toX, int toY)
        {
            AddToClosed(node);

            _tempPathes.Add(node);

            for (int d = 0; d < DiagonalArounds.Length; d++)
            {
                int nextX = node.x;
                int nextY = node.y;
                int beforeX = nextX;
                int beforeY = nextY;

                while (true)
                {
                    if (nextX == toX && nextY == toY)
                        return true;

                    if (UnavailableNode(nextX, nextY))
                        break;

                    SearchDir diagonalDir = DiagonalArounds[d].dir;
                    CoordinatePair stepPair = SearchSteps[diagonalDir];
                    Node neighbor = MakeNode(nextX, nextY, node.x, node.y, toX, toY);

                    bool found = SearchByStep(neighbor, toX, toY, nextX, nextY, stepPair.horizontal, diagonalDir, true);

                    if (found == false)
                        found = SearchByStep(neighbor, toX, toY, nextX, nextY, stepPair.vertical, diagonalDir, false);

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

        private bool SearchByStep(Node parentNode, int toX, int toY, int x, int y, Coordinate stepCoordinate, SearchDir diagonalDir, bool isHorizon)
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

                if (nextX == toX && nextY == toY)
                {
                    _tempPathes.Add(parentNode);

                    Node dest = MakeNode(nextX, nextY, parentNode.x, parentNode.y, toX, toY);
                    _tempPathes.Add(dest);

                    return true;
                }

                if (UnavailableNode(nextX, nextY))
                    return false;

                if (CheckForceNeighbor(nextX, nextY, out int neighborX, out int neighborY, diagonalDir, isHorizon))
                {
                    Node currentNode = MakeNode(nextX, nextY, parentNode.x, parentNode.y, toX, toY);
                    Node neighbor = MakeNode(neighborX, neighborY, nextX, nextY, toX, toY);

                    AddToOpenNode(neighbor);

                    _tempPathes.Add(parentNode);
                    _tempPathes.Add(currentNode);
                    _tempPathes.Add(neighbor);
                }
            }
        }

        private void AddToOpenNode(Node neighbor)
        {
            if (_closedNodes.Contains(neighbor))
                return;

            if (_openNodes.Contains(neighbor) == false)
                _openNodes.Add(neighbor);
        }

        public override void ClearPath()
        {
            _tempPathes.Clear();
            base.ClearPath();
        }
    }
}

