namespace Rayforge.VolumeComponentUtils.Parameters
{
    [System.Serializable]
    public class ObservableParameter<T> : ObservableBase<T>
        where T : struct
    {
        private readonly InterpFunc m_InterpFunc;

        public ObservableParameter(T value, InterpFunc interp, bool overrideState = false)
            : base(value, overrideState)
        {
            m_InterpFunc = interp;
        }

        public override void Interp(T from, T to, float t)
        {
            if (m_InterpFunc != null)
            {
                value = m_InterpFunc.Invoke(from, to, t);
            }
        }
    }
}