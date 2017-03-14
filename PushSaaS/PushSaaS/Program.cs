using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using PowerBIExtensionMethods;


namespace PushSaaS
{
    class Program
    {
        private static string token = String.Empty;
        private static AuthenticationContext authContext = null;
        private static string authority = Properties.Settings.Default.AADAuthorityUri;
        private static string clientID = Properties.Settings.Default.ClientID;
        private static string resourceUri = Properties.Settings.Default.PowerBiAPI;
        private static string datasetsUri = Properties.Settings.Default.PowerBiDataset;

        //private static string redirectUri = "https://login.live.com/oauth20_desktop.srf";

        private static string pbiUsername = null;
        private static string pbiPassward = null;
        private static string datasetName = null;
        private static string tableName = null;

        static void Main(string[] args)
        {
            Console.WriteLine("SaaS API Sample");


            bool exit = false;
            while (!exit)
            {
                Console.WriteLine();
                Console.WriteLine("=================================================================");
                Console.WriteLine("1. Create new Dataset");
                Console.WriteLine("2. Import PBIX report");
                Console.WriteLine("3. Delete Dataset");
                Console.WriteLine("4. Get Datasets");
                Console.WriteLine("5. Get Tables in Dataset");
                Console.WriteLine("6. Add rows to table in DS");
                Console.WriteLine("7. Delete rows of table in DS");
                Console.WriteLine("8. Update Table Scheme");
                Console.WriteLine("9. Get Sequence Number");
                Console.WriteLine("10. Get Reports");
                Console.WriteLine("11. Get Imports");
                Console.WriteLine("12. Get Import By GUID");
                Console.WriteLine("13. Get Dashboards");
                Console.WriteLine("14. Get Tiles");
                Console.WriteLine("15. Get Groups");
                Console.WriteLine("0. Exit");
                Console.WriteLine();

                Console.WriteLine("Enter Key:");
                string key = Console.ReadLine();
                Console.WriteLine();

                switch (key)
                {
                    case "1":
                        Console.WriteLine("Dataset Name is required, Enter value: ");
                        datasetName = Console.ReadLine();
                        CreateDataset(datasetName);
                        break;

                    case "2":
                        importSample();
                        break;

                    case "3":
                        DeleteDataset();
                        break;

                    case "4":
                        Datasets datasets = GetDatasets();
                        if (datasets != null)
                        {
                            PrintDatasets(datasets.value);
                        }
                        break;

                    case "5":
                        Console.WriteLine("Dataset Name is required, Enter value: ");
                        datasetName = Console.ReadLine();
                        Tables tables = GetTables(datasetName);
                        if (tables != null)
                        {
                            PrintTables(tables.value);
                        }
                        break;

                    case "6":
                        updateDatasetAndTableName();
                        AddRows(datasetName, tableName);
                        break;

                    case "7":
                        updateDatasetAndTableName();
                        DeleteRows(datasetName, tableName);
                        break;

                    case "8":
                        updateDatasetAndTableName();
                        UpdateTableSchema(datasetName, tableName);
                        break;

                    case "9":
                        updateDatasetAndTableName();
                        Console.WriteLine("Sequence Number is " + GetSequenceNumber(datasetName, tableName));
                        break;

                    case "10":
                        Reports reports = GetReports();
                        if (reports != null)
                        {
                            PrintReports(reports.value);
                        }
                        break;

                    case "11":
                        Imports imports = GetImports();
                        if (imports != null)
                        {
                            PrintImports(imports.value);
                        }
                        break;

                    case "12":
                        Console.WriteLine("import GUID is required, Enter value: ");
                        string importGUID = Console.ReadLine();
                        import import = GetImportByGUID(importGUID);
                        if (import != null)
                        {
                            import[] impArray = { import };
                            PrintImports(impArray);
                        }
                        break;

                    case "13":
                        Dashboards dashboards = GetDashboards();
                        if (dashboards != null)
                        {
                            PrintDashboards(dashboards.value);
                        }
                        break;

                    case "14":
                        Console.WriteLine("Dashboard ID is required, Enter value: ");
                        string dashboardId = Console.ReadLine();
                        Tiles tiles = GetTiles(dashboardId);
                        if (tiles != null)
                        {
                            PrintTiles(tiles.value);
                        }
                        break;

                    case "15":
                        Groups groups = GetGroups();
                        if (groups != null)
                        {
                            PrintGroups(groups.value);
                        }
                        break;

                    case "0":
                        exit = true;
                        break;

                    default:
                        Console.WriteLine("Invalid key...");
                        break;
                }
            }
        }

