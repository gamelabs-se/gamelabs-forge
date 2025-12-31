using System;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Available AI models for FORGE item generation.
    /// </summary>
    [Serializable]
    public enum ForgeAIModel
    {
        [Tooltip("GPT-5-mini - Latest fast model. Best cost/performance ratio.")]
        GPT5Mini,
        
        [Tooltip("GPT-4o - Reliable and powerful. Good for complex items.")]
        GPT4o,
        
        [Tooltip("o1 - Premium reasoning model. Most expensive but handles complex logic best.")]
        O1
    }
    
    /// <summary>
    /// Helper class for AI model configuration.
    /// </summary>
    public static class ForgeAIModelHelper
    {
        /// <summary>
        /// Gets the API model name for the given model.
        /// </summary>
        public static string GetModelName(ForgeAIModel model)
        {
            return model switch
            {
                ForgeAIModel.GPT5Mini => "gpt-5-mini",
                ForgeAIModel.GPT4o => "gpt-4o",
                ForgeAIModel.O1 => "o1-preview",
                _ => "gpt-5-mini"
            };
        }
        
        /// <summary>
        /// Gets the display name for the model.
        /// </summary>
        public static string GetDisplayName(ForgeAIModel model)
        {
            return model switch
            {
                ForgeAIModel.GPT5Mini => "GPT-5-mini (Recommended)",
                ForgeAIModel.GPT4o => "GPT-4o",
                ForgeAIModel.O1 => "o1 (Premium Reasoning)",
                _ => "GPT-5-mini"
            };
        }
        
        /// <summary>
        /// Gets the pricing information for the model.
        /// Input cost per 1M tokens, Output cost per 1M tokens.
        /// Updated: December 2025
        /// </summary>
        public static (float inputCost, float outputCost) GetPricing(ForgeAIModel model)
        {
            return model switch
            {
                ForgeAIModel.GPT5Mini => (0.25f, 2.00f),  // $0.25/1M input, $2.00/1M output
                ForgeAIModel.GPT4o => (2.50f, 10.00f),    // $2.50/1M input, $10.00/1M output
                ForgeAIModel.O1 => (15.00f, 60.00f),      // $15/1M input, $60/1M output
                _ => (0.25f, 2.00f)
            };
        }
        
        /// <summary>
        /// Calculates the cost for the given token usage.
        /// </summary>
        public static float CalculateCost(ForgeAIModel model, int promptTokens, int completionTokens)
        {
            var (inputCost, outputCost) = GetPricing(model);
            float inputCostTotal = (promptTokens / 1000000f) * inputCost;
            float outputCostTotal = (completionTokens / 1000000f) * outputCost;
            return inputCostTotal + outputCostTotal;
        }
        
        /// <summary>
        /// Gets the recommended temperature for the model.
        /// </summary>
        public static float GetRecommendedTemperature(ForgeAIModel model)
        {
            return model switch
            {
                ForgeAIModel.GPT5Mini => 1.0f, // GPT-5 only supports temperature=1
                ForgeAIModel.GPT4o => 0.8f,
                ForgeAIModel.O1 => 1.0f, // o1 models work better with temperature = 1
                _ => 1.0f
            };
        }
        
        /// <summary>
        /// Gets a description of the model's strengths.
        /// </summary>
        public static string GetDescription(ForgeAIModel model)
        {
            return model switch
            {
                ForgeAIModel.GPT5Mini => 
                    "Latest fast model with excellent cost/performance.\n" +
                    "Best for most use cases. ~10x cheaper than GPT-4o.\n" +
                    "Note: Only supports temperature=1 (no creativity adjustment).",
                    
                ForgeAIModel.GPT4o => 
                    "Powerful and reliable for complex items.\n" +
                    "Better reasoning than GPT-5-mini.\n" +
                    "~4x cheaper than o1.",
                    
                ForgeAIModel.O1 => 
                    "Premium reasoning model. Best for complex items.\n" +
                    "Superior at understanding intricate relationships.\n" +
                    "Most expensive but highest quality reasoning.",
                    
                _ => "Standard model."
            };
        }
    }
}
