﻿using System.Collections.Generic;
using System.Linq;
using Core.Chunks;
using Core.Math;
using UnityEngine;

namespace Core.Builder
{
	public static class MeshBuilder
	{
		private static Vector3[] directions = { Vector3.forward, Vector3.zero, Vector3.up, Vector3.zero, Vector3.zero, Vector3.right };
		private static Vector3[] offset1 = { Vector3.right, Vector3.right, Vector3.right, Vector3.right, Vector3.forward, Vector3.forward };
		private static Vector3[] offset2 = { Vector3.up, Vector3.up, Vector3.forward, Vector3.forward, Vector3.up, Vector3.up };

		private static int[] tri1 = { 1, 0, 2, 1, 2, 3 };
		private static int[] tri2 = { 0, 1, 2, 2, 1, 3 };
		private static int[] tris = { 1, 0, 0, 1, 1, 0 };
		private static int chunkSize = 0x10;
		

        public static Vector3 CenteredClickPositionOutSide(Vector3 hitPoint, Vector3 hitNormal)
	    {
	        //Hier wird global berechnet.
	        Vector3 blockPos = hitPoint + hitNormal / 2.0f;

	        blockPos.x = Mathf.FloorToInt(blockPos.x);
	        blockPos.y = Mathf.FloorToInt(blockPos.y);
	        blockPos.z = Mathf.FloorToInt(blockPos.z);
	        
	        return blockPos;
	    }

