using UnityEngine;
using UnityEngine.Rendering;

namespace ZG
{
    public class DualBlurVolume : VolumeComponent
    {
        public MinIntParameter downSample = new MinIntParameter(1, 0);
        public MinIntParameter iteration = new MinIntParameter(4, 0);
        public Vector2Parameter offset = new Vector2Parameter(new Vector2(6.0f, 6.0f));

        public DualBlurData data
        {
            get
            {
                DualBlurData result;
                result.downSample = downSample.value;
                result.iteration = iteration.value;
                result.offset = offset.value;

                return result;
            }
        }
    }
}