using System;
using UnityEngine;

namespace Sculpting
{
    public class SdfShapeRenderer : ScriptableObject, SdfShapeRenderHandler.ISdfRenderer
    {
        public virtual void Render(Matrix4x4 transform, Color color)
        {

        }

        public virtual Type SdfType()
        {
            return null;
        }
    }
}