        private static void CreateDataset(string datasetName)
        {          
            try
            {
                HttpWebRequest request = CreateRequest(String.Format("{0}/datasets", datasetsUri), "POST", AccessToken());
                dataset ds = GetDatasets().value.GetDataset(datasetName);
                if (ds == null)
                {
                    Console.WriteLine(PostRequest(request, new Product().ToDatasetJson(datasetName)));
                }
                else
                {
                    Console.WriteLine("Dataset exists");
                }
            }
            catch (Exception ex)
            {
                handleEX(ex);
            }
        }

        private static void importSample()
        {
            DirectoryInfo di = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            string pbixPath = string.Format(@"{0}\AdventureWorks.pbix", di.Parent.Parent.FullName);
            string datasetDisplayName = "AdventureWorks";

            string importResponse = Import(string.Format("{0}/imports?datasetDisplayName={1}", datasetsUri, datasetDisplayName), pbixPath);
            string importDatasetId = GetDatasets().value.GetDataset(datasetDisplayName).Id;
            Console.WriteLine(string.Format("Imported: {0}. Dataset ID: {1}", datasetDisplayName, importDatasetId));
        }

        private static string Import(string url, string fileName)
        {
            Console.WriteLine("Start importing...");

            string responseStatusCode = string.Empty;

            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";
            request.KeepAlive = true;
            request.Headers.Add("Authorization", String.Format("Bearer {0}", AccessToken()));

            using (Stream rs = request.GetRequestStream())
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);

                string headerTemplate = "Content-Disposition: form-data; filename=\"{0}\"\r\nContent-Type: application / octet - stream\r\n\r\n";
                string header = string.Format(headerTemplate, fileName);
                byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                rs.Write(headerbytes, 0, headerbytes.Length);

