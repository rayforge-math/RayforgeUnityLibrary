using UnityEngine;

namespace Rayforge.Utility.Rendering
{
    public static class SharedTexture
    {
        public static void Ensure(string property, Texture2D texture)
            => Ensure(Shader.PropertyToID(property), texture);

        public static void Ensure(int propertyId, Texture2D texture)
        {
            var existing = Shader.GetGlobalTexture(propertyId);
            if (existing == null)
            {
                Shader.SetGlobalTexture(propertyId, texture);
            }
        }
    }
}
