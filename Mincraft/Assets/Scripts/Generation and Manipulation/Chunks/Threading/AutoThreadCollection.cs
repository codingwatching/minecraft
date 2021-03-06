using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Chunks.Threading.Jobs;
using Core.Math;
using Unity.Mathematics;
using UnityEngine;

namespace Core.Chunks.Threading
{
    /// <summary>
    /// Represents a chunk of threads desired to to jobs concurrently
    /// </summary>
    public abstract class AutoThreadCollection
    {
        public int FinishedJobsCount => finishedJobsCounter;
        public int NoiseJobsCount => noisePass.Count;
        public int MeshJobsCount => meshJobs.Count;

        private bool running = false;
        private bool passOpen = false;
        private int finishedJobsCounter = 0;

        private Pass noisePass;
        private Pass structurePass;
        private Pass meshPass;
        private PassArray passes;

        private ConcurrentQueue<MeshJob> finishedJobs;
        private ConcurrentQueue<IJobCollection<MeshJob>> meshJobs;

        private ConcurrentQueue<IJobCollection<MeshJob>> priorityMeshJobs;
        private SemaphoreSlim mutex;
        private Thread[] threads;

        protected AutoThreadCollection(int amountThreads)
        {
            finishedJobs = new ConcurrentQueue<MeshJob>();
            mutex = new SemaphoreSlim(0);

            meshJobs = new ConcurrentQueue<IJobCollection<MeshJob>>();
            priorityMeshJobs = new ConcurrentQueue<IJobCollection<MeshJob>>();

            threads = new Thread[amountThreads];

            for (int i = 0; i < amountThreads; i++)
            {
                threads[i] = new Thread(Run) {IsBackground = true};
            }
        }

        public void Start()
        {
            running = true;
            foreach (Thread t in threads)
                t.Start();
        }

