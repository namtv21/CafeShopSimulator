using UnityEngine;

namespace CafeShopSimulator
{
    /// <summary>Keeps authored map, hotspots and HUD in the same 16:9 coordinate system.</summary>
    [ExecuteAlways]
    public sealed class ReferenceFrame : MonoBehaviour
    {
        private void OnEnable() { Canvas.preWillRenderCanvases += Fit; Fit(); }
        private void OnDisable() => Canvas.preWillRenderCanvases -= Fit;
        private void LateUpdate() => Fit();
        private void Fit()
        {
            var parent = transform.parent as RectTransform;
            if (parent == null) return;
            float scale = Mathf.Min(parent.rect.width / 1920f, parent.rect.height / 1080f);
            transform.localScale = Vector3.one * Mathf.Max(.001f, scale);
        }
    }
}
