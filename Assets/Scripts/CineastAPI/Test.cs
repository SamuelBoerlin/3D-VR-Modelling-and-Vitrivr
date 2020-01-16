using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Cineast_OpenAPI_Implementation
{
    class Test
    {
        static void Main(string[] args)
        {
            Task.Run(async () => await Run()).GetAwaiter().GetResult();
        }

        private static async Task Run()
        {
            Console.WriteLine("Start API Test");

            var cineastApiUrl = "http://192.168.56.101:4567/";
            var cineastFileUrl = "http://192.168.56.101:8000/";

            var query = new Complete3DSimilarityQuery(cineastApiUrl);
            var downloader = query.ObjectDownloader;
            downloader.HostBaseUrl = cineastFileUrl;
            downloader.HostContentPath = "data/3d/:p";
            downloader.HostThumbnailsPath = "thumbnails/:o/:s:x";
            downloader.UseCineastServer = false; //Cineast's thumbnail resolver is currently broken

            var categories = new List<string>
            {
                "sphericalharmonicshigh"
            };

            var testModelJson = "";
            using (FileStream fs = File.OpenRead("../../cube.obj"))
            {
                testModelJson = ObjToJsonConverter.Convert(fs);
            }

            var handler = new Complete3DSimilarityQuery.LoggingHandler();

            await query.PerformAsync(categories, testModelJson, handler, handler);

            Console.ReadLine();
        }
    }
}
