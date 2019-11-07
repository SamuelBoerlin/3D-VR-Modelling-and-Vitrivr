using System;
using Unity.Collections;
using UnityEngine;
using VoxelPolygonizer.CMS;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/PolygonizationProperties", order = 1)]
public class PolygonizationProperties : ScriptableObject, ICMSProperties
{
    public int airMaterial = 0;

    [Serializable]
    public class SharpFeatureProperties2D
    {
        public float maxFeatureTheta = 0.6f;
        public float maxFeaturePhi = 0.6f;
        public float maxTransitionTheta = 0.99f;
    }
    public SharpFeatureProperties2D _2DSharpFeatureProperties = new SharpFeatureProperties2D();
    
    [Serializable]
    public class SharpFeatureProperties3D
    {
        public float maxFeatureTheta = 0.6f;
        public float minCornerPhi = 0.7f;
        public float minTransitionTheta = -0.999f;
    }
    public SharpFeatureProperties3D _3DSharpFeatureProperties = new SharpFeatureProperties3D();

    public bool IsSolid(int material)
    {
        return material != airMaterial;
    }

    public bool IsSharp2DFeature(float theta, float phi, NativeArray<int> materials)
    {
        return theta < _2DSharpFeatureProperties.maxFeatureTheta && phi < _2DSharpFeatureProperties.maxFeaturePhi;
    }

    public bool IsSharp3DFeature(float theta, ComponentMaterials materials)
    {
        return theta < _3DSharpFeatureProperties.maxFeatureTheta;
    }

    public bool IsSharp3DCornerFeature(float phi, ComponentMaterials materials)
    {
        return phi > _3DSharpFeatureProperties.minCornerPhi;
    }

    public bool IsValid2DTransitionFeature(float minTheta, float maxTheta, NativeArray<int> materials)
    {
        //min theta, max theta??
        return /*maxTheta < 0.9999f*/ minTheta < _2DSharpFeatureProperties.maxTransitionTheta;
    }

    public bool IsValid3DTransitionFeature(float theta, ComponentMaterials materials)
    {
        return theta > _3DSharpFeatureProperties.minTransitionTheta;
    }
}