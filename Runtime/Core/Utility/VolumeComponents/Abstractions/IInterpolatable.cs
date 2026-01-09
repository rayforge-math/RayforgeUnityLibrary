namespace Rayforge.Core.Utility.VolumeComponents.Abstractions
{
    public interface IInterpolatable<T>
    {
        public void Interp(T from, T to, float t);
    }
}