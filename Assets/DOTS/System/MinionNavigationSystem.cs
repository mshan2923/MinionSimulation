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
    // �켱 �̴Ͼ�鳢�� �������� �ʰ� ���������� �̵� �ؾ���
    // ���ع� ������ ���߿� �ϰ�
    /*
    #region AddComponentLater
    public float3 Target;
    public float MoveSpeed = 1.4f;
    public float PressureSpeed = 1.0f;
    public float RotationSpeed = 5f;
    public float cellRadius = 0.5f;
    #endregion
    */

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

        [ReadOnly] public NativeArray<LocalTransform> particleData;// �̴Ͼ� ������ 
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
    }//�̴Ͼ���ġ�� �ؽ�ȭ �ؼ� NativeParallelMultiHashMap �� Ű������ ���� , value ���� QueryIndex

    [BurstCompile]
    private struct ComputePressure : IJobParallelFor
    {
        [ReadOnly] public NativeParallelMultiHashMap<int, int> hashMap;
        [ReadOnly] public NativeArray<int> cellOffsetTable;

        [ReadOnly] public NativeArray<LocalTransform> particleData;// �̴Ͼ� ������
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

            var position = particleData[index].Position;//������ �̴Ͼ� ��ġ
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
                        }//�̴Ͼ�鳢���� �Ÿ�
                    }

                    found = hashMap.TryGetNextValue(out j, ref iterator);
                }
            }
        }
    }

    [BurstCompile]
    private struct ComputeCollision : IJobParallelFor
    {
        [ReadOnly] public NativeArray<LocalTransform> particleData;// �̴Ͼ� ������ 
        [ReadOnly] public NativeArray<MinionData> MinionDatas;
        [ReadOnly] public NativeArray<float3> pressureDir;

        [WriteOnly] public NativeArray<LocalTransform> pressureVel;// ���� ������

        public float3 Target;
        public float MinionRadius;
        public float MoveSpeed;
        public float PressureSpeed;
        public float RotationSpeed;

        public float DT;

        public void Execute(int index)
        {
            if (MinionDatas[index].IsActive == false)
                return;

            var temp = particleData[index];

            if (Vector3.SqrMagnitude(Target - temp.Position) < 0.01f)
                // Mathf.Approximately �� ����� �۵�X
            {
                pressureVel[index] = temp;
                return;
            }

            //�ӷ��� �׻� �÷��̾� ������ ���ϹǷ� 
            // �����Ÿ����� ���ϰ� ���� �дٰ� , �������� ���̸� �̵� ��� �Ÿ��α� �ؾ� �ҵ�

            if (Vector3.SqrMagnitude(pressureDir[index]) < 0.0001f)
            {
                var lookTarget = Quaternion.LookRotation(math.normalize(Target - temp.Position));
                temp.Position += math.normalize(Target - temp.Position) * MoveSpeed * DT;
                temp.Rotation = Quaternion.Lerp(temp.Rotation, lookTarget, DT * RotationSpeed);

            }else if (Vector3.SqrMagnitude(pressureDir[index]) >= (MinionRadius * MinionRadius))
            {
                temp.Position += math.normalize(pressureDir[index]) * PressureSpeed * DT;
                temp.Rotation = Quaternion.Lerp(temp.Rotation, Quaternion.LookRotation(math.normalize(pressureDir[index])), DT * RotationSpeed);
            }//�ٸ� �̴Ͼ� ���� (������ ������ ����)

            //Debug.Log($"Dir : {pressureDir[index]} , Target : {Target} \n pre Trans : {particleData[index]} , Added Trans : {temp}");

            pressureVel[index] = temp;
        }
    }

    [BurstCompile]
    private partial struct ApplyNaviData : IJobEntity
    {
        [ReadOnly] public NativeArray<MinionNaviData> NaviDatas;
        [ReadOnly] public NativeArray<float3> pressureDir;

        public float MinionRadius;

        public void Execute([EntityIndexInQuery] int index, in LocalTransform transform,  ref MinionNaviData naviData)
        {
            var navi = NaviDatas[index];
            navi.PreviousPosition = transform.Position;

            if (Vector3.SqrMagnitude(pressureDir[index]) < 0.0001f)
            {
                navi.isStoped = false;
            }// �̵�
            else if (Vector3.SqrMagnitude(pressureDir[index]) > (4 * MinionRadius * MinionRadius))
            {
                navi.isStoped = true;
            }else
            {
                navi.isStoped = true;
            }

            naviData = navi;
        }
    }
    [BurstCompile]
    private partial struct ApplyPosition : IJobEntity
    {
        //������ HashedFluidSimulation ���� FluidSimlationComponent ���� ��ġ�� �ӷ� , ���ӵ��� �����ϰ� �־�
        // ���� �ӷ��� ����� ������ FluidSimlationComponent �� ����
        // �׸��� ����� ��ġ���� ���� Job ���� ������

        [ReadOnly] public NativeArray<LocalTransform> particleData;
        [ReadOnly] public NativeArray<LocalTransform> pressureVel;

        [ReadOnly] public NativeArray<MinionData> MinionDatas;

        public void Execute([EntityIndexInQuery] int index, ref LocalTransform transform, ref MinionNaviData naviData)
        {
            if (MinionDatas[index].IsActive == false)
                return;

            naviData.PreviousPosition = particleData[index].Position;
            transform = pressureVel[index];
        }
    }
    #endregion

    private EntityQuery MinionGroup;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        MinionGroup = GetEntityQuery(typeof(MinionData), typeof(MinionNaviData), typeof(LocalTransform));//TestNaviComponent

        if (MinionGroup.CalculateEntityCount() <= 0)
        {
            Enabled = false;
            Debug.LogWarning("Can't find 'MinionData'");
        }    

        Entities
            .WithAll<MinionNaviData>()
            .ForEach((int entityInQueryIndex, ref MinionNaviData naviData, in LocalTransform transform) => 
            {
                naviData.PreviousPosition = transform.Position;
            }).ScheduleParallel(Dependency).Complete();
    }

    protected override void OnUpdate()
    {

        #region �ʱ�ȭ

        var minionTransform = MinionGroup.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var minionDatas = MinionGroup.ToComponentDataArray<MinionData>(Allocator.TempJob);
        var minionNavi = MinionGroup.ToComponentDataArray<MinionNaviData>(Allocator.TempJob);
        int minionAmount = minionTransform.Length;

        var minionAddTransform = new NativeArray<LocalTransform>(minionAmount, Allocator.TempJob);
        var hashMap = new NativeParallelMultiHashMap<int, int>(minionAmount, Allocator.TempJob);

        var minionDir = new NativeArray<float3>(minionAmount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
        var cellOffsetTableNative = new NativeArray<int>(cellOffsetTable, Allocator.TempJob);
        #endregion

        #region ����

        var animControllData = SystemAPI.GetSingleton<MinionAnimatorControllData>();

        var minionDirJob = new MemsetNativeArray<float3> { Source = minionDir , Value = Vector3.zero};
        JobHandle minionDirHandle = minionDirJob.Schedule(minionAmount, 64);

        var hashPositionsJob = new HashPositions
        {
            cellRadius = animControllData.cellRadius,
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
            cellRadius = animControllData.cellRadius,
            minionRadius = animControllData.MinionRadius
        };
        var computePressureHandle = computePressureJob.Schedule(minionAmount, 64, hashPositionsHandle);

        var computeCollisionJob = new ComputeCollision
        {
            particleData = minionTransform,
            pressureVel = minionAddTransform,
            MinionDatas = minionDatas,
            pressureDir = minionDir,

            Target = animControllData.Target,
            MinionRadius = animControllData.MinionRadius,
            MoveSpeed = animControllData.MoveSpeed,
            PressureSpeed = animControllData.PressureSpeed,
            RotationSpeed = animControllData.RotationSpeed,
            DT = SystemAPI.Time.DeltaTime,
        };
        var computeCollisionHandle = computeCollisionJob.Schedule(minionAmount, 64, computePressureHandle);
        #endregion

        var applyNaviDataHandle = new ApplyNaviData
        {
            pressureDir = minionDir,
            NaviDatas = minionNavi,
            MinionRadius = animControllData.MinionRadius
        }.ScheduleParallel(MinionGroup, computeCollisionHandle);

        var applyPositionHandle = new ApplyPosition
        {
            particleData = minionTransform,
            pressureVel = minionAddTransform,
            MinionDatas = minionDatas,
        }.ScheduleParallel(MinionGroup, applyNaviDataHandle);
        applyPositionHandle.Complete();

        #region Dispose
        minionTransform.Dispose();
        minionAddTransform.Dispose();
        minionDatas.Dispose();
        minionNavi.Dispose();

        hashMap.Dispose();
        minionDir.Dispose();
        cellOffsetTableNative.Dispose();
        #endregion
    }
}
