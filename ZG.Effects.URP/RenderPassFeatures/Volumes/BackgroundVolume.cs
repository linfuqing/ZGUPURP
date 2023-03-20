using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ZG
{
    public class BackgroundVolume : VolumeComponent
    {
        [Serializable]
        public struct Layer
        {
            public LayerMask layerMask;

            public float near;
            public float far;

            public void Interp(Layer from, Layer to, float t)
            {
                layerMask = t < 0.5f ? from.layerMask : to.layerMask;
                near = Mathf.Lerp(from.near, to.near, t);
                far = Mathf.Lerp(from.far, to.far, t);
            }
        }

        [Serializable]
        public class LayersParameter : VolumeParameter<Layer[]>
        {
            public override void Interp(Layer[] from, Layer[] to, float t)
            {
                var values = value;
                int source = values == null ? 0 : values.Length, 
                    fromLength = from == null ? 0 : from.Length, 
                    toLength = to == null ? 0 : to.Length, 
                    destination = Mathf.Max(
                        fromLength,
                        toLength);
                if(source < destination)
                {
                    Array.Resize(ref values, destination);

                    value = values;
                }

                for(int i = 0; i < destination; ++i)
                    values[i].Interp(i < fromLength ? from[i] : default, i < toLength ? to[i] : default, t);
            }
        }

        public LayersParameter layers = new LayersParameter();
    }
}
