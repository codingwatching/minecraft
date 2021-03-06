using Core.Math;
using Core.Chunks.Threading;
using Core.Player;
using Utilities;

namespace Core.Chunks
{
    public static class ChunkBuffer
    {
        public static bool UsingChunkBuffer { get; set; } = true;

        private static Array2D<ChunkColumn> data;
        private static object globalMutex = new object();
        private static ChunkJobManager chunkJobManager;

        public static int Dimension { get; private set; }
        public static int DrawDistanceInChunks { get; private set; }
        public static int ChunksVertically { get; private set; }
        public static int ChunksInTotal { get; private set; }
        
        private static int minHeight;
        private static int maxHeight;

        public static void Init(int chunkSize, int _minHeight, int _maxHeight, int drawDistanceInChunks)
        {
            chunkJobManager = ChunkJobManager.ChunkJobManagerUpdaterInstance;

            Dimension = 2 * drawDistanceInChunks + 1;
            DrawDistanceInChunks = drawDistanceInChunks;
            minHeight = _minHeight;
            maxHeight = _maxHeight;

            ChunksVertically = (System.Math.Abs(minHeight) + System.Math.Abs(maxHeight)) / chunkSize;
            ChunksInTotal = Dimension * Dimension * ChunksVertically;

            data = new Array2D<ChunkColumn>(Dimension);
        }

        #region Move

        /// <summary>
        /// Shift all entries inside the array in the given direction. Only one direction at a time supported
        /// </summary>
        /// <param name="direction">Desired direction you moved to</param>
        public static void Shift(Direction direction)
        {
            switch (direction)
            {
                case Direction.Right:
                    MoveRight();
                    break;
                case Direction.Left:
                    MoveLeft();
                    break;
                case Direction.Forward:
                    MoveForward();
                    break;
                case Direction.Back:
                    MoveBack();
                    break;
            }
        }
        
        private static void MoveForward()
        {
            const ShiftingOptionDirection dir = ShiftingOptionDirection.Forward;
            DeleteChunkColumns(dir);

            lock (globalMutex)
            {
                ShiftBlock(ShiftingOptionDirection.Forward);

                chunkJobManager.PassBegin();
                //Create new
                ChunkColumn rightNeighbour = data[0, Dimension - 1];
                for (int x = 0; x < Dimension; x++)
                {
                    Int2 globalPosition = new Int2(rightNeighbour.GlobalPosition.X + (x * 16),
                        rightNeighbour.GlobalPosition.Y + 16);
                    Int2 localPosition = new Int2(x, Dimension - 1);

                    ChunkColumn column = new ChunkColumn(globalPosition, localPosition, minHeight, maxHeight);
                    for (int h = minHeight, localy = 0; h < maxHeight; h += 16, localy++)
                    {
                        Chunk chunk = new Chunk()
                        {
                            LocalPosition = new Int3(x, localy, Dimension - 1),
                            GlobalPosition = new Int3(globalPosition.X, h, globalPosition.Y),

                            ChunkColumn = column
                        };

                        column[localy] = chunk;
                    }

                    data[x, Dimension - 1] = column;
                    
                    chunkJobManager.Add(new ChunkJob(column));
                    chunkJobManager.Add(new ChunkJob(data[x, Dimension - 2]), false);
                }

                chunkJobManager.PassEnd();
            }
        }

