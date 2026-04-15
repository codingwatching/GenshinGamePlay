using System;
using UnityEngine;

namespace TaoTie
{
    public class TouchInfo : IDisposable
    {
        public bool IsScroll;
        public bool IsStartOverUI;
        public TouchPhase Phase;
        public Vector2 Position;
        public Vector2 DeltaPosition;
        public void Dispose()
        {
            IsStartOverUI = false;
            IsScroll = false;
        }
    }
}