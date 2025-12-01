using UnityEngine;

namespace Rayforge.ShaderExtensions
{
    public static class ResourcePaths
    {
        private static readonly string k_ShaderNamespace = "Rayforge/";
        public static string ShaderNamespace => k_ShaderNamespace;

        private static readonly string k_TextureResourceFolder = "Textures/";
        public static string TextureResourceFolder => k_TextureResourceFolder;

        private static readonly string k_ShaderResourceFolder = "Shaders/";
        public static string ShaderResourceFolder => k_ShaderResourceFolder;
    }
}