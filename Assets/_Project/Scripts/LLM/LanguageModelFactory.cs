namespace Chatan.LLM
{
    /// <summary>Chooses the language model from the settings.</summary>
    public static class LanguageModelFactory
    {
        /// <summary>
        /// The mock when there are no settings or <see cref="LlmSettings.useMock"/> is on,
        /// otherwise an <see cref="OllamaClient"/>.
        /// </summary>
        public static ILanguageModel Create(LlmSettings settings) =>
            settings == null || settings.useMock
                ? new MockLanguageModel(settings != null ? settings.mockDelaySeconds : 0f)
                : new OllamaClient(settings);
    }
}
