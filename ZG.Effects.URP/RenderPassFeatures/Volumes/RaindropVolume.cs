using UnityEngine.Rendering;

namespace ZG
{
    public class RaindropVolume : VolumeComponent
    {
        public FloatParameter rainAmountSmoothTime = new FloatParameter(5.0f);

        public FloatParameter timeSmoothTime = new FloatParameter(0.5f);

        public ClampedFloatParameter rainAmount = new ClampedFloatParameter(0.3f, 0.0f, 1.0f);

        public FloatParameter rainZoom = new FloatParameter(0.5f);

        public FloatParameter timeScale = new FloatParameter(1.0f);

        public FloatParameter maxBlur = new FloatParameter(5.0f);
        public FloatParameter minBlur = new FloatParameter(2.0f);

        public RaindropData data
        {
            get
            {
                RaindropData result;
                result.rainAmountSmoothTime = rainAmountSmoothTime.value;
                result.timeSmoothTime = timeSmoothTime.value;
                result.rainAmount = rainAmount.value;
                result.rainZoom = rainZoom.value;
                result.timeScale = timeScale.value;
                result.maxBlur = maxBlur.value;
                result.minBlur = minBlur.value;

                return result;
            }
        }
    }
}