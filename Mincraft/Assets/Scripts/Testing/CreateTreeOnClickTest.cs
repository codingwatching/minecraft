using Core.Builder;
using Core.Chunking;
using Core.Chunking.Threading;
using Core.Math;
using Core.Performance.Parallelisation;
using Core.Player;
using Core.StructureGeneration;
using UnityEngine;

namespace Testing
{
    public class CreateTreeOnClickTest : MonoBehaviour
    {
        private Int2 playerPos = Int2.Zero;
        private Vector3 chunkPos = Vector3.zero;

        private int drawDistance;
        private const int CHUNKSIZE = 0x10;

        //private MeshJobManager _meshJobManager;
        private JobManager _jobManager;

        private void Start()
        {
            PlayerMovementTracker.OnChunkPositionChanged += (x, y) =>
            {
                playerPos.X = x;
                playerPos.Y = y;
            };

            _jobManager = JobManager.JobManagerUpdaterInstance;
            //_meshJobManager = MeshJobManager.MeshJobManagerUpdaterInstance;
        }

//        private void Update()
//        {
//            if (Input.GetKeyDown(KeyCode.H))
//            {
//                Chunk c = ChunkBuffer.GetChunk(3, 5, 2);
//
//                c.AddBlock(new Block(new Int3(8, 8, 8))
//                {
//                    ID = (int) BlockUV.Dirt
//                });
//                c.ChunkState = ChunkState.Dirty;
//
//                ChunkBuffer.GetChunkColumn(c.LocalPosition.X, c.LocalPosition.Z).Dirty = true;
//            }
//        }
    }
}