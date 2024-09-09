using System;
using System.Collections.Generic;

namespace Algorithm
{
    public abstract class PathFinderBase
    {
        public int xCount { get; private set; }
        public int yCount { get; private set; }

        public ICollection<Coordinate> obstacles => _obstacles;
        public IEnumerable<Node> pathes => _pathes;

        protected readonly HashSet<Coordinate> _obstacles = new HashSet<Coordinate>(100);
        protected readonly List<Node> _closedNodes = new List<Node>(100);
        protected readonly List<Node> _openNodes = new List<Node>(100);
        protected readonly List<Node> _pathes = new List<Node>(100);

        protected int _startX, _startY;
        protected int _endX, _endY;

        protected bool IsObstacle(int x, int y) => _obstacles.Contains(new Coordinate(x, y));
        protected bool ContainOpen(Node node) => _openNodes.Contains(node);

        public void Init(int xCount, int yCount)
        {
            this.xCount = xCount;
            this.yCount = yCount;
        }

        public void Set(int startX, int startY, int endX, int endY)
        {
            this._startX = startX;
            this._startY = startY;
            this._endX = endX;
            this._endY = endY;
        }

        protected void AddToClosed(Node node)
        {
            _openNodes.Remove(node);

            if (_closedNodes.Contains(node))
                return;

            _closedNodes.Add(node);
        }

        protected bool ContainClosed(int x, int y)
        {
            for (int i = 0; i < _closedNodes.Count; i++)
            {
                if (_closedNodes[i].x == x && _closedNodes[i].y == y)
                    return true;
            }
            return false;
        }

        public bool AddObstacle(Coordinate coordinate)
        {
            if (_obstacles.Contains(coordinate))
                return false;

            if (coordinate.x == _startX && coordinate.y == _startY)
                return false;

            if (coordinate.x == _endX && coordinate.y == _endY)
                return false;

            _obstacles.Add(coordinate);
            return true;
        }

        protected Node GetNodeInClosed(int x, int y)
        {
            for (int i = 0; i < _closedNodes.Count; i++)
            {
                if (_closedNodes[i].x == x && _closedNodes[i].y == y)
                    return _closedNodes[i];
            }
            return default;
        }

        protected Node GetNodeInOpen(int x, int y)
        {
            for (int i = 0; i < _openNodes.Count; i++)
            {
                if (_openNodes[i].x == x && _openNodes[i].y == y)
                    return _openNodes[i];
            }
            return default;
        }

        public bool SearchPath() => SearchPath(_startX, _startY, _endX, _endY);

        protected abstract bool SearchPath(int fromX, int fromY, int toX, int toY);

        public virtual void ClearPath()
        {
            _pathes.Clear();
        }
    }
}