                using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = 0;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        rs.Write(buffer, 0, bytesRead);
                    }
                }
                byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                rs.Write(trailer, 0, trailer.Length);
            }
            using (HttpWebResponse response = request.GetResponse() as System.Net.HttpWebResponse)
            {
                responseStatusCode = response.StatusCode.ToString();
            }
            return responseStatusCode;
        }

        private static void DeleteDataset()
        {
            try
            {
                Console.WriteLine("Dataset Name is required, Enter value: ");
                string datasetName = Console.ReadLine();

                dataset ds = GetDatasets().value.GetDataset(datasetName);
                if (ds == null)
                {
                    Console.WriteLine("No Dataset with that name");
                }
                else
                {
                    string datasetId = ds.Id;
                    HttpWebRequest request = CreateRequest(String.Format("{0}/datasets/{1}", datasetsUri, datasetId), "DELETE", AccessToken());
                    request.ContentLength = 0;
                    Console.WriteLine(GetResponse(request));
                }
            }
            catch (Exception ex)
            {
                handleEX(ex);
            }
        }

        private static Datasets GetDatasets()
        {
            Datasets response = null;
            try
            {
                HttpWebRequest request = CreateRequest(String.Format("{0}/datasets", datasetsUri), "GET", AccessToken());
                string responseContent = GetResponse(request);
                JavaScriptSerializer json = new JavaScriptSerializer();
                response = (Datasets)json.Deserialize(responseContent, typeof(Datasets));
            }
            catch (Exception ex)
            {
                handleEX(ex);
            }
            return response;
        }

        private static Tables GetTables(string datasetName)
        {
            Tables response = null;
            try
            {
                string datasetId = getDatasetId(datasetName);
                if (datasetId != null)
                {
                    HttpWebRequest request = CreateRequest(String.Format("{0}/datasets/{1}/tables", datasetsUri, datasetId), "GET", AccessToken());
                    string responseContent = GetResponse(request);
                    JavaScriptSerializer json = new JavaScriptSerializer();
                    response = (Tables)json.Deserialize(responseContent, typeof(Tables));
                }
            }
            catch (Exception ex)
            {
                handleEX(ex);
            }
            return response;
        }

        private static Reports GetReports()
        {
            Reports response = null;
            try
            {
                HttpWebRequest request = CreateRequest(String.Format("{0}/reports", datasetsUri), "GET", AccessToken());
                string responseContent = GetResponse(request);
                JavaScriptSerializer json = new JavaScriptSerializer();
                response = (Reports)json.Deserialize(responseContent, typeof(Reports));
            }
            catch (Exception ex)
            {
                handleEX(ex);
            }
            return response;
        }

        static Imports GetImports()
        {
            Imports response = null;
            try
            {
                HttpWebRequest request = CreateRequest(String.Format("{0}/imports", datasetsUri), "GET", AccessToken());
                string responseContent = GetResponse(request);
                JavaScriptSerializer json = new JavaScriptSerializer();
                response = (Imports)json.Deserialize(responseContent, typeof(Imports));
            }
            catch (Exception ex)
            {
                handleEX(ex);
            }
            return response;
        }

        private static import GetImportByGUID(string importGUID)
        {
            import response = null;
            try
            {
                HttpWebRequest request = CreateRequest(String.Format("{0}/imports/{1}", datasetsUri, importGUID), "GET", AccessToken());
                string responseContent = GetResponse(request);
                JavaScriptSerializer json = new JavaScriptSerializer();
                response = (import)json.Deserialize(responseContent, typeof(import));
            }
            catch (Exception ex)
            {
                handleEX(ex);
            }
            return response;
        }

        private static Dashboards GetDashboards()
        {
            Dashboards response = null;
            try
            {
                HttpWebRequest request = CreateRequest(String.Format("{0}/dashboards", datasetsUri), "GET", AccessToken());
                string responseContent = GetResponse(request);
                JavaScriptSerializer json = new JavaScriptSerializer();
                response = (Dashboards)json.Deserialize(responseContent, typeof(Dashboards));
            }
            catch (Exception ex)
            {
                handleEX(ex);
            }
            return response;
        }

        private static Tiles GetTiles(string dashboardId)
        {
            if (!isValidDashboard(dashboardId))
            {
                return null;
            }
            Tiles response = null;
            try
            {
                HttpWebRequest request = CreateRequest(String.Format("{0}/dashboards/{1}/tiles", datasetsUri, dashboardId), "GET", AccessToken());
                string responseContent = GetResponse(request);
                JavaScriptSerializer json = new JavaScriptSerializer();
                response = (Tiles)json.Deserialize(responseContent, typeof(Tiles));
            }
            catch (Exception ex)
            {
                handleEX(ex);
            }
            return response;
        }

        private static Groups GetGroups()
        {
            Groups response = null;
            try
            {
                HttpWebRequest request = CreateRequest(String.Format("{0}/groups", datasetsUri), "GET", AccessToken());
                string responseContent = GetResponse(request);
                JavaScriptSerializer json = new JavaScriptSerializer();
                response = (Groups)json.Deserialize(responseContent, typeof(Groups));
            }
            catch (Exception ex)
            {
                handleEX(ex);
            }
            return response;
        }

        private static void AddRows(string datasetName,string tableName)
        {
            try
            {
                string datasetId = getDatasetId(datasetName);

                if (datasetId != null && isValidTableName(datasetName, tableName)) {
                    HttpWebRequest request = CreateRequest(String.Format("{0}/datasets/{1}/tables/{2}/rows", datasetsUri, datasetId, tableName), "POST", AccessToken());
                    int SequenceNumber = GetSequenceNumber(datasetName, tableName) + 1;
                    request = addSequenceNumberToRequest(request, Convert.ToString(SequenceNumber));        
                    List<Product> products = new List<Product>
                    {
                        new Product{ProductID = 1, Name="Adjustable Race", Category="Components", IsCompete = true, ManufacturedOn = new DateTime(2014, 7, 30)},
                        new Product{ProductID = 2, Name="LL Crankarm", Category="Components", IsCompete = true, ManufacturedOn = new DateTime(2014, 7, 30)},
                        new Product{ProductID = 3, Name="HL Mountain Frame - Silver", Category="Bikes", IsCompete = true, ManufacturedOn = new DateTime(2014, 7, 30)},
                    };
                    Console.WriteLine(PostRequest(request, products.ToJson(JavaScriptConverter<Product>.GetSerializer())));
                    Console.WriteLine();
                    Console.WriteLine("Rows added");
                }
            }
            catch (Exception ex)
            {
                handleEX(ex);
            }
        }

        private static void DeleteRows(string datasetName, string tableName)
        {
            try
            {
                string datasetId = getDatasetId(datasetName);
                if (datasetId != null && isValidTableName(datasetName, tableName))
                {
                    HttpWebRequest request = CreateRequest(String.Format("{0}/datasets/{1}/tables/{2}/rows", datasetsUri, datasetId, tableName), "DELETE", AccessToken());
                    request.ContentLength = 0;
                    Console.WriteLine(GetResponse(request));
                    Console.WriteLine();
                    Console.WriteLine("Rows deleted");
                }
            }
            catch (Exception ex)
            {
                handleEX(ex);
            }
        }

        private static void UpdateTableSchema(string datasetName, string tableName)
        {         
            try
            {
                string datasetId = getDatasetId(datasetName);

                if (datasetId != null && isValidTableName(datasetName, tableName))
                {
                    HttpWebRequest request = CreateRequest(String.Format("{0}/datasets/{1}/tables/{2}", datasetsUri, datasetId, tableName), "PUT", AccessToken());
                    Console.WriteLine();
                    Console.WriteLine(PostRequest(request, new Product2().ToTableSchema(tableName)));
                }
            }
            catch (Exception ex)
            {
                handleEX(ex);
            }
        }

        private static int GetSequenceNumber(string datasetName, string tableName)
        {
            int result = 0;
            try
            {
                string datasetId = getDatasetId(datasetName);

                if (datasetId != null && isValidTableName(datasetName, tableName))
                {
                    var url = string.Format("{0}/datasets/{1}/tables/{2}/sequenceNumbers", datasetsUri, datasetId, tableName);
                    HttpWebRequest request = CreateRequest(url, "GET", AccessToken());
                    string responseContent = GetResponse(request);
                    JavaScriptSerializer json = new JavaScriptSerializer();
                    SequanceRequestJson response = (SequanceRequestJson)json.Deserialize(responseContent, typeof(SequanceRequestJson));
                    if (response.value.Length > 0)
                    {
                        result = response.value[0].sequenceNumber;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                handleEX(ex);
            }
            return result;
        }

        private static HttpWebRequest addSequenceNumberToRequest(HttpWebRequest request, string SequenceNumber)
        {
            request.Headers.Add("X-PowerBI-PushData-SequenceNumber", SequenceNumber);
            return request;
        }

        private static string AccessToken()
        {
            if (token == String.Empty)
            {
                TokenCache TC = new TokenCache();
                if (pbiUsername == null || pbiPassward == null)
                {
                    updateUserAndPassword();
                }
                authContext = new AuthenticationContext(authority, TC);
                UserCredential CC = new UserCredential(pbiUsername, pbiPassward);

                /* 
                 * every new PBI user (or any user) need to consent to use the app for the first time he use her.
                 * so when creating the new app and new user use the app for the first time, use the line below insted of the line outside the comment
                 * 
                 * token = authContext.AcquireToken(resourceUri, clientID, new Uri(redirectUri), PromptBehavior.RefreshSession).AccessToken;
                 */

                token = authContext.AcquireTokenAsync(resourceUri, clientID, CC).Result.AccessToken;
            }
            else
            {
                token = authContext.AcquireTokenSilentAsync(resourceUri, clientID).Result.AccessToken;
            }
            return token;
        }

        private static void updateUserAndPassword()
        {
            bool isValidUser = false;
            while (!isValidUser)
            {
                try
                {
                    Console.Write("Enter your AAD username :");
                    string aadUsername = Console.ReadLine();

                    //we need to extract the tenant id from the AAD username in order to do the mapping to PBI user
                    string tenant = aadUsername.Split('@')[1];
                    //TODO not good users. find diffrent users.
                    switch (tenant)
                    {
                        case "ten1.com":
                            pbiUsername = "user0@a830edad9050849554E17030901.onmicrosoft.com";
                            pbiPassward = "Hpop1234";
                            isValidUser = true;
                            break;
                        case "ten2.com":
                            pbiUsername = "user0@a830edad9050849545E17030823.onmicrosoft.com";
                            pbiPassward = "Hpop1234";
                            isValidUser = true;
                            break;
                        case "ten3.com":
                            pbiUsername = "user0@a830edad9050849343E17030823.onmicrosoft.com";
                            pbiPassward = "Hpop1234";
                            isValidUser = true;
                            break;
                        default:
                            Console.WriteLine();
                            Console.WriteLine("Not valid AAD username for this sample. the tenant must be:");
                            Console.WriteLine("ten1.com, ten2.com or ten3.com . please try again ");
                            Console.WriteLine();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    handleEX(ex);
                }
            }
        }

        private static string GetResponse(HttpWebRequest request)
        {
            string response = string.Empty;
            using (HttpWebResponse httpResponse = request.GetResponse() as System.Net.HttpWebResponse)
            {
                using (StreamReader reader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
                {
                    response = reader.ReadToEnd();
                }
            }
            return response;
        }

        private static HttpWebRequest CreateRequest(string datasetsUri, string method, string accessToken)
        {
            HttpWebRequest request = System.Net.WebRequest.Create(datasetsUri) as System.Net.HttpWebRequest;
            request.KeepAlive = true;
            request.Method = method;
            request.ContentLength = 0;
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", String.Format("Bearer {0}", accessToken));

            return request;
        }

        private static string PostRequest(HttpWebRequest request, string json)
        {
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(json);
            request.ContentLength = byteArray.Length;
            using (Stream writer = request.GetRequestStream())
            {
                writer.Write(byteArray, 0, byteArray.Length);
            }

            return GetResponse(request);
        }

        private static void handleEX(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Ooops, something broke: {0}", ex);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
        }

        private static void updateDatasetAndTableName()
        {
            Console.WriteLine("Dataset Name is required, Enter value: ");
            datasetName = Console.ReadLine();
            Console.WriteLine("Table Name is required, Enter value: ");
            tableName = Console.ReadLine();
        }

        private static void PrintDatasets(dataset[] dsList)
        {
            foreach (dataset ds in dsList)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(String.Format("Name: {0}", ds.Name));
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(String.Format("   ID: {0}", ds.Id));
            }
        }

        private static void PrintTables(table[] tableList)
        {
            foreach (table tb in tableList)
            {
                Console.WriteLine(String.Format("   Table name: {0}", tb.Name));
            }
        }

        private static void PrintReports(report[] reportList)
        {
            foreach (report rep in reportList)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(String.Format("Name: {0}", rep.Name));
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(String.Format("   ID: {0}", rep.Id));
                Console.WriteLine(String.Format("   webUrl: {0}", rep.WebUrl));
                Console.WriteLine(String.Format("   embedUrl: {0}", rep.EmbedUrl));
                Console.WriteLine();
            }
        }

        private static void PrintImports(import[] importList)
        {
            foreach (import imp in importList)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(String.Format("Import Name: {0}", imp.Name));
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(String.Format("   ID: {0}", imp.Id));
                Console.WriteLine(String.Format("   Created Date Time: {0}", imp.CreatedDateTime));
                Console.WriteLine();
                Console.WriteLine(" reports:");
                PrintReports(imp.Reports);
                Console.WriteLine(" datasets:");
                PrintDatasets(imp.Datasets);
                Console.WriteLine();
                Console.WriteLine();
            }
        }

        private static void PrintDashboards(dashboard[] dsList)
        {
            foreach (dashboard ds in dsList)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(String.Format("Display Name: {0}", ds.DisplayName, ds.Id));
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(String.Format("   ID: {0}", ds.Id));
            }
        }

        private static void PrintTiles(tile[] tileList)
        {
            foreach (tile tl in tileList)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(String.Format("ID: {0}", tl.Id));
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(String.Format("   Title: {0}", tl.Title));
                Console.WriteLine(String.Format("   Embed Url: {0}", tl.EmbedUrl));

            }
        }

        private static void PrintGroups(group[] groupList)
        {
            foreach (group gr in groupList)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(String.Format("ID: {0}", gr.Id));
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(String.Format("   Name: {0}", gr.Name));
                Console.WriteLine(String.Format("   Is Read Only: {0}", gr.IsReadOnly));

            }
        }

        private static string getDatasetId(string datasetName)
        {
            string res = null;
            dataset ds = GetDatasets().value.GetDataset(datasetName);
            if (ds == null)
            {
                Console.WriteLine("No Dataset with that name");
            }
            else if (!ds.AddRowsAPIEnabled)
            {
                Console.WriteLine("Not a pushable dataset");
            }
            else
            {
                res = ds.Id;
            }
            return res;
        }

        private static bool isValidTableName(string datasetName, string tableName)
        {
            bool res = false;
            table[] tableList = GetTables(datasetName).value;
            foreach (table tb in tableList)
            {
                if (tb.Name == tableName)
                {
                    res = true;
                    break;
                }
            }
            if (!res)
            {
                Console.WriteLine("Table with that name dosnt exist in this database");
            }
            return res;
        }

        private static bool isValidDashboard(string dashboardId)
        {
            bool res = false;
            Dashboards dsList = GetDashboards();
            if (dsList != null)
            {
                foreach (dashboard ds in dsList.value)
                {
                    if (ds.Id == dashboardId)
                    {
                        return true;
                    }
                }
            }
            Console.WriteLine("No dashboard with that id");
            return res;
        }
    }
}
