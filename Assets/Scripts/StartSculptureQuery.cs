using Sculpting;
using UnityEngine;

public class StartSculptureQuery : MonoBehaviour
{
    [SerializeField] private VRGuiPointer guiPointer = null;

    public void OnStartSculptureQuery()
    {
        guiPointer.guiManager.StartSculptureQuery();
    }
}
