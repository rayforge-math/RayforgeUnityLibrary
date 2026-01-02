namespace Rayforge.VolumeComponentUtils.Parameters
{
    [System.Serializable]
    public class NoInterpObservableParameter<T> : ObservableBase<T>
        where T : struct
    {
        public NoInterpObservableParameter(T value, bool overrideState = false)
            : base(value, overrideState) { }

        public override void Interp(T from, T to, float t)
        {
            value = t > 0f ? to : from;
        }
    }
}