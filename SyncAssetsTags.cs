using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Linq;
using System.Text;

namespace SyncAssetTag
{

    public class SyncAssetsTags
    {
        static HttpClient client = new HttpClient();

        static Token accessToken { get; set; }

        static string apiUrl = "https:///api/";
        internal class Token
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }
        }
        public static async Task GetToken()
        {
            string baseAddress = "https:///auth/token";
            client = new HttpClient();
            var form = new Dictionary<string, string>
                {
                    {"grant_type", "client_credentials"},
                    {"client_id", ""},
                    {"client_secret", ""},
                    {"scope", "all"}
                };

            HttpResponseMessage tokenResponse = await client.PostAsync(baseAddress, new FormUrlEncodedContent(form));
            var jsonContent = await tokenResponse.Content.ReadAsStringAsync();
            Token tok = JsonConvert.DeserializeObject<Token>(jsonContent);
            accessToken = tok;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tok.AccessToken);
        }


        [FunctionName("SyncAssetsTags")]
        public async Task RunAsync([TimerTrigger("0 */60 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            await GetToken();

            await SyncAssets();
            client.Dispose();
            return;
        }

        public static async Task<AssetRoot> GetAssetDetails(int id)
        {
            var response = await httphaloGetAsync("Asset/" + id);
            var asset = JsonConvert.DeserializeObject<AssetRoot>(response);
            return asset;
        }

        public static async Task<string> httphaloPostAsync(string api, JArray jArray)
        {

            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                response = await client.PostAsync(apiUrl + api, new StringContent(jArray.ToString(), Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}" + "  -  " + jArray.ToString());
            }

            string result = await response.Content.ReadAsStringAsync();
            return result;
        }

        public static async Task SyncAssets()
        {
            var i = 1;
            var response = "";
            while (true)
            {
                response = await httpHaloGetPage("Asset", i);
                var assetlist = JsonConvert.DeserializeObject<Root>(response);

                if (assetlist.record_count == 0)
                {
                    break;
                }

                foreach (var asset in assetlist.assets)
                {
                    var assetdetails = await GetAssetDetails(asset.id);
                    if (assetdetails == null) { continue; }
                    if (assetdetails.fields == null) { continue; }

                    asset.fields = assetdetails.fields;

                    var assetname = "";

                    assetname = asset.fields.FirstOrDefault(p => p.name == "Name")?.value;
                    if (assetname == "" || assetname == null)
                    {
                        assetname = asset.fields.FirstOrDefault(p => p?.name == "device_name")?.value;
                    }
                    if (assetname == "" || assetname == null)
                    {
                        assetname = asset.fields.FirstOrDefault(p => p?.name == "longname")?.value;
                    }

                    if (assetname == null) { continue; }

                    if (!asset.inventory_number.Equals(asset.client_id + "\\" + assetname))
                    {
                        var jarray = new JArray();
                        var jobject = new JObject();
                        jobject["id"] = asset.id;
                        jobject["inventory_number"] = asset.client_id + "\\" + assetname;
                        jarray.Add(jobject);

                        await httphaloPostAsync("Asset", jarray);
                    }               
                }
                i++;
            }
        }
        

        public static async Task<string> httphaloGetAsync(string api)
        {
            string result = "";
            try
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl + api);
                response.EnsureSuccessStatusCode();
                result = await response.Content.ReadAsStringAsync();
            }

            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
            return result;
        }

        public static async Task<string> httpHaloGetPage(string api, int pagenumber)
        {
            var responseBody = await httphaloGetAsync(api + "?pageinate=true&page_size=50&page_no=" + pagenumber);
            return responseBody;
        }
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Asset
    {
        public List<Field> fields { get; set; }
        public int id { get; set; }
        public string inventory_number { get; set; }
        public string key_field { get; set; }
        public string key_field2 { get; set; }
        public string key_field3 { get; set; }
        public int client_id { get; set; }
        public string client_name { get; set; }
        public int site_id { get; set; }
        public string site_name { get; set; }
        public int business_owner_id { get; set; }
        public int business_owner_cab_id { get; set; }
        public int technical_owner_id { get; set; }
        public int technical_owner_cab_id { get; set; }
        public int assettype_id { get; set; }
        public string assettype_name { get; set; }
        public string colour { get; set; }
        public bool inactive { get; set; }
        public string contract_ref { get; set; }
        public int supplier_id { get; set; }
        public int supplier_contract_id { get; set; }
        public int supplier_sla_id { get; set; }
        public int supplier_priority_id { get; set; }
        public int itemstock_id { get; set; }
        public int item_id { get; set; }
        public bool non_consignable { get; set; }
        public int reserved_salesorder_id { get; set; }
        public int reserved_salesorder_line_id { get; set; }
        public string use { get; set; }
        public int device_number { get; set; }
        public int status_id { get; set; }
        public int third_party_id { get; set; }
        public int automate_id { get; set; }
        public int ninjarmm_id { get; set; }
        public int syncroid { get; set; }
        public string itglue_url { get; set; }
        public int defaultsequence { get; set; }
        public int datto_alternate_id { get; set; }
        public int snow_id { get; set; }
        public int passportal_id { get; set; }
        public string auvik_device_id { get; set; }
        public string datto_id { get; set; }
        public string addigy_id { get; set; }
        public int issue_consignment_line_id { get; set; }
        public string item_name { get; set; }
        public string datto_url { get; set; }
        public int ncentral_details_id { get; set; }
    }

    public class Root
    {
        public int page_no { get; set; }
        public int page_size { get; set; }
        public int record_count { get; set; }
        public List<Asset> assets { get; set; }
    }

    public class Field
    {
        public int id { get; set; }
        public string name { get; set; }
        public string validate { get; set; }
        public string value { get; set; }
        public string display { get; set; }
        public bool mandatory { get; set; }
        public bool showonactivity { get; set; }
        public int lookup { get; set; }
        public int systemuse { get; set; }
        public int parenttype_id { get; set; }
        public string url { get; set; }
        public int access_level { get; set; }
        public int typeinfo_id { get; set; }
        public int tab_id { get; set; }
        public string tab_name { get; set; }
        public int techdetail { get; set; }
        public int userdetail { get; set; }
    }

    public class AssetRoot
    {
        public int id { get; set; }
        public string inventory_number { get; set; }
        public string key_field { get; set; }
        public string key_field2 { get; set; }
        public string key_field3 { get; set; }
        public int client_id { get; set; }
        public string client_name { get; set; }
        public int site_id { get; set; }
        public string site_name { get; set; }
        public int business_owner_id { get; set; }
        public string business_owner_name { get; set; }
        public int business_owner_cab_id { get; set; }
        public int technical_owner_id { get; set; }
        public string technical_owner_name { get; set; }
        public int technical_owner_cab_id { get; set; }
        public int assettype_id { get; set; }
        public string assettype_name { get; set; }
        public string colour { get; set; }
        public bool inactive { get; set; }
        public string contract_ref { get; set; }
        public int supplier_id { get; set; }
        public string supplier_name { get; set; }
        public int supplier_contract_id { get; set; }
        public string supplier_contract_ref { get; set; }
        public int supplier_sla_id { get; set; }
        public int supplier_priority_id { get; set; }
        public List<Field> fields { get; set; }
        public List<object> customfields { get; set; }
        public List<object> custombuttons { get; set; }
        public int itemstock_id { get; set; }
        public int item_id { get; set; }
        public bool non_consignable { get; set; }
        public int reserved_salesorder_id { get; set; }
        public int reserved_salesorder_line_id { get; set; }
        public int sla_id { get; set; }
        public int priority_id { get; set; }
        public int contract_id { get; set; }
        public int goodsin_po_id { get; set; }
        public int commissioned { get; set; }
        public string intune_id { get; set; }
        public int prtg_id { get; set; }
        public int device42_id { get; set; }
        public string ateraid { get; set; }
        public string lansweeper_id { get; set; }
        public string lansweeper_url { get; set; }
        public DateTime dlastupdate { get; set; }
        public double item_cost { get; set; }
        public string itglue_id { get; set; }
        public bool bookmarked { get; set; }
        public string auvik_network_id { get; set; }
        public string qualys_id { get; set; }
        public string azureTenantId { get; set; }
        public string use { get; set; }
        public int device_number { get; set; }
        public int status_id { get; set; }
        public int third_party_id { get; set; }
        public int automate_id { get; set; }
        public int ninjarmm_id { get; set; }
        public int syncroid { get; set; }
        public string itglue_url { get; set; }
        public int datto_alternate_id { get; set; }
        public int snow_id { get; set; }
        public int passportal_id { get; set; }
        public string auvik_device_id { get; set; }
        public string datto_id { get; set; }
        public string addigy_id { get; set; }
        public int issue_consignment_line_id { get; set; }
        public string datto_url { get; set; }
        public int ncentral_details_id { get; set; }
    }
}
