using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(MinionSystem))]
public partial class MinionNavigationSystem : SystemBase
{
    // 우선 미니언들끼리 겹쳐지지 않고 한지점으로 이동 해야함
    // 장해물 판정은 나중에 하고

    #region AddComponentLater
    public float3 Target;
    public float Speed = 2.5f;
    public float RotationSpeed = 5f;
    public float cellRadius = 0.5f;
    #endregion

    private static readonly int[] cellOffsetTable =
    {
        1, 1, 1, 1, 1, 0, 1, 1, -1, 1, 0, 1, 1, 0, 0, 1, 0, -1, 1, -1, 1, 1, -1, 0, 1, -1, -1,
        0, 1, 1, 0, 1, 0, 0, 1, -1, 0, 0, 1, 0, 0, 0, 0, 0, -1, 0, -1, 1, 0, -1, 0, 0, -1, -1,
       -1, 1, 1, -1, 1, 0, -1, 1, -1, -1, 0, 1, -1, 0, 0, -1, 0, -1, -1, -1, 1, -1, -1, 0, -1, -1, -1
    };

    #region Job

    [BurstCompile]
    private struct HashPositions : IJobParallelFor
    {
        //#pragma warning disable 0649
        [ReadOnly] public float cellRadius;

        [ReadOnly] public NativeArray<LocalTransform> particleData;// 미니언 데이터 
        [ReadOnly] public NativeArray<MinionData> MinionDatas;

        public NativeParallelMultiHashMap<int, int>.ParallelWriter hashMap;
        //#pragma warning restore 0649

        public void Execute(int index)
        {
            if (MinionDatas[index].IsActive)
            {
                float3 position = particleData[index].Position;

                int hash = GridHash.Hash(position, cellRadius);
                hashMap.Add(hash, index);
            }
        }
    }//미니언위치를 해쉬화 해서 NativeParallelMultiHashMap 의 키값으로 삽입 , value 값은 QueryIndex

    [BurstCompile]
    private struct ComputePressure : IJobParallelFor
    {
        [ReadOnly] public NativeParallelMultiHashMap<int, int> hashMap;
        [ReadOnly] public NativeArray<int> cellOffsetTable;

        [ReadOnly] public NativeArray<LocalTransform> particleData;// 미니언 데이터
        [ReadOnly] public NativeArray<MinionData> MinionDatas;

        [ReadOnly] public float cellRadius;
        [ReadOnly] public float minionRadius;
        public float DT;

        public NativeArray<float3> pressureDir;
        //public NativeArray<float3> pressureVel;

        public void Execute(int index)
        {
            if (MinionDatas[index].IsActive == false)
                return;

            var position = particleData[index].Position;//각각의 미니언 위치
            int i, hash, j;
            int3 gridOffset;
            int3 gridPosition = GridHash.Quantize(position, cellRadius);
            bool found;

            int collisionCount = 0;
            //pressureVel[index] = float3.zero;

            // Find neighbors
            for (int oi = 0; oi < 27; oi++)
            {
                i = oi * 3;
                gridOffset = new int3(cellOffsetTable[i], cellOffsetTable[i + 1], cellOffsetTable[i + 2]);
                hash = GridHash.Hash(gridPosition + gridOffset);
                NativeParallelMultiHashMapIterator<int> iterator;
                found = hashMap.TryGetFirstValue(hash, out j, out iterator);

                while (found)
                {
                    collisionCount++;

                    // Neighbor found, get density
                    if (index != j)
                    {
                        var rij = position - particleData[j].Position;
                        float r2 = math.lengthsq(rij);

                        if (r2 < 4 * minionRadius * minionRadius && MinionDatas[j].IsActive)
                        {
                            pressureDir[index] += rij;
                            //pressureVel[index] += particleData[j].velocity;
                        }//미니언들끼리의 거리
                    }

                    found = hashMap.TryGetNextValue(out j, ref iterator);
                }
            }
        }
    }

    [BurstCompile]
    private struct ComputeCollision : IJobParallelFor
    {
        [ReadOnly] public NativeArray<LocalTransform> particleData;// 미니언 데이터 
        [ReadOnly] public NativeArray<MinionData> MinionDatas;
        [ReadOnly] public NativeArray<float3> pressureDir;

        [WriteOnly] public NativeArray<LocalTransform> pressureVel;// 변경 데이터

        public float3 Target;
        public float MinionRadius;
        public float Speed;
        public float RotationSpeed;

        public float DT;

