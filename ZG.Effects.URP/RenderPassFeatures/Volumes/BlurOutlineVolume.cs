using UnityEngine;
using UnityEngine.Rendering;

namespace ZG
{
    public class BlurOutlineVolume : VolumeComponent, IRenderBlurOutline
    {
        public static readonly int SolidColor = Shader.PropertyToID("_SolidColor");

        public BoolParameter depthSort = new BoolParameter(true);

        public LayerMaskParameter layerMask = new LayerMaskParameter(~0);

        public ColorParameter color = new ColorParameter(Color.white);

        public IntParameter blurIterCount = new MinIntParameter(1, 0);
        public IntParameter downSample = new MinIntParameter(1, 0);
        public FloatParameter strength = new MinFloatParameter(1.0f, 0);
        public Vector2Parameter blurScale = new Vector2Parameter(Vector2.one);

        public bool isVail => active;

        public bool needDepthSort => depthSort.value;

        public RenderBlurOutlineData data
        {
            get
            {
                RenderBlurOutlineData result;
                result.blurIterCount = blurIterCount.value;
                result.downSample = downSample.value;
                result.strength = strength.value;
                result.blurScale = blurScale.value;

                return result;
            }
        }

        public void Draw(
            Material[] silhouetteMaterials,
            CommandBuffer cmd, 
            ref ScriptableRenderContext context, 
            ref CullingResults cullingResults, 
            ref DrawingSettings drawingSettings)
        {
            cmd.SetGlobalColor(SolidColor, color.value);

            drawingSettings.overrideMaterial = silhouetteMaterials[0];

            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, layerMask.value);

            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            IRenderBlurOutline.instance = this;
        }

        protected override void OnDisable()
        {
            if (IRenderBlurOutline.instance == (IRenderBlurOutline)this)
                IRenderBlurOutline.instance = null;

            base.OnDisable();
        }
    }
}