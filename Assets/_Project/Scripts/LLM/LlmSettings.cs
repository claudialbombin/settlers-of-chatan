using UnityEngine;

namespace Chatan.LLM
{
    /// <summary>
    /// Configuration of the language model: which one to use and how to reach it.
    /// </summary>
    /// <remarks>
    /// The asset in the repo (Data/Settings/LlmSettings.asset) keeps <see cref="useMock"/>
    /// on, so the project opens without errors on a computer without Ollama. Turn it off
    /// locally to talk to the real model.
    /// </remarks>
    [CreateAssetMenu(menuName = "Chatan/LLM Settings", fileName = "LlmSettings")]
    public class LlmSettings : ScriptableObject
    {
        /// <summary>Default Ollama address.</summary>
        public const string DefaultBaseUrl = "http://localhost:11434";

        /// <summary>Default model: small, fast and good at Spanish.</summary>
        public const string DefaultModel = "qwen3.5:4b";

        [Tooltip("Si está activado, se usa MockLanguageModel y no se llama a nada.")]
        public bool useMock = true;

        [Tooltip("Dirección de Ollama.")]
        public string baseUrl = DefaultBaseUrl;

        [Tooltip("Nombre del modelo tal y como sale en 'ollama list'.")]
        public string model = DefaultModel;

        [Tooltip("Segundos antes de rendirse. Si se pasa, el juego usa respuestas de reserva.")]
        [Min(1)]
        public int timeoutSeconds = 20;

        [Tooltip("Pide al modelo que no 'piense' antes de responder (qwen3.5, gpt-oss...). Mucho más rápido.")]
        public bool disableThinking = true;

        [Tooltip("Retardo falso del Mock para ver el 'escribiendo...'.")]
        [Min(0f)]
        public float mockDelaySeconds = 0.6f;

        [Tooltip("Imprime prompts y respuestas en la consola.")]
        public bool logPrompts;

        /// <summary>Full address of Ollama's chat endpoint, without a doubled slash.</summary>
        public string ChatUrl => (baseUrl ?? DefaultBaseUrl).TrimEnd('/') + "/api/chat";
    }
}
