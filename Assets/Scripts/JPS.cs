using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using static Algorithm.PathAlgorithmHelper;
using static UnityEngine.InputManagerEntry;

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

        protected override bool SearchPath(int fromX, int fromY, int toX, int toY)
        {
            _openNodes.Clear();
            _closedNodes.Clear();

            if (fromX == toX && fromY == toY)
                return false;

            Node firstNode = MakeNode(fromX, fromY, fromX, fromY, toX, toY);
            AddToOpenNode(firstNode);

            bool found = FindPathInner(firstNode, toX, toY);

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

        private void ConnectNodesBetween(Node node1, Node node2)
        {
            if (node1.x == node2.x)
                ConnectHorizontal(node1, node2);
            else if (node1.y == node2.y)
                ConnectVertical(node1, node2);
            else
                ConnectNodesDiagonal(node1, node2);
        }

        private void ConnectNodesDiagonal(Node node1, Node node2)
        {
            int size = Math.Abs(node1.x - node2.x) - 1;
            int addH = node1.x < node2.x ? 1 : -1;
            int addV = node1.y < node2.y ? 1 : -1;
            int count = 0;
            int x = node1.x;
            int y = node1.y;

            while (count < size)
            {
                count++;
                Node newNode = MakeNode(x + addH, y + addV, x, y, _endX, _endY);
                _pathes.Add(newNode);
                x += addH;
                y += addV;
            }
        }

        private void ConnectVertical(Node node1, Node node2)
        {
            int min = Math.Min(node1.x, node2.x);
            int size = Math.Abs(node1.x - node2.x) - 1;
            int count = 0;
            int parent = min;

            while (count < size)
            {
                count++;
                Node newNode = MakeNode(min + count, node1.y, parent, node1.y, _endX, _endY);
                _pathes.Add(newNode);
                parent = newNode.x;
            }
        }

        private void ConnectHorizontal(Node node1, Node node2)
        {
            int min = Math.Min(node1.y, node2.y);
            int size = Math.Abs(node1.y - node2.y) - 1;
            int count = 0;
            int parent = min;

            while (count < size)
            {
                count++;
                Node newNode = MakeNode(node1.x, min + count, node1.x, parent, _endX, _endY);
                _pathes.Add(newNode);
                parent = newNode.y;
            }
        }

        private bool FindPathInner(Node startNode, int toX, int toY)
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

