using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sculpting
{
    [RequireComponent(typeof(MeshRenderer))]
    public class SdfShapeRenderHandler : MonoBehaviour
    {
        public interface ISdfRenderer
        {
            Type SdfType();

            void Render(Matrix4x4 transform, Color color);
        }

        private Dictionary<Type, ISdfRenderer> registry = new Dictionary<Type, ISdfRenderer>();

        [SerializeField]
        private SdfShapeRenderer[] renderers = new SdfShapeRenderer[0];

        private void Start()
        {
            registry.Clear();
            foreach (ISdfRenderer renderer in renderers)
            {
                registry[renderer.SdfType()] = renderer;
            }
        }

        public void Render(Vector3 position, Quaternion rotation, ISdf sdf, Color color)
        {
            var transform = Matrix4x4.TRS(position, rotation, Vector3.one);
            var renderingTransform = Matrix4x4.identity;

            var transforms = new List<Matrix4x4>();

            int depth = 0;

            var rendering = sdf;
            var cur = sdf;
            while (cur != null)
            {
                rendering = cur;

                var curTransform = cur.RenderingTransform();
                if (curTransform.HasValue)
                {
                    transforms.Add(curTransform.Value);
                }

                cur = cur.RenderChild();

                if (++depth > 30)
                {
                    Debug.Log("Max SDF rendering depth reached");
                    break;
                }
            }

            if (registry.TryGetValue(rendering.GetType(), out ISdfRenderer renderer))
            {
                transforms.Reverse();

                foreach (var matrix in transforms)
                {
                    renderingTransform = matrix * renderingTransform;
                }

                renderingTransform = transform * renderingTransform;

                renderer.Render(renderingTransform, color);
            }
        }
    }
}
