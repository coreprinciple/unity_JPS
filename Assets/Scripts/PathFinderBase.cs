using System.Collections.Generic;

namespace Algorithm
{
    public abstract class PathFinderBase
    {
        public int xCount { get; private set; }
        public int yCount { get; private set; }

        public IEnumerable<Node> pathes => _pathes;

        protected readonly List<Node> _closedNodes = new List<Node>(100);
        protected readonly List<Node> _openNodes = new List<Node>(100);
        protected readonly List<Node> _pathes = new List<Node>(100);

        protected bool[,] _obstacles;
        protected int _startX, _startY;
        protected int _endX, _endY;

        protected bool ContainOpen(Node node) => _openNodes.Contains(node);

        public bool IsObstacle(int x, int y) => _obstacles[x, y];

        public void Init(int xCount, int yCount)
        {
            this.xCount = xCount;
            this.yCount = yCount;
            _obstacles = new bool[xCount, yCount];
        }

        public void Set(int startX, int startY, int endX, int endY)
        {
            _startX = startX;
            _startY = startY;
            _endX = endX;
            _endY = endY;
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

        public void RemoveObstacle(int x, int y)
        {
            _obstacles[x, y] = false;
        }

        public bool AddObstacle(int x, int y)
        {
            if (IsObstacle(x, y))
                return false;

            if (x == _startX && y == _startY)
                return false;

            if (x == _endX && y == _endY)
                return false;

            _obstacles[x, y] = true;
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

        public void ClearObstacle()
        {
            for (int x = 0; x < _obstacles.GetLength(0); x++) {
                for (int y = 0; y < _obstacles.GetLength(1); y++) {
                    _obstacles[x, y] = false;
                }
            }
        }

        public virtual void ClearPath()
        {
            _pathes.Clear();
        }
    }
}

