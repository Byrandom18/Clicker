using UnityEngine;

namespace Clicker
{
    public static class VfxPresentation
    {
        const float TowardCamera = 0.45f;
        const float MinMaxParticleSize = 3f;

        public static Vector3 Place(Vector3 worldPos)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return worldPos;

            Vector3 toCam = cam.transform.position - worldPos;
            toCam.x = 0f;
            toCam.y = 0f;
            if (toCam.sqrMagnitude < 1e-8f)
                return worldPos;
            return worldPos + toCam.normalized * TowardCamera;
        }

        public static void Prepare(GameObject go, string sortingLayer, int sortingOrder)
        {
            if (go == null)
                return;

            var renderers = go.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;

                renderer.sortingLayerName = sortingLayer;
                renderer.sortingOrder = sortingOrder;
                renderer.enableGPUInstancing = false;
                renderer.maxParticleSize = Mathf.Max(renderer.maxParticleSize, MinMaxParticleSize);
            }
        }
    }
}
