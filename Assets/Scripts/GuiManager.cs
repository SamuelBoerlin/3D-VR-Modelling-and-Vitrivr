using Sculpting;
using UnityEngine;

[RequireComponent(typeof(SdfShapeRenderHandler))]
public class GuiManager : MonoBehaviour
{
    [SerializeField] private string guiInput = "";

    [SerializeField] private string triggerInput = "";

    [SerializeField] private string smearToggleInput = "";

    [SerializeField] private GameObject guiPrefab = null;

    [SerializeField] private GameObject laserPrefab = null;

    [SerializeField] private SpriteRenderer brushRenderer = null;

    [SerializeField] private Transform pointerHandTransform = null;

    [SerializeField] private Camera camera = null;

    private GameObject openGui = null;
    private GameObject guiLaser = null;

    private bool wasTriggerDown = false;
    private bool wasSmearToggleDown = false;

    private bool _isSmearMode = false;
    private bool IsSmearMode
    {
        get
        {
            return _isSmearMode;
        }
        set
        {
            _isSmearMode = value;
            brushRenderer.GetComponent<SpriteRenderer>().enabled = _isSmearMode;
        }
    }

    private Vector3 lastSmearPos = Vector3.zero;

    private OperationType opType = OperationType.Union;
    private BrushType brushType = BrushType.Cube;

    private float startScale;

    private SdfShapeRenderHandler previewRenderer;

    private void Start()
    {
        startScale = GameObject.FindGameObjectWithTag("Sculpture").transform.localScale.x;
        previewRenderer = GetComponent<SdfShapeRenderHandler>();

        brushRenderer.GetComponent<SpriteRenderer>().enabled = IsSmearMode;
    }

    void Update()
    {
        if (Input.GetButton(guiInput))
        {
            SpawnGui();
        }
        else
        {
            RemoveGui();
        }

        bool isSmearToggleDown = Input.GetAxis(smearToggleInput) > 0.5f;
        if (!wasSmearToggleDown && isSmearToggleDown)
        {
            IsSmearMode = !IsSmearMode;
        }
        wasSmearToggleDown = isSmearToggleDown;

        var brushPosition = pointerHandTransform.position + pointerHandTransform.rotation * new Vector3(0, 0, 0.3f);

        GameObject sculpture = GameObject.FindGameObjectWithTag("Sculpture");
        if (sculpture != null)
        {
            var previewColor = Color.blue;
            if (opType == OperationType.Difference)
            {
                previewColor = Color.red;
            }

            Sculpture script = sculpture.GetComponent<Sculpture>();
            if (brushType == BrushType.Cube)
            {
                var shape = new ScaleSDF<BoxSDF>(1.0f / (sculpture.transform.localScale.x / startScale) * sculpture.transform.lossyScale.x, new BoxSDF(8.0f));
                previewRenderer.Render(brushPosition, pointerHandTransform.rotation, shape, previewColor);
            }
            else if (brushType == BrushType.Cylinder)
            {
                var shape = new ScaleSDF<CylinderSDF>(1.0f / (sculpture.transform.localScale.x / startScale) * sculpture.transform.lossyScale.x, new CylinderSDF(8.0f, 8.0f));
                previewRenderer.Render(brushPosition, pointerHandTransform.rotation, shape, previewColor);
            }
            else if (brushType == BrushType.Pyramid)
            {
                var shape = new ScaleSDF<PyramidSDF>(1.0f / (sculpture.transform.localScale.x / startScale) * sculpture.transform.lossyScale.x, new PyramidSDF(16.0f, 16.0f));
                previewRenderer.Render(brushPosition, pointerHandTransform.rotation, shape, previewColor);
            }
            else
            {
                var shape = new ScaleSDF<SphereSDF>(1.0f / (sculpture.transform.localScale.x / startScale) * sculpture.transform.lossyScale.x, new SphereSDF(8.0f));
                previewRenderer.Render(brushPosition, pointerHandTransform.rotation, shape, previewColor);
            }
        }

        bool isTriggerDown = Input.GetAxis(triggerInput) > 0.5f;
        if ((!wasTriggerDown && isTriggerDown) || (IsSmearMode && isTriggerDown && (lastSmearPos - pointerHandTransform.position).magnitude > 0.01f))
        {
            lastSmearPos = pointerHandTransform.position;

            if (openGui == null)
            {
                if (sculpture != null)
                {
                    Sculpture script = sculpture.GetComponent<Sculpture>();

                    if (script != null)
                    {
                        int material;
                        if (opType == OperationType.Union)
                        {
                            material = 1;
                        }
                        else
                        {
                            material = 0;
                        }

                        if (brushType == BrushType.Cube)
                        {
                            var shape = new ScaleSDF<BoxSDF>(1.0f / (sculpture.transform.localScale.x / startScale), new BoxSDF(8.0f));
                            script.ApplySdf(brushPosition, pointerHandTransform.rotation, shape, material, false);
                        }
                        else if (brushType == BrushType.Cylinder)
                        {
                            var shape = new ScaleSDF<CylinderSDF>(1.0f / (sculpture.transform.localScale.x / startScale), new CylinderSDF(8.0f, 8.0f));
                            script.ApplySdf(brushPosition, pointerHandTransform.rotation, shape, material, false);
                        }
                        else if (brushType == BrushType.Pyramid)
                        {
                            var shape = new ScaleSDF<PyramidSDF>(1.0f / (sculpture.transform.localScale.x / startScale), new PyramidSDF(16.0f, 16.0f));
                            script.ApplySdf(brushPosition, pointerHandTransform.rotation, shape, material, false);
                        }
                        else
                        {
                            var shape = new ScaleSDF<SphereSDF>(1.0f / (sculpture.transform.localScale.x / startScale), new SphereSDF(8.0f));
                            script.ApplySdf(brushPosition, pointerHandTransform.rotation, shape, material, false);
                        }
                    }
                }
            }
        }

        wasTriggerDown = isTriggerDown;
    }

    private void SpawnGui()
    {
        if (openGui == null)
        {
            GameObject gui = Instantiate(guiPrefab);

            VRGuiPointer pointer = gui.GetComponent<VRGuiPointer>();

            pointer.handTransform = pointerHandTransform;
            pointer.camera = camera;
            pointer.guiManager = this;

            gui.transform.SetParent(gameObject.transform, false);
            openGui = gui;

            guiLaser = Instantiate(laserPrefab);
            guiLaser.transform.SetParent(pointerHandTransform, false);
        }
    }

    private void RemoveGui()
    {
        if (openGui != null)
        {
            Destroy(openGui);
            openGui = null;

            Destroy(guiLaser);
            guiLaser = null;
        }
    }

    public void SetOperationMode(OperationType type)
    {
        Debug.Log(type);
        opType = type;
    }

    public void SetBrushMode(BrushType type)
    {
        Debug.Log(type);
        brushType = type;
    }
}