        public void Execute(int index)
        {
            if (MinionDatas[index].IsActive == false)
                return;

            var temp = particleData[index];

            if (Vector3.SqrMagnitude(Target - temp.Position) < 0.01f)// Mathf.Approximately 는 제대로 작동X
            {
                pressureVel[index] = temp;
                return;
            }

            //속력은 항상 플레이어 쪽으로 향하므로 
            // 일정거리안은 약하게 서로 밀다가 , 겹쳐지기 전이면 이동 대신 거리두기 해야 할듯

            if (Vector3.SqrMagnitude(pressureDir[index]) < 0.01f)
            {
                var lookTarget = Quaternion.LookRotation(math.normalize(Target - temp.Position));
                temp.Position += math.normalize(Target - temp.Position) * Speed * DT;
                temp.Rotation = Quaternion.Lerp(temp.Rotation, lookTarget, DT * RotationSpeed);

            }
            else
            {
                //다른 미니언 존재 (막히지 않을때 포함)

                if (false)//(Vector3.SqrMagnitude(pressureDir[index]) > MinionRadius * MinionRadius)
                {
                    var lerpNormal = math.lerp(math.normalize(Target - temp.Position), math.normalize(pressureDir[index]),
                        Mathf.Clamp01(Vector3.Magnitude(pressureDir[index]) / (MinionRadius)));
                    temp.Position += lerpNormal * Speed * DT;
                    temp.Rotation = Quaternion.Lerp(temp.Rotation, Quaternion.LookRotation(math.normalize(pressureDir[index])), DT * RotationSpeed);
                }
                else
                {
                    temp.Position += math.normalize(pressureDir[index]) * Speed * DT;
                    temp.Rotation = Quaternion.Lerp(temp.Rotation, Quaternion.LookRotation(math.normalize(pressureDir[index])), DT * RotationSpeed);
                }
            }

            //Debug.Log($"Dir : {pressureDir[index]} , Target : {Target} \n pre Trans : {particleData[index]} , Added Trans : {temp}");

            pressureVel[index] = temp;
        }
    }

    [BurstCompile]
    private partial struct ApplyPosition : IJobEntity
    {
        //원래의 HashedFluidSimulation 에선 FluidSimlationComponent 에서 위치랑 속력 , 가속도를 관리하고 있어
        // 먼저 속력을 계산해 참조로 FluidSimlationComponent 값 변경
        // 그리고 변경된 위치값을 다음 Job 에서 적용함

        //[ReadOnly] public NativeArray<LocalTransform> particleData;
        [ReadOnly] public NativeArray<LocalTransform> pressureVel;

        [ReadOnly] public NativeArray<MinionData> MinionDatas;

        public void Execute([EntityIndexInQuery] int index, ref LocalTransform transform)
        {
            if (MinionDatas[index].IsActive == false)
                return;

            transform = pressureVel[index];
        }
    }
    #endregion

    private EntityQuery MinionGroup;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        MinionGroup = GetEntityQuery(typeof(MinionData), typeof(LocalTransform));//TestNaviComponent

        if (MinionGroup.CalculateEntityCount() <= 0)
        {
            Enabled = false;
            Debug.LogWarning("Can't find 'MinionData'");
        }    
    }

    protected override void OnUpdate()
    {
        //--시발 또 너냐?

        #region 초기화

        var minionTransform = MinionGroup.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var minionDatas = MinionGroup.ToComponentDataArray<MinionData>(Allocator.TempJob);
        int minionAmount = minionTransform.Length;

        var minionAddTransform = new NativeArray<LocalTransform>(minionAmount, Allocator.TempJob);
        var hashMap = new NativeParallelMultiHashMap<int, int>(minionAmount, Allocator.TempJob);

        var minionDir = new NativeArray<float3>(minionAmount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var cellOffsetTableNative = new NativeArray<int>(cellOffsetTable, Allocator.TempJob);
        #endregion

        if (math.isfinite(minionTransform[0].Position).x == false)
            Debug.LogError($"{minionTransform[0].Position} : {minionDatas[0].isEnablePart}");

        #region 설정

        var minionDirJob = new MemsetNativeArray<float3> { Source = minionDir , Value = Vector3.zero};
        JobHandle minionDirHandle = minionDirJob.Schedule(minionAmount, 64);

        var hashPositionsJob = new HashPositions
        {
            cellRadius = cellRadius,
            particleData = minionTransform,
            MinionDatas = minionDatas,
            hashMap = hashMap.AsParallelWriter()
        };
        var hashPositionsHandle = hashPositionsJob.Schedule(minionAmount, 64, minionDirHandle);
        #endregion

        #region Calculate

        var computePressureJob = new ComputePressure
        {
            hashMap = hashMap,
            cellOffsetTable = cellOffsetTableNative,
            particleData = minionTransform,
            MinionDatas = minionDatas,
            pressureDir = minionDir,
            
            DT = SystemAPI.Time.DeltaTime,
            cellRadius = cellRadius,
            minionRadius = cellRadius
        };
        var computePressureHandle = computePressureJob.Schedule(minionAmount, 64, hashPositionsHandle);

        var computeCollisionJob = new ComputeCollision
        {
            particleData = minionTransform,
            pressureVel = minionAddTransform,
            MinionDatas = minionDatas,
            pressureDir = minionDir,

            Target = Target,
            MinionRadius = cellRadius,
            Speed = Speed,
            RotationSpeed = RotationSpeed,
            DT = SystemAPI.Time.DeltaTime,
        };
        var computeCollisionHandle = computeCollisionJob.Schedule(minionAmount, 64, computePressureHandle);
        #endregion

        var applyPositionHandle = new ApplyPosition
        {
            //particleData = minionTransform,
            pressureVel = minionAddTransform,
            MinionDatas = minionDatas,
        }.ScheduleParallel(MinionGroup, computeCollisionHandle);
        applyPositionHandle.Complete();

        #region Dispose
        minionTransform.Dispose();
        minionAddTransform.Dispose();
        minionDatas.Dispose();
        hashMap.Dispose();
        minionDir.Dispose();
        cellOffsetTableNative.Dispose();
        #endregion
    }
}
