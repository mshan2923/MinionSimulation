using Unity.Entities;
using UnityEngine;

class TestNavi : MonoBehaviour
{
    
}

class TestNaviBaker : Baker<TestNavi>
{
    public override void Bake(TestNavi authoring)
    {
        AddComponent(GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic),
            new TestNaviComponent());
    }
}