        public void Run()
        {
            while (running)
            {
                mutex.Wait();

                if (passes.CurrentPass != null)
                {
                    Pass currentPass = passes.CurrentPass;
                    if (currentPass.Dequeue(out JobCollectionItemContainer job))
                    {
                        // In diesem Fall ist es der NoiseJob, der ausgeführt und verarbeitet wird
                        IJobCollection<MeshJob>[] seq = job.SequentialCollection;
                        for (int i = 0; i < seq.Length; i++)
                        {
                            seq[i].Execute();
                            seq[i].Finished = true;
                        }

                        //Für alle Jobs, die als parallel aktiviert wurden, werden diese einzeln in die Queue für Meshjobs
                        //hinzugefügt
                        IJobCollection<MeshJob>[] par = job.ParallelizedCollection;
                        for (int i = 0; i < par.Length; i++)
                        {
                            par[i].OtherJobs = par;
                            ScheduleMeshJob(par[i]);
                        }
                    }
                    else // Der Pass is empty. There are no items available anymore
                    {
                        passes.RemoveCurrent();
                        currentPass = passes.CurrentPass;
                        if (currentPass != null)
                        {
                            mutex.Release(currentPass.Jobs.Count + 1);
                        }
                    }
                }

                IJobCollection<MeshJob> meshJob;
                for (int i = 0; i < 50; i++)
                {
                    if (priorityMeshJobs.TryDequeue(out meshJob))
                    {
                        if (!meshJob.Finished)
                        {
                            meshJob.Execute();
                            meshJob.Finished = true;
                        }

                        if (meshJob.OtherJobs.All((IJobCollectionItem other) => other.Finished))
                        {
                            finishedJobs.Enqueue(meshJob.Target);
                            Interlocked.Increment(ref finishedJobsCounter);
                        }

                        continue;
                    }

                    if (meshJobs.TryDequeue(out meshJob))
                    {
                        if (!meshJob.Finished)
                        {
                            meshJob.Execute();
                            meshJob.Finished = true;
                        }

                        if (meshJob.OtherJobs.All(other => other.Finished))
                        {
                            finishedJobs.Enqueue(meshJob.Target);
                            Interlocked.Increment(ref finishedJobsCounter);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public MeshJob DequeueFinishedJob()
        {
            if (finishedJobs.TryDequeue(out MeshJob result))
            {
                Interlocked.Decrement(ref finishedJobsCounter);
                return result;
            }

            throw new Exception("Dequeuing but Queue is empty");
        }

        public void PassBegin()
        {
            if (passOpen) throw new InvalidOperationException("Add Range can't begin twice");

            noisePass = new Pass();
            structurePass = new Pass();
            meshPass = new Pass();

            passes = new PassArray(3);

            passOpen = true;
        }

        public void PassEnd()
        {
            if (!passOpen) throw new InvalidOperationException("Add Range can't end twice");
            passOpen = false;

            passes.Add(noisePass);
            passes.Add(structurePass);
            passes.Add(meshPass);

            // + 1 für irgendeinen Thread, der abfragt ob ein Item im pass ist, feststellt, dass dort kein Item enthalten
            // ist und den Pass dann entfernt
            mutex.Release(noisePass.Jobs.Count + 1);
        }

        private void ScheduleMeshJob(IJobCollection<MeshJob> job, ChunkJobPriority priority = ChunkJobPriority.Normal)
        {
            switch (priority)
            {
                case ChunkJobPriority.Normal:
                {
                    meshJobs.Enqueue(job);
                    mutex.Release();
                    break;
                }
                case ChunkJobPriority.High:
                {
                    priorityMeshJobs.Enqueue(job);
                    mutex.Release();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(priority), priority, null);
            }
        }

        public void Add(ChunkJob item, bool runWithNoise = true)
        {
            if (passOpen)
            {
                int len = item.Column.Chunks.Length;
                for (int i = 0; i < len; i++)
                {
                    MeshJob job = new MeshJob(item.Column.Chunks[i]);

                    IJobCollection<MeshJob> noiseJob = new GenerateNoiseJob(job);
                    IJobCollection<MeshJob> structureJob = new GenerateStructuresJob(job);
                    IJobCollection<MeshJob> meshJob = new MeshBuilderJob(job);
                    IJobCollection<MeshJob> greedyJob = new ReduceColliderJob(job);

                    if (runWithNoise)
                    {
                        //Noisejob
                        var noiseJobContainer = new JobCollectionItemContainer(1, 0);
                        noiseJobContainer.RunSequentially(noiseJob);
                        noisePass.Add(noiseJobContainer);
                    }

                    //Structurejob
                    var structureJobContainer = new JobCollectionItemContainer(1, 0);
                    structureJobContainer.RunSequentially(structureJob);

                    structurePass.Add(structureJobContainer);

                    //Meshjob
                    var meshJobContainer = new JobCollectionItemContainer(0, 2);
                    meshJobContainer.RunParallelized(meshJob, greedyJob);
                    meshPass.Add(meshJobContainer);
                }
            }
        }
        
        public void Add(MeshJob job, bool runWithNoise = true)
        {
            if (passOpen)
            {
                IJobCollection<MeshJob> noiseJob = new GenerateNoiseJob(job);
                IJobCollection<MeshJob> structureJob = new GenerateStructuresJob(job);
                IJobCollection<MeshJob> meshJob = new MeshBuilderJob(job);
                IJobCollection<MeshJob> greedyJob = new ReduceColliderJob(job);

                if (runWithNoise)
                {
                    //Noisejob
                    var noiseJobContainer = new JobCollectionItemContainer(1, 0);
                    noiseJobContainer.RunSequentially(noiseJob);
                    noisePass.Add(noiseJobContainer);
                }

                //Structurejob
                var structureJobContainer = new JobCollectionItemContainer(1, 0);
                structureJobContainer.RunSequentially(structureJob);

                structurePass.Add(structureJobContainer);

                //Meshjob
                var meshJobContainer = new JobCollectionItemContainer(0, 2);
                meshJobContainer.RunParallelized(meshJob, greedyJob);
                meshPass.Add(meshJobContainer);
            }
        }

        public void RecalculateChunk(Chunk item, ChunkJobPriority priority)
        {
            MeshJob job = new MeshJob(item);
            // PassBegin();
            // Add(job, false);
            // PassEnd();
            
            IJobCollection<MeshJob> meshJob = new MeshBuilderJob(job);
            IJobCollection<MeshJob> greedyJob = new ReduceColliderJob(job);
            
            //Meshjob
            var meshJobContainer = new JobCollectionItemContainer(0, 2);
            meshJobContainer.RunParallelized(meshJob, greedyJob);
            
            IJobCollection<MeshJob>[] par = meshJobContainer.ParallelizedCollection;
            foreach (IJobCollection<MeshJob> t in par)
            {
                t.OtherJobs = par;
                ScheduleMeshJob(t, priority);
            }
        }

        public void Stop()
        {
            running = false;

            foreach (Thread t in threads)
                t.Abort();
        }
    }
}