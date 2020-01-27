using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetBrushMaterial : MonoBehaviour
{
    [SerializeField] private int material = 1;

    [SerializeField] private VRGuiPointer guiPointer = null;

    public void OnGuiSetBrushMaterial()
    {
        guiPointer.guiManager.SetBrushMaterial(material);
    }
}
