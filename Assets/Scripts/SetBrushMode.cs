using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BrushType
{
    Sphere, Cube, Pyramid, Cylinder
}

public class SetBrushMode : MonoBehaviour
{
    [SerializeField] private BrushType brush = BrushType.Cube;

    [SerializeField] private VRGuiPointer guiPointer = null;

    public void OnGuiSetBrushMode()
    {
        guiPointer.guiManager.SetBrushMode(brush);
    }
}
