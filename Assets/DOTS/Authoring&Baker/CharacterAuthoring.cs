using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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
                var obj = MinionAnimationDB.Instance.GetSpawnObj(authoring.DefaultAnimation, i, true);
                if (obj != null)
                {
                    var spawnEntity = GetEntity(obj, TransformUsageFlags.Renderable);
                    //MinionAnimationDB.Instance.GetSpawnObj(authoring.DefaultAnimation, i);

                    MinionParts.Add(new MinionPart { Part = spawnEntity, SpawnBodyIndex = i });
                }
                //var spawnEntity = GetEntity(MinionAnimationDB.Instance.DefaultObject, TransformUsageFlags.Renderable);
            }


            AddComponent
                (
                    GetEntity(authoring, TransformUsageFlags.Renderable),
                    new MinionData()
                    {
                        AvaterName = targetAvatar.name,
                        isSpawnedPart = false,
                        isEnablePart = false,
                        Parts = targetAvatar.humanDescription.skeleton.Length,
                        DisableCounter = -1,
                        ImpactLocation = float3.zero
                    }
                );

            AddComponent
                (
                    GetEntity(authoring, TransformUsageFlags.Renderable),
                    new MinionAnimation
                    {
                        PreviousAnimation = -1,
                        StopedTime = 0,
                        CurrectAnimation = -1,
                        PlayTime = 0
                    }
                );
        }
    }
}
