using UnityEngine.Rendering;

namespace Rayforge.VolumeComponentUtils.Helpers
{
    /// <summary>
    /// Provides utility functions for accessing volume components from the global <see cref="VolumeManager"/> stack.
    /// </summary>
    public static class VolumeComponentHelpers
    {
        /// <summary>
        /// Retrieves a volume component of the specified type from the active <see cref="VolumeManager"/> stack.
        /// </summary>
        /// <typeparam name="Tvol">
        /// The type of the <see cref="VolumeComponent"/> to retrieve. Must inherit from <see cref="VolumeComponent"/>.
        /// </typeparam>
        /// <returns>
        /// The active instance of the requested volume component type if it exists in the current volume stack;
        /// otherwise, <c>null</c>.
        /// </returns>
        public static Tvol GetVolumeComponent<Tvol>()
            where Tvol : VolumeComponent
        {
            var stack = VolumeManager.instance.stack;
            return stack.GetComponent<Tvol>();
        }
    }
}