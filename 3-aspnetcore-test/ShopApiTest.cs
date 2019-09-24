﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AspNetCoreTest;
using Microsoft.AspNetCore.TestHost;
using Microsoft.DocAsTest;
using Newtonsoft.Json.Linq;

public class ShopApiTest
{
    private static readonly HttpClient s_client =
        new TestServer(Program.CreateWebHostBuilder(new string[0])).CreateClient();

    private readonly Dictionary<string, string> _variables = new Dictionary<string, string>();

    private readonly JsonDiff _jsonDiff = new JsonDiffBuilder()
        .UseAdditionalProperties()
        .UseWildcard()
        .Build();

    [YamlTest("~/3-aspnetcore-test/**/*.yml")]
    public async Task Run(ShopApiTestSpec spec)
    {
        foreach (var request in spec.Requests)
        {
            var response = await SendRequest(request);

            var responseContent = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();

            _jsonDiff.Verify(
                expected: JToken.Parse(request.Response),
                actual: JToken.Parse(responseContent));
        }
    }

    private Task<HttpResponseMessage> SendRequest(ApiTestSpec request)
    {
        if (request.Get != null)
        {
            return s_client.GetAsync(ApplyVariables(request.Get));
        }

        if (request.Post != null)
        {
            return s_client.PostAsync(
                ApplyVariables(request.Post),
                new StringContent(ApplyVariables(request.Body), Encoding.UTF8, "application/json"));
        }

        if (request.Delete != null)
        {
            return s_client.DeleteAsync(ApplyVariables(request.Delete));
        }

        throw new NotSupportedException();
    }

    private string ApplyVariables(string content)
    {
        foreach (var (key, value) in _variables)
        {
            content = content.Replace($"{{{key}}}", value);
        }
        return content;
    }
}

public class ApiTestSpec
{
    public string Post { get; set; }

    public string Get { get; set; }

    public string Delete { get; set; }

    public string Body { get; set; }

    public string Response { get; set; }
}

public class ShopApiTestSpec
{
    public ApiTestSpec[] Requests { get; set; }
}
