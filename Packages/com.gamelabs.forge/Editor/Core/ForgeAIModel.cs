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
        [Tooltip("GPT-4o - Recommended. Fast, cost-effective, and high quality.")]
        GPT4o,
        
        [Tooltip("o1 - Premium reasoning model. More expensive but handles complex logic better.")]
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
                ForgeAIModel.GPT4o => "gpt-4o",
                ForgeAIModel.O1 => "o1-preview",
                _ => "gpt-4o"
            };
        }
        
        /// <summary>
        /// Gets the display name for the model.
        /// </summary>
        public static string GetDisplayName(ForgeAIModel model)
        {
            return model switch
            {
                ForgeAIModel.GPT4o => "GPT-4o (Recommended)",
                ForgeAIModel.O1 => "o1 (Premium Reasoning)",
                _ => "GPT-4o"
            };
        }
        
        /// <summary>
        /// Gets the pricing information for the model.
        /// Input cost per 1M tokens, Output cost per 1M tokens.
        /// </summary>
        public static (float inputCost, float outputCost) GetPricing(ForgeAIModel model)
        {
            return model switch
            {
                ForgeAIModel.GPT4o => (2.50f, 10.00f), // $2.50/1M input, $10/1M output
                ForgeAIModel.O1 => (15.00f, 60.00f),   // $15/1M input, $60/1M output (o1-preview pricing)
                _ => (2.50f, 10.00f)
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
                ForgeAIModel.GPT4o => 0.8f,
                ForgeAIModel.O1 => 1.0f, // o1 models work better with temperature = 1
                _ => 0.8f
            };
        }
        
        /// <summary>
        /// Gets a description of the model's strengths.
        /// </summary>
        public static string GetDescription(ForgeAIModel model)
        {
            return model switch
            {
                ForgeAIModel.GPT4o => 
                    "Fast and cost-effective. Best for most use cases.\n" +
                    "Good balance of quality, speed, and cost.\n" +
                    "~4x cheaper than o1.",
                    
                ForgeAIModel.O1 => 
                    "Premium reasoning model. Best for complex items.\n" +
                    "Better at understanding intricate relationships.\n" +
                    "May return different response structure - uses extended reasoning.",
                    
                _ => "Standard model."
            };
        }
    }
}
