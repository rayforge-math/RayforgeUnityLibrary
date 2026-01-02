namespace Rayforge.Rendering.Blitter
{
    /// <summary>
    /// Represents a single color channel in a texture.
    /// Used to map source channels to target channels for blitting operations.
    /// </summary>
    public enum Channel : uint
    {
        /// <summary>Red channel (0)</summary>
        R = 0,
        /// <summary>Green channel (1)</summary>
        G = 1,
        /// <summary>Blue channel (2)</summary>
        B = 2,
        /// <summary>Alpha channel (3)</summary>
        A = 3,
        /// <summary>No channel mapping / ignore</summary>
        None = 4
    }
}