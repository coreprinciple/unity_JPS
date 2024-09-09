using System.Collections.Generic;

namespace Algorithm
{
    public class AStar : PathFinderBase
    {
        protected override bool SearchPath(int fromX, int fromY, int toX, int toY)
        {
            _openNodes.Clear();
            _closedNodes.Clear();

            if (fromX == toX && fromY == toY)
                return false;

            Node firstNode = PathAlgorithmHelper.MakeNode(fromX, fromY, fromX, fromY, toX, toY);
            _openNodes.Add(firstNode);

            return FindPathInner(firstNode, toX, toY);
        }

        private bool FindPathInner(Node startNode, int toX, int toY)
        {
            while (_openNodes.Count > 0)
            {
                _pathes.Clear();

                Node pivot = new Node();
                pivot.f = int.MaxValue;

                for (int i = 0; i < _openNodes.Count; i++)
                {
                    if (_openNodes[i].f < pivot.f)
                        pivot = _openNodes[i];
                }

                if (AddAroundOpenNodes(pivot, toX, toY))
                {
                    Node found = GetNodeInOpen(toX, toY);
                    _pathes.Add(found);

                    while (true)
                    {
                        found = GetNodeInClosed(found.parentX, found.parentY);
                        _pathes.Add(found);

                        if (found.x == startNode.x && found.y == startNode.y)
                            break;
                    }
                    return true;
                }
            }
            return false;
        }

        private bool AddAroundOpenNodes(Node node, int toX, int toY)
        {
            AddToClosed(node);

            int beforeX = node.x;
            int beforeY = node.y;

            for (int c = 0; c < PathAlgorithmHelper.Arounds.Length; c++)
            {
                int x = beforeX + PathAlgorithmHelper.Arounds[c].x;
                int y = beforeY + PathAlgorithmHelper.Arounds[c].y;

                if (x < 0 || y < 0 || x >= xCount || y >= yCount || IsObstacle(x, y))
                    continue;

                if (ContainClosed(x, y))
                    continue;

                Node newNode = PathAlgorithmHelper.MakeNode(x, y, beforeX, beforeY, toX, toY);

                if (ContainOpen(newNode))
                {
                    for (int i = 0; i < _openNodes.Count; i++)
                    {
                        if (_openNodes[i].Equals(newNode) == false)
                            continue;

                        if (newNode.g < _openNodes[i].g)
                            _openNodes[i] = newNode;
                        break;
                    }
                }
                else
                    _openNodes.Add(newNode);

                if (x == toX && y == toY)
                    return true;
            }
            return false;
        }
    }
}

