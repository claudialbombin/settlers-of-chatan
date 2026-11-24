using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Chatan.LLM
{
    /// <summary>
    /// <see cref="ILanguageModel"/> that talks over HTTP to Ollama, which runs the model on
    /// this computer (free, offline, no keys).
    /// </summary>
    /// <remarks>
    /// It uses Ollama's <c>/api/chat</c> without streaming. Network errors, timeouts,
    /// cancellations and unreadable answers all come back as <see cref="LlmResponse.Fail"/>,
    /// never as exceptions, so the game can fall back to canned replies.
    /// </remarks>
    public class OllamaClient : ILanguageModel
    {
        private readonly LlmSettings _settings;

        /// <summary>Creates a client with those settings.</summary>
        public OllamaClient(LlmSettings settings)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        /// <inheritdoc/>
        public string DisplayName => $"Ollama {_settings.model}";

        // ---- Shape of Ollama's /api/chat ----
        private sealed class ChatRequest
        {
            [JsonProperty("model")] public string Model;
            [JsonProperty("messages")] public List<Message> Messages;
            [JsonProperty("stream")] public bool Stream;
            [JsonProperty("format", NullValueHandling = NullValueHandling.Ignore)] public string Format;
            [JsonProperty("think", NullValueHandling = NullValueHandling.Ignore)] public bool? Think;
            [JsonProperty("options")] public Options Options;
        }

        private sealed class Message
        {
            [JsonProperty("role")] public string Role;
            [JsonProperty("content")] public string Content;
        }

        private sealed class Options
        {
            [JsonProperty("temperature")] public float Temperature;
            [JsonProperty("num_predict")] public int NumPredict;
        }

        private sealed class ChatResponse
        {
            [JsonProperty("message")] public Message Message;
            [JsonProperty("error")] public string Error;
        }

        /// <inheritdoc/>
        public async Task<LlmResponse> CompleteAsync(LlmRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || !request.HasMessages)
            {
                return LlmResponse.Fail("Request vacío");
            }

            string json = BuildRequestJson(request, _settings);
            if (_settings.logPrompts)
            {
                Debug.Log($"[LLM →] {json}");
            }

            using var web = new UnityWebRequest(_settings.ChatUrl, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = Math.Max(1, _settings.timeoutSeconds)
            };
            web.SetRequestHeader("Content-Type", "application/json");

            float start = Time.realtimeSinceStartup;
            try
            {
                using (cancellationToken.Register(web.Abort))
                {
                    await web.SendAsync();
                }
            }
            catch (Exception e)
            {
                return LlmResponse.Fail(e.Message, Time.realtimeSinceStartup - start);
            }

            float latency = Time.realtimeSinceStartup - start;
            if (cancellationToken.IsCancellationRequested)
            {
                return LlmResponse.Fail("Cancelado", latency);
            }

            if (web.result != UnityWebRequest.Result.Success)
            {
                string detail = web.result == UnityWebRequest.Result.ProtocolError
                    ? ReadError(web.downloadHandler?.text) ?? web.error
                    : web.error;
                return LlmResponse.Fail($"{web.result}: {detail} (¿está Ollama abierto en {_settings.baseUrl}?)", latency);
            }

            LlmResponse response = ParseResponse(web.downloadHandler.text, latency);
            if (_settings.logPrompts)
            {
                Debug.Log($"[LLM ←] {response}");
            }

            return response;
        }

        /// <summary>The JSON body sent to Ollama for <paramref name="request"/>. Public for the tests.</summary>
        public static string BuildRequestJson(LlmRequest request, LlmSettings settings)
        {
            var body = new ChatRequest
            {
                Model = settings.model,
                Messages = request.Messages.ConvertAll(m => new Message { Role = RoleName(m.Role), Content = m.Content }),
                Stream = false,
                Format = request.ExpectJson ? "json" : null,
                Think = settings.disableThinking ? false : (bool?)null,
                Options = new Options { Temperature = request.Temperature, NumPredict = request.MaxTokens }
            };
            return JsonConvert.SerializeObject(body);
        }

        /// <summary>Turns Ollama's answer into a response. Public for the tests.</summary>
        public static LlmResponse ParseResponse(string body, float latencySeconds = 0f)
        {
            try
            {
                var parsed = JsonConvert.DeserializeObject<ChatResponse>(body ?? string.Empty);
                if (!string.IsNullOrEmpty(parsed?.Error))
                {
                    return LlmResponse.Fail(parsed.Error, latencySeconds);
                }

                string content = parsed?.Message?.Content?.Trim();
                return string.IsNullOrEmpty(content)
                    ? LlmResponse.Fail("Respuesta vacía", latencySeconds)
                    : LlmResponse.Ok(content, latencySeconds);
            }
            catch (JsonException e)
            {
                return LlmResponse.Fail("JSON de Ollama ilegible: " + e.Message, latencySeconds);
            }
        }

        /// <summary>Ollama's name for a role.</summary>
        public static string RoleName(LlmRole role) => role switch
        {
            LlmRole.System => "system",
            LlmRole.Assistant => "assistant",
            _ => "user"
        };

        // On HTTP errors Ollama sends {"error": "..."}, e.g. "model 'x' not found".
        private static string ReadError(string body)
        {
            try
            {
                return string.IsNullOrEmpty(body) ? null : JsonConvert.DeserializeObject<ChatResponse>(body)?.Error;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
