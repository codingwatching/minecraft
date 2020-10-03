﻿using Core.Builder;
using Core.Chunks;
using Core.Math;
using Core.Saving;
using Core.UI;
using Core.UI.Console;
using UnityEngine;
using Utilities;

namespace Core.Player.Interaction
{
    public class RemoveBlock : MonoBehaviour, IMouseUsable, IConsoleToggle, IFullScreenUIToggle
    {
        private int chunkSize;

        public float DesiredTimeUntilAction
        {
            get => timeBetweenRemove;
            set => timeBetweenRemove = value;
        }

        public float RaycastDistance
        {
            get => raycastHitable;
            set => raycastHitable = value;
        }

        public int MouseButtonIndex
        {
            get => mouseButtonIndex;
            set => mouseButtonIndex = value;
        }

        public bool Enabled
        {
            get => this.enabled;
            set => this.enabled = value;
        }

        [Header("References")] 
        [SerializeField] private Camera cameraRef;

        [Space]
        [SerializeField] private float raycastHitable = 1000f;
        [SerializeField] private float timeBetweenRemove = 0.1f;
        [SerializeField] private int mouseButtonIndex = 0;
        
        private readonly Vector3 centerScreenNormalized = new Vector3(0.5f, 0.5f, 0f);

        private RaycastHit hit;
        
        private PlaceBlockHelper placer;
        private Timer timer;

        private void Start()
        {
            timer = new Timer(DesiredTimeUntilAction);
            placer = new PlaceBlockHelper
            {
                currentBlock =
                {
                    ID = BlockUV.Air
                }
            };
        }

        private void OnValidate()
            => timer.HardReset(timeBetweenRemove);

        private void Update()
        {
            if (Input.GetMouseButton(mouseButtonIndex) && timer.TimeElapsed(Time.deltaTime))
            {
                DoRaycast();
            }

            if (Input.GetMouseButtonDown(mouseButtonIndex))
            {
                DoRaycast();
                timer.Reset();
            }
        }

        private void DoRaycast()
        {
            Ray ray = cameraRef.ViewportPointToRay(centerScreenNormalized);

            if (Physics.Raycast(ray, out hit, RaycastDistance))
            {
                ChunkReferenceHolder holder;
                if (!hit.transform.TryGetComponent(out holder))
                    return;

                Chunk currentChunk = holder.Chunk;

                placer.latestGlobalClick = MeshBuilder.CenteredClickPositionOutSide(hit.point, hit.normal) - hit.normal;


                placer.latestGlobalClickInt.X = (int) placer.latestGlobalClick.x;
                placer.latestGlobalClickInt.Y = (int) placer.latestGlobalClick.y;
                placer.latestGlobalClickInt.Z = (int) placer.latestGlobalClick.z;

                placer.GlobalToRelativeBlock(placer.latestGlobalClick, currentChunk.GlobalPosition, ref placer.lp);

                if (MathHelper.InChunkSpace(placer.lp))
                {
                    placer.HandleAddBlock(currentChunk, placer.lp);
                }
                else
                {
                    placer.GetDirectionPlusOne(placer.lp, ref placer.dirPlusOne);
                    currentChunk = currentChunk.ChunkNeighbour(placer.dirPlusOne);
                    placer.GlobalToRelativeBlock(placer.latestGlobalClick, currentChunk.GlobalPosition, ref placer.lp);

                    placer.HandleAddBlock(currentChunk, placer.lp);
                }
            }
        }
    }
}