using Unity.Collections;
using Unity.Entities;
using UnityEngine;

class CharacterAuthoring : MonoBehaviour
{
    public int DefaultAnimation = -1;
}

class CharacterAuthoringBaker : Baker<CharacterAuthoring>
{
    public override void Bake(CharacterAuthoring authoring)
    {
        AddComponent
            (
                GetEntity(authoring, TransformUsageFlags.Dynamic),
                new MinionTag()
            );


        if (authoring.DefaultAnimation >= 0)
        {
            var targetAvatar = MinionAnimationDB.Instance.animationClips[authoring.DefaultAnimation].OriginAvatar;

            var MinionParts = AddBuffer<MinionPart>(GetEntity(authoring, TransformUsageFlags.None));
            MinionParts.Capacity = targetAvatar.humanDescription.skeleton.Length;

            for (int i = 0; i < targetAvatar.humanDescription.skeleton.Length; i++)
            {
                var spawnEntity = GetEntity(MinionAnimationDB.Instance.DefaultObject, TransformUsageFlags.Renderable);
                    //MinionAnimationDB.Instance.GetSpawnObj(authoring.DefaultAnimation, i);


                MinionParts.Add(new MinionPart { Part = spawnEntity });
            }


            AddComponent
                (
                    GetEntity(authoring, TransformUsageFlags.Renderable),
                    new MinionData()
                    {
                        AvaterName = targetAvatar.name,
                        isSpawnedPart = false,
                        TestDefaultObj = GetEntity(MinionAnimationDB.Instance.DefaultObject, TransformUsageFlags.Renderable),
                        Parts = targetAvatar.humanDescription.skeleton.Length
                    }
                );
        }
    }
}
