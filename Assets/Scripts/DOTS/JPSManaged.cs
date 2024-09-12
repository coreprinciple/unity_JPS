using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

public class JPSManaged : MonoBehaviour
{
    private static JPSManaged _instance;

    [HideInInspector] public bool searchOrder;

    public static JPSManaged Instance()
    {
        if (_instance == null)
            _instance = FindAnyObjectByType<JPSManaged>();
        return _instance;
    }

    void Awake()
    {
        _instance = this;

        JPSDotsDataLinker.Instance().gridWidth = 1.0f;
        JPSDotsDataLinker.Instance().gridHeight = 1.0f;
        JPSDotsDataLinker.Instance().startX = 3;
        JPSDotsDataLinker.Instance().startY = 28;
        JPSDotsDataLinker.Instance().endX = 23;
        JPSDotsDataLinker.Instance().endY = 14;

        JPSDotsDataLinker.Instance().obstacles.Add(new int2(20, 18));
        JPSDotsDataLinker.Instance().obstacles.Add(new int2(20, 17));
        JPSDotsDataLinker.Instance().obstacles.Add(new int2(20, 16));
        JPSDotsDataLinker.Instance().obstacles.Add(new int2(20, 15));
        JPSDotsDataLinker.Instance().obstacles.Add(new int2(20, 14));
        JPSDotsDataLinker.Instance().obstacles.Add(new int2(20, 13));
        JPSDotsDataLinker.Instance().obstacles.Add(new int2(20, 12));
        JPSDotsDataLinker.Instance().obstacles.Add(new int2(20, 11));
        JPSDotsDataLinker.Instance().obstacles.Add(new int2(20, 10));
        JPSDotsDataLinker.Instance().obstacles.Add(new int2(20, 9));

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        Entity tagEntity = ecb.CreateEntity();
        ecb.AddComponent<JPSTagComponent>(tagEntity);
        ecb.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
        ecb.Dispose();
    }

    public void OnClickSearch()
    {
        searchOrder = true;
    }
}
