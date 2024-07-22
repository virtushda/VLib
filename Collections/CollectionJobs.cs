using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

// Required for generic Burst compilation
[assembly: RegisterGenericJobType(typeof(VLib.CollectionJobs.MatchListLengthToJob<float, int>))]

namespace VLib
{
    public static class CollectionJobs
    {
        public static JobHandle MatchListLengthTo<T, U>(this NativeList<T> modifyList, NativeList<U> sourceLengthList, JobHandle inDeps = default) 
            where T : unmanaged 
            where U : unmanaged
        {
            return new MatchListLengthToJob<U, T>(sourceLengthList, modifyList).Schedule(inDeps);
        }
        
        [BurstCompile]
        public struct MatchListLengthToJob<T, U> : IJob 
            where T : unmanaged 
            where U : unmanaged
        {
            NativeList<T> sourceLengthList;
            NativeList<U> modifyLengthList;

            public MatchListLengthToJob(NativeList<T> sourceLengthList, NativeList<U> modifyLengthList)
            {
                this.sourceLengthList = sourceLengthList;
                this.modifyLengthList = modifyLengthList;
            }

            public void Execute()
            {
                modifyLengthList.Length = sourceLengthList.Length;
            }
        }
    }
}