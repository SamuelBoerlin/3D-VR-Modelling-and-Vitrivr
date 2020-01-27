using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum OperationType
{
    Union, Difference, Replace
}

public class SetOperationMode : MonoBehaviour
{
    [SerializeField] private OperationType operation = OperationType.Union;

    [SerializeField] private VRGuiPointer guiPointer = null;

    public void OnGuiSetOperationMode()
    {
        guiPointer.guiManager.SetOperationMode(operation);
    }
}