        private static void MoveBack()
        {
            const ShiftingOptionDirection dir = ShiftingOptionDirection.Back;
            DeleteChunkColumns(dir);

            lock (globalMutex)
            {
                //Shift everything
                for (int x = 0; x < Dimension; x++)
                {
                    for (int y = Dimension - 1; y >= 1; y--)
                    {
                        ChunkColumn column = data[x, y - 1];
                        column.LocalPosition = new Int2(x, y);

                        for (int h = 0; h < column.Chunks.Length; h++)
                        {
                            column[h].LocalPosition = new Int3(x, h, y);
                        }

                        data[x, y] = column;
                    }
                }

                chunkJobManager.PassBegin();
                //Create new
                ChunkColumn rightNeighbour = data[0, 1];
                for (int x = 0; x < Dimension; x++)
                {
                    Int2 globalPosition = new Int2(rightNeighbour.GlobalPosition.X + (x * 16),
                        rightNeighbour.GlobalPosition.Y - 16);
                    Int2 localPosition = new Int2(x, 0);

                    ChunkColumn column = new ChunkColumn(globalPosition, localPosition, minHeight, maxHeight);
                    for (int h = minHeight, localy = 0; h < maxHeight; h += 16, localy++)
                    {
                        Chunk chunk = new Chunk()
                        {
                            LocalPosition = new Int3(x, localy, 0),
                            GlobalPosition = new Int3(globalPosition.X, h, globalPosition.Y),

                            ChunkColumn = column
                        };

                        column[localy] = chunk;
                    }

                    data[x, 0] = column;

                    chunkJobManager.Add(new ChunkJob(data[x, 1]), false);
                    chunkJobManager.Add(new ChunkJob(column));
                }

                chunkJobManager.PassEnd();
            }
        }

        private static void MoveLeft()
        {
            const ShiftingOptionDirection dir = ShiftingOptionDirection.Left;
            DeleteChunkColumns(dir);

            lock (globalMutex)
            {
                //Shift everything
                for (int x = Dimension - 1; x >= 1; x--)
                {
                    for (int y = 0; y < Dimension; y++)
                    {
                        var column = data[x - 1, y];
                        column.LocalPosition = new Int2(x, y);

                        for (int h = 0; h < column.Chunks.Length; h++)
                        {
                            column[h].LocalPosition = new Int3(x, h, y);
                        }

                        data[x, y] = column;
                    }
                }

                chunkJobManager.PassBegin();
                //Create new
                ChunkColumn rightNeighbour = data[1, 0];
                for (int y = 0; y < Dimension; y++)
                {
                    Int2 globalPosition = new Int2(rightNeighbour.GlobalPosition.X - 16,
                        rightNeighbour.GlobalPosition.Y + (y * 16));
                    Int2 localPosition = new Int2(0, y);

                    ChunkColumn column = new ChunkColumn(globalPosition, localPosition, minHeight, maxHeight);
                    for (int h = minHeight, localy = 0; h < maxHeight; h += 16, localy++)
                    {
                        Chunk chunk = new Chunk()
                        {
                            LocalPosition = new Int3(0, localy, y),
                            GlobalPosition = new Int3(globalPosition.X, h, globalPosition.Y),

                            ChunkColumn = column
                        };

                        column[localy] = chunk;
                    }

                    data[0, y] = column;

                    chunkJobManager.Add(new ChunkJob(data[1, y]), false);
                    chunkJobManager.Add(new ChunkJob(column));
                }

                chunkJobManager.PassEnd();
            }
        }

        private static void MoveRight()
        {
            const ShiftingOptionDirection dir = ShiftingOptionDirection.Right;
            DeleteChunkColumns(dir);

            lock (globalMutex)
            {
                ShiftBlock(dir);

                chunkJobManager.PassBegin();
                ChunkColumn leftNeighbour = data[Dimension - 2, 0];
                for (int y = 0; y < Dimension; y++)
                {
                    Int2 globalPosition = new Int2(leftNeighbour.GlobalPosition.X + 16,
                        leftNeighbour.GlobalPosition.Y + (y * 16));
                    Int2 localPosition = new Int2(Dimension - 1, y);

                    ChunkColumn column = new ChunkColumn(globalPosition, localPosition, minHeight, maxHeight);
                    for (int h = minHeight, localy = 0; h < maxHeight; h += 16, localy++)
                    {
                        Chunk chunk = new Chunk()
                        {
                            LocalPosition = new Int3(Dimension - 1, localy, y),
                            GlobalPosition = new Int3(globalPosition.X, h, globalPosition.Y),

                            ChunkColumn = column
                        };

                        column[localy] = chunk;
                    }

                    data[Dimension - 1, y] = column;

                    chunkJobManager.Add(new ChunkJob(data[Dimension - 2, y]), false);
                    chunkJobManager.Add(new ChunkJob(column));
                }

                chunkJobManager.PassEnd();
            }
        }

