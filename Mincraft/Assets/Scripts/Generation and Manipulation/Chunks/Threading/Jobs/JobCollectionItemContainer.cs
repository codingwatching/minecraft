namespace Core.Chunks.Threading.Jobs
{
    public struct JobCollectionItemContainer
    {
        public IJobCollection<MeshJob>[] SequentialCollection;
        public IJobCollection<MeshJob>[] ParallelizedCollection;

        private int currentIndex;

        public JobCollectionItemContainer(int sequentialLength, int parallelizedLength)
        {
            this.SequentialCollection = new IJobCollection<MeshJob>[sequentialLength];
            this.ParallelizedCollection = new IJobCollection<MeshJob>[parallelizedLength];
            this.currentIndex = 0;
        }

        public void RunSequentially(params IJobCollection<MeshJob>[] items)
        {
            SequentialCollection = items;
        }

        public void RunParallelized(params IJobCollection<MeshJob>[] items)
        {
            ParallelizedCollection = items;
        }
    }
}