using UnityEditor;
using UnityEngine;

namespace Test
{
    [CustomEditor(typeof(TestMonoPathFinder))]
    public class TestMonoAstarInspector : Editor
    {
        private TestMonoPathFinder _instance;

        private void OnEnable()
        {
            _instance = (TestMonoPathFinder)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(10);

            if (Application.isPlaying)
                DrawPathMenu();
            else
                DrawGridMenu();
        }

        private void DrawGridMenu()
        {
            if (GUILayout.Button("MakeGrid"))
            {
                DeleteGrid();
                MakeGrid();
            }
        }

        private void DrawPathMenu()
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Search"))
                {
                    _instance.ResetPathState();
                    _instance.ResetDestGridColor();
                    _instance.SearchPath();
                    _instance.ShowPath();
                }

                if (GUILayout.Button("Clear Path"))
                {
                    _instance.ResetPathState();
                    _instance.ResetDestGridColor();
                }

                if (GUILayout.Button("Clear"))
                {
                    _instance.ResetGridState();
                    _instance.ResetDestGridColor();
                }
            }
            GUILayout.EndHorizontal();
        }

        public void DeleteGrid()
        {
            foreach (Transform grid in _instance.grids)
            {
                if (grid == null)
                    continue;

                DestroyImmediate(grid.gameObject);
            }
            _instance.grids.Clear();
        }

        public void MakeGrid()
        {
            for (int y = 0; y < _instance.yCount; y++)
            {
                for (int x = 0; x < _instance.xCount; x++)
                {
                    MakeGrid(x, y);
                }
            }
        }

        private void MakeGrid(int x, int y)
        {
            GameObject grid = GameObject.CreatePrimitive(PrimitiveType.Plane);
            grid.transform.parent = _instance.gridParent;
            grid.name = $"grid_{x}x{y}";

            float xOffset = _instance.gridWidth * 0.5f - _instance.xCount * _instance.gridWidth * 0.5f;
            float yOffset = _instance.gridHeight * 0.5f - _instance.yCount * _instance.gridHeight * 0.5f;

            Transform gridTransform = grid.transform;
            gridTransform.localScale = Vector3.one * 0.08f;
            gridTransform.localPosition = new Vector3(x * _instance.gridWidth + xOffset, 0.0f, y * _instance.gridHeight + yOffset);
            _instance.grids.Add(gridTransform);
        }
    }
}

