namespace Rayforge.VolumeComponentUtils.Abstractions
{
    public delegate void NotifyDelegate<T>(IObservableParameter<T> sender);

    public interface IObservableParameter<T>
    {
        public event NotifyDelegate<T> OnValueChanged;
        public void NotifyObservers();
        public bool Changed();
    }
}