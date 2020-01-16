using IO.Swagger.Api;
using IO.Swagger.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Cineast_OpenAPI_Implementation
{
    public class CineastObjectDownloader
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public bool UseCineastServer { get; set; } = true;

        public string HostBaseUrl { get; set; }
        public string HostThumbnailsPath { get; set; } = "thumbnails/:o/:s:x";
        public string HostContentPath { get; set; } = "objects/:p";
        public bool UseDescriptorContentPath { get; set; } = false;

        private Dictionary<MediaObjectDescriptor.MediatypeEnum, string> suffices = new Dictionary<MediaObjectDescriptor.MediatypeEnum, string>()
        {
            { MediaObjectDescriptor.MediatypeEnum.IMAGE, "png" },
            { MediaObjectDescriptor.MediatypeEnum.VIDEO, "png" }
        };
        public string DefaultSuffix = "jpg";

        public void RegisterSuffix(MediaObjectDescriptor.MediatypeEnum type, string suffix)
        {
            suffices[type] = suffix;
        }

        public async Task<Stream> RequestThumbnailAsync(Apiv1Api api, MediaObjectDescriptor objectDescriptor, MediaSegmentDescriptor segmentDescriptor)
        {
            if (UseCineastServer)
            {
                return await api.ApiV1GetThumbnailsIdGetAsync(objectDescriptor.ObjectId);
            }
            if (HostBaseUrl == null)
            {
                throw new InvalidOperationException("HostBaseUrl is null");
            }
            return await httpClient.GetStreamAsync(HostBaseUrl + CompletePath(HostThumbnailsPath, objectDescriptor, segmentDescriptor));
        }

        public async Task<Stream> RequestContentAsync(Apiv1Api api, MediaObjectDescriptor objectDescriptor, MediaSegmentDescriptor segmentDescriptor)
        {
            if (UseCineastServer)
            {
                return await api.ApiV1GetObjectsIdGetAsync(objectDescriptor.ObjectId);
            }
            if (UseDescriptorContentPath)
            {
                return await httpClient.GetStreamAsync(HostBaseUrl + objectDescriptor.ContentURL);
            }
            if (HostBaseUrl == null)
            {
                throw new InvalidOperationException("HostBaseUrl is null");
            }
            return await httpClient.GetStreamAsync(HostBaseUrl + CompletePath(HostContentPath, objectDescriptor, segmentDescriptor));
        }

        private string CompletePath(string path, MediaObjectDescriptor objectDescriptor, MediaSegmentDescriptor segmentDescriptor)
        {
            string suffix = DefaultSuffix;
            if (objectDescriptor.Mediatype.HasValue)
            {
                if (!suffices.TryGetValue(objectDescriptor.Mediatype.Value, out suffix)) suffix = DefaultSuffix;
            }

            //Same as in vitrivr-ng: https://github.com/vitrivr/vitrivr-ng/blob/master/src/app/core/basics/resolver.service.ts
            path = path.Replace(":o", objectDescriptor.ObjectId);
            path = path.Replace(":n", objectDescriptor.Name);
            path = path.Replace(":p", objectDescriptor.Path);
            path = path.Replace(":t", Enum.GetName(typeof(MediaObjectDescriptor.MediatypeEnum), objectDescriptor.Mediatype).ToLower());
            path = path.Replace(":T", Enum.GetName(typeof(MediaObjectDescriptor.MediatypeEnum), objectDescriptor.Mediatype).ToUpper());
            path = path.Replace(":s", segmentDescriptor.SegmentId);
            path = path.Replace(":x", "." + suffix);

            return path;
        }
    }
}
