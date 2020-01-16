using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(MeshFilter))]
public class QueryResultObject : MonoBehaviour
{
    [SerializeField] private Text display;

    public UnityCineastApi.QueryResult QueryResult
    {
        get;
        private set;
    }

    public void SetQueryData(UnityCineastApi.QueryResult data)
    {
        QueryResult = data;

        if (display != null)
        {
            display.text = data.objectDescriptor.Name + "\nScore: " + data.score.ToString("0.00");
        }
    }
}
