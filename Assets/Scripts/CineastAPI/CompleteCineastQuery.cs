using IO.Swagger.Api;
using IO.Swagger.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cineast_OpenAPI_Implementation
{
    public class CompleteCineastQuery
    {
        public Apiv1Api Api
        {
            get;
            private set;
        }

        public CineastObjectDownloader ObjectDownloader
        {
            get;
            private set;
        }

        public CompleteCineastQuery(string cineastApiUrl)
        {
            Api = new Apiv1Api(new Configuration
            {
                BasePath = cineastApiUrl
            });
            ObjectDownloader = new CineastObjectDownloader();
        }
    }
}