        #endregion

        private static void ShiftBlock(ShiftingOptionDirection direction)
        {
            if (direction == ShiftingOptionDirection.Forward || direction == ShiftingOptionDirection.Right)
            {
                bool boundingLimitBoolean = direction == ShiftingOptionDirection.Forward;
                int yLimit = boundingLimitBoolean ? Dimension - 1 : Dimension;
                int xLimit = boundingLimitBoolean ? Dimension : Dimension - 1;

                for (int x = 0; x < xLimit; x++)
                {
                    for (int y = 0; y < yLimit; y++)
                    {
                        //column = data[x, y + 1] 
                        ChunkColumn column = boundingLimitBoolean ? data[x, y + 1] : data[x + 1, y];
                        column.LocalPosition = new Int2(x, y);

                        for (int h = 0; h < column.Chunks.Length; h++)
                        {
                            column[h].LocalPosition = new Int3(x, h, y);
                        }

                        data[x, y] = column;
                    }
                }
            }
        }

        #region Helperfunctions
        private static void DeleteChunkColumns(ShiftingOptionDirection direction)
        {
            int deleteHorizontal = direction == ShiftingOptionDirection.Forward ? 0 : Dimension - 1;
            int deleteVertical = direction == ShiftingOptionDirection.Right ? 0 : Dimension - 1;

            if (direction == ShiftingOptionDirection.Back || direction == ShiftingOptionDirection.Forward)
            {
                for (int x = 0; x < Dimension; x++)
                {
                    for (int h = minHeight, localy = 0; h < maxHeight; h += 16, localy++)
                    {
                        data[x, deleteHorizontal][localy].ReleaseGameObject();
                    }
                }
            }
            else if (direction == ShiftingOptionDirection.Left || direction == ShiftingOptionDirection.Right)
            {
                for (int y = 0; y < Dimension; y++)
                {
                    for (int h = minHeight, localy = 0; h < maxHeight; h += 16, localy++)
                    {
                        data[deleteVertical, y][localy].ReleaseGameObject();
                    }
                }
            }
        }

        public static Chunk GetChunk(Int3 local)
        {
            if (local.Y >= ChunksVertically || local.Y < 0)
                return null;

            lock (data[local.X, local.Z])
            {
                return data[local.X, local.Z][local.Y];
            }
        }

        public static ChunkColumn GetChunkColumn(int x, int z)
        {
            lock (data[x, z])
            {
                return data[x, z];
            }
        }

        /// <summary>
        /// Set ChunkColumn NonThreadSafe
        /// </summary>
        /// <param name="x">local x position of the column</param>
        /// <param name="z">local z position of the column</param>
        /// <param name="value">The ChunkColumn</param>
        public static void SetChunkColumnNTS(int x, int z, ChunkColumn value)
        {
            data[x, z] = value;
        }

        public static int DataLength => data.Length;

        public static bool InLocalSpace(Int3 localPosition)
            => localPosition.X >= 0 && localPosition.X < Dimension &&
               localPosition.Y >= 0 && localPosition.Y < Dimension &&
               localPosition.Z >= 0 && localPosition.Z < Dimension;
        
        #endregion
        
        private enum ShiftingOptionDirection
        {
            Forward,
            Back,
            Left,
            Right
        }

        public static void Clear()
        {
            for (int x = 0; x < Dimension; x++)
            {
                for (int y = 0; y < Dimension; y++)
                {
                    Chunk[] chunks = data[x, y].Chunks;
                    
                    for (int i = 0; i < chunks.Length; i++)
                    {
                        chunks[i].ReleaseGameObject();
                    }
                }
            }
        }
    }
}