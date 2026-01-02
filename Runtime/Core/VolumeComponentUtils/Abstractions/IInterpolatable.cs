namespace Rayforge.VolumeComponentUtils.Abstractions
{
    public interface IInterpolatable<T>
    {
        public void Interp(T from, T to, float t);
    }
}