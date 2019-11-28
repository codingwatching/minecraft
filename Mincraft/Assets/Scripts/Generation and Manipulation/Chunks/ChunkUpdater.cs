﻿using System.Linq;
using Core.Chunking;
using Core.Chunking.Threading;
using Core.Math;
using Core.Saving;
using Extensions;
using UnityEngine;

public class ChunkUpdater : SingletonBehaviour<ChunkUpdater>
{
    //public Int3 LatestPlayerPosition => latestPlayerPosition;
    [SerializeField] private bool moveWithPlayer = true;
    [SerializeField] private int drawDistanceInChunks = 12;
    [Header("References")]
    [SerializeField] private GameObject player = null;

    private int chunkSize;

    private int minHeight;
    private int maxHeight;

    public int xPlayerPos = 0;
    public int zPlayerPos = 0;

    private ChunkJobManager chunkJobManager;
    private NoiseJobManager noiseJobManager;

    private SavingJob savingJob;
    private bool isChecking = false;

    int distanceNorm = 0;

    private void Start()
    {
        PlayerMovementTracker.OnChunkPositionChanged += (x, z) =>
        {
            this.xPlayerPos = x;
            this.zPlayerPos = z;
        };

        xPlayerPos = PlayerMovementTracker.Instance.xPlayerPos;
        zPlayerPos = PlayerMovementTracker.Instance.zPlayerPos;
        
        PlayerMovementTracker.OnDirectionModified += DirectionModified;
        
        var minMaxYHeight = ChunkSettings.MinMaxYHeight;
        minHeight = minMaxYHeight.X;
        maxHeight = minMaxYHeight.Y;

        chunkJobManager = new ChunkJobManager(true);
        chunkJobManager.Start();
        noiseJobManager = new NoiseJobManager(true);
        noiseJobManager.Start();

        chunkSize = ChunkSettings.ChunkSize;
        ChunkBuffer.Init(chunkSize, minHeight, maxHeight, drawDistanceInChunks);

        distanceNorm = 2 * drawDistanceInChunks + 1;

        for (int x = xPlayerPos - (drawDistanceInChunks * chunkSize) - chunkSize, localx = 0; x <= xPlayerPos + (drawDistanceInChunks * chunkSize) + chunkSize; x += chunkSize, localx++)
        {
            for (int z = zPlayerPos - (drawDistanceInChunks * chunkSize) - chunkSize, localz = 0; z <= zPlayerPos + (drawDistanceInChunks * chunkSize) + chunkSize; z += chunkSize, localz++)
            {
                //Create chunkColumn
                ChunkColumn column = new ChunkColumn(new Int2(x, z), new Int2(localx, localz), minHeight, maxHeight);
                ChunkBuffer.SetChunkColumn(localx, localz, column);

                //Insert this created chunkColumn to the NoiseJobs 
                for (int y = minHeight, localy = 0; y < maxHeight; y += chunkSize, localy++)
                {
                    Chunk chunk = new Chunk()
                    {
                        LocalPosition = new Int3(localx, localy, localz),
                        GlobalPosition = new Int3(x, y, z)
                    };

                    column[localy] = chunk;
                }

                NoiseJob noiseJob = new NoiseJob()
                {
                    Column = column
                };

                noiseJobManager.AddJob(noiseJob);
                column.State = DrawingState.InNoiseQueue;
                if (column.LocalPosition.X > 0 && column.LocalPosition.X <= distanceNorm &&
                    column.LocalPosition.Y > 0 && column.LocalPosition.Y <= distanceNorm)
                {
                    column.DesiredForVisualization = true;
                }
            }
        }
    }

    private void DirectionModified(Direction direction)
    {
        ChunkBuffer.Shift(direction);
    }
    
    private void Update()
    {
        isChecking = true;
        
        int dimension = ChunkBuffer.dimension;
        for (int x = 0; x < dimension; x++)
        {
            for (int y = 0; y < dimension; y++)
            {
                ChunkColumn column = ChunkBuffer.GetChunkColumn(x, y);
                if (column.DesiredForVisualization && column.State == DrawingState.NoiseReady)
                {
                    ChunkColumn[] neighbours = column.Neighbours();

                    if (neighbours.All(c => c.State == DrawingState.NoiseReady || c.State == DrawingState.Drawn))
                    {
                        column.State = DrawingState.Drawn;
                        for (int h = minHeight, localy = 0; h < maxHeight; h += chunkSize, localy++)
                        {
                            Chunk chunk = column[localy];
                            ChunkJob job = new ChunkJob();
                            job.CreateChunkFromExisting(chunk, column);
                            chunkJobManager.AddJob(job);
                        }
                    }
                }
            }
        }

        isChecking = false;
    }

    private void OnDestroy()
    {
        chunkJobManager.Dispose();
        noiseJobManager.Dispose();
    }
}