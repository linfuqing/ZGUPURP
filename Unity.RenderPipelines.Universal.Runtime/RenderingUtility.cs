using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public static class RenderingUtility
    {
        public static RTHandle GetActiveCameraColorAttachment(this UniversalRenderer renderer)
        {
            return renderer.m_ActiveCameraColorAttachment;
        }
    }
}