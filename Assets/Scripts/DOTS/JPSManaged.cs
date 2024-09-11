using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class JPSManaged : MonoBehaviour
{
    void Awake()
    {
        JPSDotsDataLinker.Instance().gridWidth = 1.0f;
        JPSDotsDataLinker.Instance().gridHeight = 1.0f;
        JPSDotsDataLinker.Instance().startX = 3;
        JPSDotsDataLinker.Instance().startY = 28;
        JPSDotsDataLinker.Instance().endX = 23;
        JPSDotsDataLinker.Instance().endY = 14;

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        Entity tagEntity = ecb.CreateEntity();
        ecb.AddComponent<JPSTagComponent>(tagEntity);
        ecb.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
        ecb.Dispose();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
