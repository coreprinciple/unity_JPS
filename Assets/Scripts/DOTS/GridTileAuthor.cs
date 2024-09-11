using UnityEngine;
using Unity.Entities;
using Unity.Rendering;

public class GridTileAuthor : MonoBehaviour
{
    [SerializeField] private GameObject _gridTilePrefab;

    private class GridTileBake : Baker<GridTileAuthor>
    {
        public override void Bake(GridTileAuthor authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new GridPrefabComponent { 
                prefab = GetEntity(authoring._gridTilePrefab, TransformUsageFlags.Dynamic) 
            });
        }
    }
}
