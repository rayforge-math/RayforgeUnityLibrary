using UnityEngine;
using Rayforge.ManagedResources.Abstractions;
using System.Runtime.InteropServices;

namespace Rayforge.Rendering.Blitter
{
    /// <summary>
    /// Parameter struct for channel blitting operations.
    /// Used to specify per-channel mapping and source rectangle within the source texture.
    /// Layout is sequential to match GPU cbuffer layout.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ChannelBlitterParams : IComputeData<ChannelBlitterParams>
    {
        /// <summary>Target red channel is mapped from this source channel.</summary>
        public Channel R;
        /// <summary>Target green channel is mapped from this source channel.</summary>
        public Channel G;
        /// <summary>Target blue channel is mapped from this source channel.</summary>
        public Channel B;
        /// <summary>Target alpha channel is mapped from this source channel.</summary>
        public Channel A;

        /// <summary>
        /// Pixel offset of the source rectangle within the source texture.
        /// Used to shift the sampling window before scaling.
        /// </summary>
        public Vector2 offset;

        /// <summary>
        /// Size of the source rectangle in pixels.
        /// Defines the portion of the source texture to map onto the target.
        /// </summary>
        public Vector2 size;

        /// <summary>
        /// Returns data as raw compute data struct.
        /// </summary>
        public ChannelBlitterParams RawData => this;
    }
}