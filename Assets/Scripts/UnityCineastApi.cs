using Cineast_OpenAPI_Implementation;
using IO.Swagger.Model;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class UnityCineastApi : MonoBehaviour
{
    [System.Serializable]
    private struct TestSettings
    {
        public bool runTest;
        public TextAsset testModel;
        public bool debugLog;
    }
    [SerializeField] private TestSettings testSettings = new TestSettings();

    [SerializeField] private string cineastApiUrl = "http://192.168.56.101:4567/";

    [SerializeField] private string[] queryCategories = { "sphericalharmonicshigh" };

    [System.Serializable]
    public struct ObjectDownloaderSettings
    {
        public bool useCineastServer;
        public string hostBaseUrl;
        public string hostThumbnailsPath;
        public string hostContentPath;
        public bool useDescriptorContentPath;
        public string defaultSuffix;
    }
    [SerializeField]
    private ObjectDownloaderSettings objectDownloaderSettings = new ObjectDownloaderSettings
    {
        useCineastServer = false, //Cineast's thumbnail resolver is currently broken
        hostBaseUrl = "http://192.168.56.101:8000/",
        hostThumbnailsPath = "thumbnails/:o/:s:x",
        hostContentPath = "data/3d/:p",
        useDescriptorContentPath = false,
        defaultSuffix = "jpg"
    };

    [SerializeField] private GameObject[] callbackObjects = new GameObject[0];

    public interface QueryResultCallback
    {
        void OnCineastQueryCompleted(List<QueryResult> results);
    }

    public struct QueryResult
    {
        public double score;
        public MediaSegmentDescriptor segmentDescriptor;
        public MediaObjectDescriptor objectDescriptor;
        public string objModel;
    }

    private class CollectingQueryResultCallback : Complete3DSimilarityQuery.Callback
    {
        private readonly List<QueryResult> results;

        public CollectingQueryResultCallback(List<QueryResult> results)
        {
            this.results = results;
        }

        public void OnFullQueryResult(StringDoublePair entry, MediaSegmentDescriptor segmentDescriptor, MediaObjectDescriptor objectDescriptor, string objModel)
        {
            results.Add(new QueryResult
            {
                score = entry.Value.Value,
                segmentDescriptor = segmentDescriptor,
                objectDescriptor = objectDescriptor,
                objModel = objModel
            });
        }
    }

    private class LoggingWrapper : Complete3DSimilarityQuery.Handler, Complete3DSimilarityQuery.Callback
    {
        private readonly Complete3DSimilarityQuery.Handler handler;
        private readonly Complete3DSimilarityQuery.Callback callback;

        public LoggingWrapper(Complete3DSimilarityQuery.Handler handler, Complete3DSimilarityQuery.Callback callback)
        {
            this.handler = handler;
            this.callback = callback;
        }

        public SimilarityQuery OnStartQuery(SimilarityQuery query)
        {
            Debug.Log("Start 3D Similarity Query Request");
            return handler != null ? handler.OnStartQuery(query) : query;
        }

        public SimilarityQueryResultBatch OnFinishQuery(SimilarityQueryResultBatch result)
        {
            Debug.Log("Finished 3D Similarity Query Request");

            Debug.Log("Results:");
            Debug.Log("");
            return handler != null ? handler.OnFinishQuery(result) : result;
        }

        public IdList OnStartSegmentsByIdQuery(SimilarityQueryResult similarityResult, StringDoublePair entry, IdList idList)
        {
            Debug.Log("---------------------------");
            Debug.Log("Segment ID: " + entry.Key + ", Similarity Score: " + entry.Value);
            return handler != null ? handler.OnStartSegmentsByIdQuery(similarityResult, entry, idList) : idList;
        }

        public MediaSegmentQueryResult OnFinishSegmentsByIdQuery(SimilarityQueryResult similarityResult, StringDoublePair entry, MediaSegmentQueryResult result)
        {
            return handler != null ? handler.OnFinishSegmentsByIdQuery(similarityResult, entry, result) : result;
        }

        public void OnStartObjectByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, MediaSegmentDescriptor descriptor)
        {
            Debug.Log("Object ID: " + descriptor.ObjectId);
            if (handler != null)
            {
                handler.OnStartObjectByIdQuery(queryResult, entry, descriptor);
            }
        }

        public MediaObjectQueryResult OnFinishObjectByIdQuery(SimilarityQueryResult queryResult, StringDoublePair entry, MediaSegmentDescriptor descriptor, MediaObjectQueryResult result)
        {
            return handler != null ? handler.OnFinishObjectByIdQuery(queryResult, entry, descriptor, result) : result;
        }

        public void OnFullQueryResult(StringDoublePair entry, MediaSegmentDescriptor segmentDescriptor, MediaObjectDescriptor objectDescriptor, string objModel)
        {
            Debug.Log("Downloaded Object: ");

            var lines = objModel.Split('\n');
            int maxLines = Mathf.Min(lines.Length, 8);
            for (int i = 0; i < maxLines; i++)
            {
                Debug.Log(lines[i]);
            }
            Debug.Log("...");
            Debug.Log("---------------------------");
            Debug.Log("");

            if (callback != null)
            {
                callback.OnFullQueryResult(entry, segmentDescriptor, objectDescriptor, objModel);
            }
        }
    }

    void Update()
    {
        if (testSettings.runTest)
        {
            testSettings.runTest = false;

            using (Stream stream = new MemoryStream(testSettings.testModel.bytes))
            {
                StartQuery(ObjToJsonConverter.Convert(stream));
            }
        }
    }

    public void StartQuery(string modelJson)
    {
        if (testSettings.debugLog)
        {
            Debug.Log("Start Similarity Query");
        }

        var query = new Complete3DSimilarityQuery(cineastApiUrl);
        var downloader = query.ObjectDownloader;

        downloader.UseCineastServer = objectDownloaderSettings.useCineastServer;
        downloader.HostBaseUrl = objectDownloaderSettings.hostBaseUrl;
        downloader.HostThumbnailsPath = objectDownloaderSettings.hostThumbnailsPath;
        downloader.HostContentPath = objectDownloaderSettings.hostContentPath;
        downloader.UseDescriptorContentPath = objectDownloaderSettings.useDescriptorContentPath;
        downloader.DefaultSuffix = objectDownloaderSettings.defaultSuffix;

        var categories = new List<string>(queryCategories);

        StartCoroutine(CreateQueryCoroutine(query, categories, modelJson, results =>
        {
            foreach (GameObject obj in callbackObjects)
            {
                QueryResultCallback[] callbacks = obj.GetComponents<QueryResultCallback>();
                foreach (QueryResultCallback callback in callbacks)
                {
                    if (callback != null)
                    {
                        callback.OnCineastQueryCompleted(results);
                    }
                }
            }
        }, testSettings.debugLog));
    }

    public delegate void QueryResultCallbackDelegate(List<QueryResult> results);
    public static IEnumerator CreateQueryCoroutine(Complete3DSimilarityQuery query, List<string> categories, string modelJson, QueryResultCallbackDelegate callback, bool log = false)
    {
        var results = new List<QueryResult>();

        //This callback just puts all results in the results list
        Complete3DSimilarityQuery.Callback queryCallback = new CollectingQueryResultCallback(results);
        if (log)
        {
            queryCallback = new LoggingWrapper(null, queryCallback);
        }

        //Create query task
        var queryTask = query.PerformAsync(categories, modelJson, queryCallback, null);

        //Run query task in another thread
        Task.Run(async () => await queryTask);

        //Check if task is completed, otherwise make the coroutine wait
        while (!queryTask.IsCompleted)
        {
            yield return null;
        }

        //Call callback with query results
        callback(results);
    }
}
