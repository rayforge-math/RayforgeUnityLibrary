namespace Rayforge.CustomVolumeComponent.Example
{
    [System.Serializable, VolumeComponentMenu("Custom/Test")]
    private class TestVolumeComponent : VolumeComponent, IPostProcessComponent
    {
        public enum ValueEnabled
        {
            None = 0,
            Val1 = 1,
            Val12 = 2
        }

        [System.Serializable]
        public struct Entry
        {   
            public float x;
            public int y;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // register for observables at startup
            RegisterParameters();
        }

        protected override void OnDisable()
        {
            UnregisterParameters();
            base.OnDisable();
        }

        // using the Foldout attribute you can easily arange parameters in foldout groups
        private const string k_Foldout = "MyFoldout";

        // just define an observable parameter like this and pass in any value type as a template parameter, 
        // non default types still require a custom drawer
        [InspectorName("Enable values"), Foldout(k_Foldout)]
        public NoInterpObservableParameter<ValueEnabled> enableValues = new NoInterpObservableParameter<ValueEnabled>(ValueEnabled.Val1);
        // using the ConditionalField attribute lets you easily grey out parameters,
        // it takes in the parameter name as a string, the value to compare against for equality and optionally a bool signaling to invert the equality check result.
        // It's possible to pass in an array of values as an object[] to compare against.
        [InspectorName("Enable value 2"), Foldout(k_Foldout)]
        public ObservableBoolParameter enableValue2 = new ObservableBoolParameter(false);
        [InspectorName("Value 1"), Foldout(k_Foldout)]
        [ConditionalField(
            "enableValues",
            new ValueEnabled[] { ValueEnabled.Val1, ValueEnabled.Val12 })]
        public ObservableBoolParameter value1 = new ObservableBoolParameter(false);
        // the ConditionalFields attribute lets you do the same like the ConditionalField attribute, but takes in an array of values for each parameter
        [InspectorName("Value 2"), Foldout(k_Foldout)]
        [ConditionalFields(
            new[] { "enableValues", "enableValue2" },
            new object[] { new ValueEnabled[] { ValueEnabled.None, ValueEnabled.Val1 }, true },
            new[] { true, false })]
        public ObservableClampedFloatParameter value2 = new ObservableClampedFloatParameter(0.0f, 0.0f, 1.0f);
        [InspectorName("Entries"), Foldout(k_Foldout)]
        public NoInterpObservableListParameter<Entry> entries = new NoInterpObservableListParameter<Entry>();

        // for the observable parameters just declare dirty flags however you want and register a handler that gets triggered on each value change in order to set it.
        private bool m_Dirty = false;
        public bool Dirty => m_Dirty;
        public void ResetDirty() => m_Dirty = false;

        // The observable will set the dirty flag whenever the internal value gets set
        private void HandleChanged<T>(IObservableParameter<T> observable)
        {
            m_Dirty = true;
        }

        // the Changed method internaly checks if the values got actually changed and caches them if true for the next comparison
        public bool Changed()
        {
            return
                enableValues.Changed() ||
                enableValue2.Changed() ||
                value1.Changed() ||
                value2.Changed() ||
                entries.Changed();
        }

        private void RegisterParameters()
        {
            enableValues.OnValueChanged += HandleChanged;
            enableValue2.OnValueChanged += HandleChanged;
            value1.OnValueChanged += HandleChanged;
            value2.OnValueChanged += HandleChanged;
            entries.OnValueChanged += HandleChanged;

            m_Dirty = true;
        }

        private void UnregisterParameters()
        {
            enableValues.OnValueChanged -= HandleChanged;
            enableValue2.OnValueChanged -= HandleChanged;
            value1.OnValueChanged -= HandleChanged;
            value2.OnValueChanged -= HandleChanged;
            entries.OnValueChanged -= HandleChanged;
        }
    }

#if UNITY_PIPELINE_URP
    public class TestRenderPass : ScriptableRenderPass
    {
        public TestRenderPass(Material material)
        {}

        private static Tvol GetVolumeComponent<Tvol>()
            where Tvol : VolumeComponent
        {
            var stack = VolumeManager.instance.stack;
            return stack.GetComponent<Tvol>();
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var component = GetVolumeComponent<TestVolumeComponent>();

            if (component != null && component.IsActive())
            {
                //if you are just using a global volume without any interpolation it's enough to check against the dirty flag
                if(component.Dirty)
                {
                    var enableValues = component.enableValues.value;
                    /* ... */

                    component.ResetDirty();
                }

                //if you are using interpolation use the Dirty flag as a fast check and then check if the value actually got changed.
                // this is due to the component system interpolating multiple times between component values and calling the setter, triggering the dirty flag
                // every frame. Afterwards compare the value to the last cached value using Changed. This is the simpelest solution I came up with making global and interpolated
                // volume usage possible while saving on unnecessary and possibly "more" expensive operations.
                if(component.Dirty)
                {
                    if(component.enableValues.Changed())
                    {
                        var enableValues = component.enableValues.value;
                        /* ... */
                    }
                    
                    component.ResetDirty();
                }
            }
        }
    }
#endif
}