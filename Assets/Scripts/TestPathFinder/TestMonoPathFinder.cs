using Algorithm;
using UnityEngine;
using System.Collections.Generic;

namespace Test
{
    public class TestMonoPathFinder : MonoBehaviour
    {
        public Transform gridParent;

        public float gridWidth = 1.0f;
        public float gridHeight = 1.0f;

        public int xCount = 30;
        public int yCount = 30;

        public int startX, startY;
        public int endX, endY;

        public List<Transform> grids => _grids;

        [HideInInspector]
        [SerializeField] private List<Transform> _grids;

        [SerializeField] private Material _normalMat;
        [SerializeField] private Material _obstacleMat;
        [SerializeField] private Material _pathMat;
        [SerializeField] private Material _destMat;

        private PathFinderBase _pathFinder;

        private Transform GetGrid(int x, int y) => _grids[y * _pathFinder.xCount + x];

        private void Start()
        {
            //_pathFinder = new AStar();
            _pathFinder = new JPS();
            _pathFinder.Init(xCount, yCount);

            ResetGridState();
        }

        public void ResetGridState()
        {
            _pathFinder.obstacles.Clear();
            _pathFinder.ClearPath();

            foreach (Transform grid in _grids)
                grid.gameObject.GetComponent<Renderer>().sharedMaterial = _normalMat;
        }

        public void ResetPathState()
        {
            foreach (var path in _pathFinder.pathes)
                SetGridColor(path.x, path.y, _normalMat);
            _pathFinder.ClearPath();
        }

        public void ResetDestGridColor()
        {
            GetGridRenderer(endX, endY).sharedMaterial = _destMat;
        }

        public void SearchPath()
        {
            _pathFinder.Set(startX, startY, endX, endY);
            _pathFinder.SearchPath();
        }

        private void SetGridColor(int x, int y, Material material)
        {
            GetGridRenderer(x, y).sharedMaterial = material;
        }

        private Renderer GetGridRenderer(int x, int y)
        {
            return GetGrid(x, y).GetComponent<Renderer>();
        }

        public void ShowPath()
        {
            foreach (var grid in _pathFinder.pathes)
            {
                if (grid.x == endX && grid.y == endY)
                    continue;

                SetGridColor(grid.x, grid.y, _pathMat);
            }
        }

        private void PickObstacleGrid()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (!Physics.Raycast(ray, out RaycastHit hitInfo, float.MaxValue))
                return;

            string name = hitInfo.transform.name;

            if (!name.StartsWith("grid_"))
                return;

            float xOffset = xCount * gridWidth * 0.5f;
            float yOffset = yCount * gridHeight * 0.5f;

            Vector3 point = hitInfo.point + new Vector3(xOffset, 0.0f, yOffset);

            int x = Mathf.FloorToInt(point.x / gridWidth);
            int y = Mathf.FloorToInt(point.z / gridHeight);

            Coordinate coordinate = new Coordinate(x, y);
            
            if (_pathFinder.obstacles.Contains(coordinate))
            {
                _pathFinder.obstacles.Remove(coordinate);
                SetGridColor(x, y, _normalMat);
            }
            else if (_pathFinder.AddObstacle(coordinate))
                SetGridColor(x, y, _obstacleMat);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                SearchPath();

            if (Input.GetMouseButtonDown(0))
                PickObstacleGrid();
        }
    }
}

