﻿using System;
using Core.Chunking;
using Core.Math;

namespace Core.Builder
{
    [System.Serializable]
    public struct Block
    {
        public Int3 Position; // Local position from [0, 0, 0] to [15, 15, 15]
        public int ID { get; set; }// For UV-Setting
        public float GlobalLightPercent { get; set; }

        public Block(Int3 position)
        {
            this.ID = -1;
            this.Position = position;
            this.GlobalLightPercent = 0f;
        }

        public void SetID(int id)
        {
            this.ID = id;
        }

        //TODO: Add this to the actual block object for performance
        public bool IsTransparent()
            => UVDictionary.IsTransparentID((BlockUV) this.ID);

        //TODO: Add this to the actual block object for performance
        public bool IsSolid()
            => UVDictionary.IsSolidID((BlockUV) this.ID);

        public float MeshOffset()
            => UVDictionary.MeshOffsetID((BlockUV)this.ID);

        public static Block Empty()
        {
            return new Block(new Int3(0, 0, 0))
            {
                ID = 0
            };
        }

        public float TransparentcyLevel()
            => UVDictionary.TransparencyLevelID((BlockUV)this.ID);
    }
}