        public static MeshData Combine(Chunk chunk)
	    {
		    List<Vector3> vertices = new List<Vector3>();
		    List<int> triangles = new List<int>();
	        List<int> transparentTriangles = new List<int>();
		    List<Vector2> uvs = new List<Vector2>();

		    const int chunksize = 16;
            
            ExtendedArray3D<Block> blocks = chunk.Blocks;
            Block[] neighbourBlocks = new Block[6];
            bool[] boolNeighbours = new bool[6];
            
		    for (int x = 0; x < chunksize; x++)
		    {
			    for (int y = 0; y < chunksize; y++)
			    {
				    for (int z = 0; z < chunksize; z++)
				    {
					    Block block = blocks[x, y, z];
					    Int3 pos = new Int3(x, y, z);

		                bool transparent = block.IsTransparent();

		                if (block.ID == (short)BlockUV.Air)
		                {
			                continue;
		                }
		                //Check, ob dieser Block transparent ist, oder nicht
		                // Wenn es so sein sollte, bleibt das neighbours-Array mit 6 false-Werten und jede Seite wird gezeichnet
		                neighbourBlocks = chunk.GetBlockNeighbours(pos);

		                if (!transparent)
		                {
			                for (int j = 0; j < 6; j++)
			                {
				                Block currentNeighbour = neighbourBlocks[j];
				                boolNeighbours[j] = currentNeighbour.ID != BlockUV.Air && !currentNeighbour.IsTransparent();
			                }
		                }
		                else
		                {
			                for (int j = 0; j < 6; j++)
			                {
				                boolNeighbours[j] = false;
			                }
		                }

		                Vector3 blockPos = pos.ToVector3();

		                if (boolNeighbours.Any(state => state == false))
		                {
			                UVData[] currentUVData = UVDictionary.GetValue(block.ID);
			                float meshOffset = UVDictionary.MeshOffsetID(block.ID);

			                for (int faceIndex = 0; faceIndex < 6; faceIndex++)
			                {
				                if (boolNeighbours[faceIndex] == false)
				                {
					                int vc = vertices.Count;


					                #region Meshoffsets

					                Vector3 dir = directions[faceIndex];
					                Vector3 off1 = offset1[faceIndex];
					                Vector3 off2 = offset2[faceIndex];

					                Vector3 meshOffsetForwardBack = new Vector3(0f, 0f, meshOffset);
					                Vector3 meshOffsetLeftRight = new Vector3(meshOffset, 0f, 0f);
					                
					                switch (faceIndex)
					                {
						                case 0: //Forward
							                vertices.Add(dir               + blockPos - meshOffsetForwardBack);
							                vertices.Add(dir + off1        + blockPos - meshOffsetForwardBack);
							                vertices.Add(dir        + off2 + blockPos - meshOffsetForwardBack);
							                vertices.Add(dir + off1 + off2 + blockPos - meshOffsetForwardBack);
							                break;
						                case 1: //Back
							                vertices.Add(dir               + blockPos + meshOffsetForwardBack);
							                vertices.Add(dir + off1        + blockPos + meshOffsetForwardBack);
							                vertices.Add(dir        + off2 + blockPos + meshOffsetForwardBack);
							                vertices.Add(dir + off1 + off2 + blockPos + meshOffsetForwardBack);
							                break;
						                case 2: //Up
							                vertices.Add(dir               + blockPos);
							                vertices.Add(dir + off1        + blockPos);
							                vertices.Add(dir        + off2 + blockPos);
							                vertices.Add(dir + off1 + off2 + blockPos);
							                break;
						                case 3: // Down
							                vertices.Add(dir               + blockPos);
							                vertices.Add(dir + off1        + blockPos);
							                vertices.Add(dir        + off2 + blockPos);
							                vertices.Add(dir + off1 + off2 + blockPos);
							                break;
						                case 4: //Left
							                vertices.Add(dir               + blockPos + meshOffsetLeftRight);
							                vertices.Add(dir + off1        + blockPos + meshOffsetLeftRight);
							                vertices.Add(dir        + off2 + blockPos + meshOffsetLeftRight);
							                vertices.Add(dir + off1 + off2 + blockPos + meshOffsetLeftRight);
							                break;
						                case 5: //Right
							                vertices.Add(dir               + blockPos - meshOffsetLeftRight);
							                vertices.Add(dir + off1        + blockPos - meshOffsetLeftRight);
							                vertices.Add(dir        + off2 + blockPos - meshOffsetLeftRight);
							                vertices.Add(dir + off1 + off2 + blockPos - meshOffsetLeftRight);
							                break;
					                }

					                #endregion

					                if (!transparent)
					                {
						                for (int k = 0; k < 6; k++)
						                {
							                triangles.Add(vc + (tris[faceIndex] == 0 ? tri1[k] : tri2[k]));
						                }
					                }
					                else
					                {
						                for (int k = 0; k < 6; k++)
						                {
							                transparentTriangles.Add(vc + (tris[faceIndex] == 0 ? tri1[k] : tri2[k]));
						                }
					                }

					                UVData uvdata = currentUVData[faceIndex];
					                
					                uvs.Add(new Vector2(uvdata.TileX, uvdata.TileY));
					                uvs.Add(new Vector2(uvdata.TileX + uvdata.SizeX, uvdata.TileY));
					                uvs.Add(new Vector2(uvdata.TileX, uvdata.TileY + uvdata.SizeY));
					                uvs.Add(new Vector2(uvdata.TileX + uvdata.SizeX, uvdata.TileY + uvdata.SizeY));
				                }
			                }
		                }
				    }
			    }
		    }
		    
		    return new MeshData(vertices, triangles, transparentTriangles, uvs, chunk.CurrentGO);
	    }

        
        public static MeshData TestCombine(Chunk chunk)
	    {   
		    ExtendedArray3D<Block> blocks = chunk.Blocks;
		    List<Vector3> vertices = new List<Vector3>();
		    List<int> triangles = new List<int>();
	        List<int> transparentTriangles = new List<int>();
		    List<Vector2> uvs = new List<Vector2>();

		    for (int x = 0; x < 16; x++)
            {
	            for (int y = 0; y < 16; y++)
	            {
		            for (int z = 0; z < 16; z++)
		            {
			            bool transparent = blocks[x, y, z].IsTransparent();

		                Block block = blocks[x, y, z];
					    if (block.ID == (short) BlockUV.Air)
					    {
						    continue;
					    }

					    Vector3 blockPos = new Vector3(x, y, z);
					    
					    UVData[] currentUVData = UVDictionary.GetValue((BlockUV) block.ID);
		                float meshOffset = UVDictionary.MeshOffsetID((BlockUV)block.ID);

					    for (int faceIndex = 0; faceIndex < 6; faceIndex++)
					    {
						    int vc = vertices.Count;


		                    #region Meshoffsets
		                    switch (faceIndex)
		                    {
		                        case 0: //Forward
		                            vertices.Add(directions[faceIndex] + blockPos - new Vector3(0, 0f, meshOffset));
		                            vertices.Add(directions[faceIndex] + offset1[faceIndex] + blockPos - new Vector3(0, 0f, meshOffset));
		                            vertices.Add(directions[faceIndex] + offset2[faceIndex] + blockPos - new Vector3(0, 0f, meshOffset));
		                            vertices.Add(directions[faceIndex] + offset1[faceIndex] + offset2[faceIndex] + blockPos - new Vector3(0, 0f, meshOffset));
		                            break;
		                        case 1: //Back
		                            vertices.Add(directions[faceIndex] + blockPos + new Vector3(0, 0f, meshOffset));
		                            vertices.Add(directions[faceIndex] + offset1[faceIndex] + blockPos + new Vector3(0, 0f, meshOffset));
		                            vertices.Add(directions[faceIndex] + offset2[faceIndex] + blockPos + new Vector3(0, 0f, meshOffset));
		                            vertices.Add(directions[faceIndex] + offset1[faceIndex] + offset2[faceIndex] + blockPos + new Vector3(0, 0f, meshOffset));
		                            break;
		                        case 2: //Up
		                            vertices.Add(directions[faceIndex] + blockPos);
		                            vertices.Add(directions[faceIndex] + offset1[faceIndex] + blockPos);
		                            vertices.Add(directions[faceIndex] + offset2[faceIndex] + blockPos);
		                            vertices.Add(directions[faceIndex] + offset1[faceIndex] + offset2[faceIndex] + blockPos);
		                            break;
		                        case 3: // Down
		                            vertices.Add(directions[faceIndex] + blockPos);
		                            vertices.Add(directions[faceIndex] + offset1[faceIndex] + blockPos);
		                            vertices.Add(directions[faceIndex] + offset2[faceIndex] + blockPos);
		                            vertices.Add(directions[faceIndex] + offset1[faceIndex] + offset2[faceIndex] + blockPos);
		                            break;
		                        case 4: //Left
		                            vertices.Add(directions[faceIndex] + blockPos + new Vector3(meshOffset, 0f, 0f));
		                            vertices.Add(directions[faceIndex] + offset1[faceIndex] + blockPos + new Vector3(meshOffset, 0f, 0f));
		                            vertices.Add(directions[faceIndex] + offset2[faceIndex] + blockPos + new Vector3(meshOffset, 0f, 0f));
		                            vertices.Add(directions[faceIndex] + offset1[faceIndex] + offset2[faceIndex] + blockPos + new Vector3(meshOffset, 0f, 0f));
		                            break;
		                        case 5: //Right
		                            vertices.Add(directions[faceIndex] + blockPos - new Vector3(meshOffset, 0f, 0f));
		                            vertices.Add(directions[faceIndex] + offset1[faceIndex] + blockPos - new Vector3(meshOffset, 0f, 0f));
		                            vertices.Add(directions[faceIndex] + offset2[faceIndex] + blockPos - new Vector3(meshOffset, 0f, 0f));
		                            vertices.Add(directions[faceIndex] + offset1[faceIndex] + offset2[faceIndex] + blockPos - new Vector3(meshOffset, 0f, 0f));
		                            break;
		                    }
		                    #endregion

		                    if (!transparent)
		                    {
						        for (int k = 0; k < 6; k++)
						        {
							        triangles.Add(vc + (tris[faceIndex] == 0 ? tri1[k] : tri2[k]));
						        }
		                    }
		                    else
		                    {
		                        for (int k = 0; k < 6; k++)
		                        {
		                            transparentTriangles.Add(vc + (tris[faceIndex] == 0 ? tri1[k] : tri2[k]));
		                        }
		                    }

						    uvs.Add(new Vector2(currentUVData[faceIndex].TileX, currentUVData[faceIndex].TileY));
						    uvs.Add(new Vector2(currentUVData[faceIndex].TileX + currentUVData[faceIndex].SizeX, currentUVData[faceIndex].TileY));
						    uvs.Add(new Vector2(currentUVData[faceIndex].TileX, currentUVData[faceIndex].TileY + currentUVData[faceIndex].SizeY));
						    uvs.Add(new Vector2(currentUVData[faceIndex].TileX + currentUVData[faceIndex].SizeX,currentUVData[faceIndex].TileY + currentUVData[faceIndex].SizeY));
					    }
		            }
	            }
            }


		    
		    return new MeshData(vertices, triangles, transparentTriangles, uvs, chunk.CurrentGO);
	    }
    }
}
