﻿using Core.Builder;
using Core.Chunking;
using Core.Chunking.Debugging;
using Core.Chunking.Threading;
using Extensions;
using UnityEngine;

public class ChunkDrawerTest : SingletonBehaviour<ChunkDrawerTest>
{
    public ChunkGameObjectPool GoPool { get; set; }
    [SerializeField] private int drawsPerUpdate = 2;

    private ChunkJobManager chunkJobManager;

    private MeshModifier modifier;

    private void Start()
    {
        GoPool = ChunkGameObjectPool.Instance;
        chunkJobManager = ChunkJobManager.ChunkJobManagerUpdaterInstance;

        modifier = new MeshModifier();
    }

    private void Update()
    {
        for (int i = 0; i < chunkJobManager.FinishedJobsCount/* && i < drawsPerUpdate*/; i++)
        {
            ChunkJob task = chunkJobManager.DequeueFinishedJobs();

            if(task != null && task.Completed && task.MeshData.Vertices.Count != 0)
            {
                //if (task.Counter > 1)
                //    drawsPerUpdate = 1;
                //else
                //    drawsPerUpdate = 10;

                //if (!task.RedrawTwice)
                //    chunkJobManager.Add(task);

                RenderCall(task);
            }
        }
    }

    private void RenderCall(ChunkJob t)
    {
        var drawingChunk = t.Chunk;

        if (drawingChunk.CurrentGO == null)
        {
            drawingChunk.ChunkState = ChunkState.Drawn;
            drawingChunk.CurrentGO = GoPool.GetNextUnusedChunk();
            drawingChunk.CurrentGO.SetActive(true);
            drawingChunk.CurrentGO.transform.position = drawingChunk.GlobalPosition.ToVector3();
            drawingChunk.CurrentGO.name = drawingChunk.GlobalPosition.ToString();

            drawingChunk.CurrentGO.GetComponent<NeighbourTestVisualizer>().Chunk = drawingChunk;

            modifier.SetMesh(drawingChunk.CurrentGO, t.MeshData, t.ColliderData);
        }
        else
        {
            modifier.SetMesh(drawingChunk.CurrentGO, t.MeshData, t.ColliderData);
            drawingChunk.ChunkState = ChunkState.Drawn;
        }
    }
}
