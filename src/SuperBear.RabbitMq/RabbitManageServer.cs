using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Superbear.Common.Contract;
using SuperBear.RabbitMq.Domain;

namespace SuperBear.RabbitMq
{
    public class RabbitManageServer
    {
        private readonly RabbitOption _rabbitOption;
        private readonly string _url;
        public RabbitManageServer(IOptions<RabbitOption> rabbitOption)
        {
            _rabbitOption = rabbitOption.Value;
            _url = $"http://{_rabbitOption.HostName}:{_rabbitOption.ManagePort}";
        }
        private string GetAuthorization()
        {
            byte[] bytes = Encoding.UTF8.GetBytes($"{_rabbitOption.UserName}:{_rabbitOption.Password}");
            return Convert.ToBase64String(bytes);
        }
        public static string BuildQuery(IDictionary<string, string> parameters)
        {
            var postData = new StringBuilder();
            var hasParam = false;
            using (var dem = parameters.GetEnumerator())
            {
                while (dem.MoveNext())
                {
                    var name = dem.Current.Key;
                    var value = dem.Current.Value;
                    // 忽略参数名或参数值为空的参数
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                    {
                        if (hasParam)
                            postData.Append("&");
                        postData.Append(name);
                        postData.Append("=");
                        var encodedValue = HttpUtility.UrlEncode(value, Encoding.UTF8);
                        postData.Append(encodedValue);
                        hasParam = true;
                    }
                }
                return postData.ToString();
            }
        }
        public async Task<ResponseResult<QueuesInfo>> GetQueues(string keyWord, bool useRegex = false)
        {
            var suffix = "api/queues";
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {GetAuthorization()}");
                IDictionary<string, string> parameters = new Dictionary<string, string>()
                {
                    {"page","1"},
                    {"page_size","100"},
                    {"use_regex",useRegex.ToString().ToLower()},
                    {"pagination","true" }
                };
                if (!string.IsNullOrEmpty(keyWord))
                {
                    parameters.Add("name", keyWord);
                }
                var query = BuildQuery(parameters);
                var url = $"{_url}/{suffix}?{query}";
                var result = await httpClient.GetStringAsync(url);
                QueuesInfo queuesInfo;
                try
                {
                    queuesInfo = JsonConvert.DeserializeObject<QueuesInfo>(result);
                }
                catch (Exception)
                {
                    return new ResponseResult<QueuesInfo>(null)
                    {
                        State = false,
                        Message = result,
                        Result = null
                    };
                }
                return new ResponseResult<QueuesInfo>(queuesInfo);
            }
        }
    }
}
