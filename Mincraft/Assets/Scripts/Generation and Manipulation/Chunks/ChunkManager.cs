﻿using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ChunkManager : SingletonBehaviour<ChunkManager>
{
    public static Vector3Int GetMaxSize => Instance.maxSize;
    
    [SerializeField] private Vector3Int maxSize;
    
    [HideInInspector]
    public List<IChunk> chunks = new List<IChunk>();

    private ChunkGameObjectPool chunkGOPool;

    private void Start()
    {
        chunkGOPool = ChunkGameObjectPool.Instance;
    }

    /// <summary>
    /// Adds the block to the chunk AND DOES NOT REDRAW
    /// </summary>
    /// <param name="block"></param>
    /// <returns></returns>
    public IChunk AddBlock(Block block)
    {
        BlockDictionary.Add(block.Position, block.Position);
        IChunk chunk = GenerateOrGetChunkGameObject(block.Position);
        chunk.AddBlock(block);

        return chunk;
    }

    /// <summary>
    /// Removes the block from the chunk AND REDRAWS
    /// </summary>
    /// <param name="currentChunk"></param>
    /// <param name="block"></param>
    public void RemoveBlock(GameObject currentChunk, Block block)
    {
        BlockDictionary.Remove(block.Position);
        
        IChunk chunk = currentChunk.GetComponent<IChunk>();
        chunk.RemoveBlock(block);

        MeshData data = ModifyMesh.Combine(chunk);
        ModifyMesh.RedrawMeshFilter(currentChunk, data);
        
        DeleteChunkIfNotNeeded(chunk);
    }

    private void DeleteChunkIfNotNeeded(IChunk currentChunk)
    {
        if (currentChunk.BlockCount() == 0)
        {
            chunks.Remove(currentChunk);
            ChunkDictionary.Remove(currentChunk.ChunkOffset);
            Destroy(currentChunk.CurrentGO);
        }
    }

    private IChunk GenerateOrGetChunkGameObject(Vector3 target)
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            (Vector3Int lowerBound, Vector3Int higherBound) = chunks[i].GetChunkBounds();
            
            if ((target.x >= lowerBound.x && target.x <= higherBound.x) &&
                (target.y >= lowerBound.y && target.y <= higherBound.y) &&// Dann liegt es dazwischen und muss 
                (target.z >= lowerBound.z && target.z <= higherBound.z))// dementsprechend in diesen Chunk
            {
                return chunks[i];
            }
        }

        int x = Mathf.RoundToInt(target.x / maxSize.x) * maxSize.x;
        int y = Mathf.RoundToInt(target.y / maxSize.y) * maxSize.y;
        int z = Mathf.RoundToInt(target.z / maxSize.z) * maxSize.z;
        
        Vector3Int chunkPos = new Vector3Int(x, y, z);

        var chunk = new Chunk {ChunkOffset = chunkPos, CurrentGO = chunkGOPool.GetNextUnusedChunk(),};
        
        chunks.Add(chunk);
        return chunk;
    }
    
    public static (Vector3Int[] Directions, bool Result) IsBoundBlock((Vector3Int lowerBound, Vector3Int higherBound) tuple, Vector3Int pos)
    {
        Vector3Int chunkSize = GetMaxSize;
        List<Vector3Int> directions = new List<Vector3Int>();
        bool result = false;
        
        
        if (pos.x == tuple.lowerBound.x || pos.x - 1 == tuple.lowerBound.x)
        {
            directions.Add(new Vector3Int(-chunkSize.x, 0, 0));
            result = true;
        }

        if (pos.y == tuple.lowerBound.y || pos.y - 1 == tuple.lowerBound.y)
        {
            directions.Add(new Vector3Int(0, -chunkSize.y, 0));
            result = true;
        }

        if (pos.z == tuple.lowerBound.z || pos.z - 1 == tuple.lowerBound.z)
        {
            directions.Add(new Vector3Int(0, 0, -chunkSize.z));
            result = true;
        }
        
        if (pos.x == tuple.higherBound.x || pos.x + 1 == tuple.higherBound.x)
        {
            directions.Add(new Vector3Int(chunkSize.x, 0, 0));
            result = true;
        }

        if (pos.y == tuple.higherBound.y || pos.y + 1 == tuple.higherBound.y)
        {
            directions.Add(new Vector3Int(0, chunkSize.y, 0));
            result = true;
        }
        
        if (pos.z == tuple.higherBound.z || pos.z + 1 == tuple.higherBound.z)
        {
            directions.Add(new Vector3Int(0, 0, chunkSize.z));
            result = true;
        }


        return (directions.ToArray(), result);
    }
}