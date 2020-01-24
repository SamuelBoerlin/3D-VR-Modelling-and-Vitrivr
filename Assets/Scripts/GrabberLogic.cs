using Sculpting;
using UnityEngine;

public class GrabberLogic : MonoBehaviour
{
    [SerializeField] private string triggerInput = "";

    [SerializeField] private string scaleInput = "";
    [SerializeField] private Transform scaleHandTransform = null;
    [SerializeField] [Range(0.1f, 10.0f)] private float minScale = 0.1f;
    [SerializeField] [Range(0.1f, 10.0f)] private float maxScale = 10.0f;
    [SerializeField] private float scaleStrength = 0.1f;

    [SerializeField] private float grabDistance = 5.0f;

    private Vector3 lastGrabberPos;

    private bool isScaling = false;
    private Vector3 initialScalePosition;
    private float initialScale;
    private float initialScaleDistance;

    private void Update()
    {
        if (Input.GetAxis(triggerInput) > 0.5)
        {
            GameObject sculpture = GameObject.FindGameObjectWithTag("Sculpture");
            if (sculpture != null)
            {
                Sculpture script = sculpture.GetComponent<Sculpture>();
                if (script != null)
                {
                    if (script.RayCast(transform.position, transform.rotation * new Vector3(0, 0, 1), grabDistance, out Sculpture.RayCastResult result))
                    {
                        sculpture.transform.SetParent(this.gameObject.transform);
                        Rigidbody rb = sculpture.GetComponent<Rigidbody>();
                        if (rb != null)
                            rb.isKinematic = true;
                        return;
                    }
                }
            }

            QueryResultObject[] queryResults = FindObjectsOfType<QueryResultObject>();
            foreach (var queryResultObject in queryResults)
            {
                var collider = queryResultObject.GetComponent<Collider>();
                Debug.Log(queryResultObject);
                if (collider != null)
                {
                    Debug.Log((collider.ClosestPoint(transform.position) - transform.position).magnitude);
                    if ((collider.ClosestPoint(transform.position) - transform.position).magnitude < 0.1f)
                    {
                        queryResultObject.transform.SetParent(this.gameObject.transform);
                        Rigidbody rb = queryResultObject.GetComponent<Rigidbody>();
                        if (rb != null)
                            rb.isKinematic = true;
                        return;
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
        var transform = this.gameObject.GetComponent<Transform>();

        Vector3 velocity = (transform.position - lastGrabberPos) / Time.fixedDeltaTime;

        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);

            if (Input.GetAxis(triggerInput) < 0.5)
            {
                child.SetParent(null, true);
                Rigidbody rb = child.gameObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.velocity = velocity;
                }
            }

            if (Input.GetButton(scaleInput))
            {
                float handDistance = Vector3.Distance(transform.position, scaleHandTransform.position);
                Debug.Log(isScaling);
                if (!isScaling)
                {
                    initialScalePosition = child.transform.localPosition;
                    initialScale = child.transform.localScale.x;
                    initialScaleDistance = handDistance;
                    isScaling = true;

                    Debug.Log("initPos: " + initialScalePosition);
                }

                float dst = (handDistance - initialScaleDistance);
                float newScale = initialScale + dst * scaleStrength;
                //Debug.Log("Scale: " + newScale);
                //Matrix4x4 scaling = Matrix4x4.TRS(child.transform.localPosition, Quaternion.identity, Vector3.one * newScale);

                Debug.Log("NEW SCALE: " + newScale);

                child.transform.localScale = Vector3.one * newScale;
                child.transform.localPosition = initialScalePosition + initialScalePosition * (newScale - initialScale) / initialScale; // * newScale - initialScalePosition * newScale;


            }
            else
            {
                isScaling = false;
            }
        }

        this.lastGrabberPos = transform.position;
    }
}
