using ApiModules.Crypto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TeamServer.Services;
using Microsoft.Extensions.Configuration;

namespace TeamServer.Models
{
    [Controller]
    public class HttpListenerController : ControllerBase
    {
        private readonly IAgentService _agents;
        private readonly IConfiguration _config;

        public HttpListenerController(IAgentService agents, IConfiguration config) //constructor takes IAgentService
        {
            _agents = agents;
            _config = config;
        }

        public async Task<IActionResult> HandleImplant()
        {
            var metadata = ExtractMetadata(HttpContext.Request.Headers);
            if (metadata is null) return NotFound();

            var agent = _agents.GetAgent(metadata.Id);
            
            if (agent is null)
            {
                agent = new Agent(metadata);
                _agents.AddAgent(agent);
            }
            
            agent.CheckIn();

            if (HttpContext.Request.Method == "POST")
            {
                string json;
                
                using (var sr = new StreamReader(HttpContext.Request.Body))
                {
                    json = await sr.ReadToEndAsync();
                }

                var results = JsonConvert.DeserializeObject<IEnumerable<AgentTaskResult>>(json);
                agent.AddTaskResults(results);
            }
            
            var tasks = agent.GetPendingTasks();
            return Ok(tasks);
        }

        private AgentMetadata ExtractMetadata(IHeaderDictionary headers)
        {
            if (!headers.TryGetValue("Authorization", out var headerValue))
                return null;

            // Strip "Bearer " prefix safely
            var encryptedMetadata = headerValue.ToString();
            if (encryptedMetadata.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                encryptedMetadata = encryptedMetadata.Substring(7);

            try
            {
                // Decrypt using the shared key from appsettings.json
                var encryptionKey = _config["Encryption:Key"];
                var json = AesEncryption.Decrypt(encryptedMetadata, encryptionKey);
                return JsonConvert.DeserializeObject<AgentMetadata>(json);
            }
            catch
            {
                // If decryption fails for any reason, reject the request
                return null;
            }
        }
    }
}