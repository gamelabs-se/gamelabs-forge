using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Extracts JSON schema from C# types using reflection.
    /// Used to generate dynamic templates for the AI to understand item structures.
    /// </summary>
    public static class ForgeSchemaExtractor
    {
        /// <summary>
        /// Represents a field in the schema with its metadata.
        /// </summary>
        [Serializable]
        public class FieldSchema
        {
            public string name;
            public string type;
            public string description;
            public bool isRequired;
            public object defaultValue;
            public object minValue;
            public object maxValue;
            public string[] enumValues;
        }
        
        /// <summary>
        /// Represents the complete schema of a type.
        /// </summary>
        [Serializable]
        public class TypeSchema
        {
            public string typeName;
            public string description;
            public List<FieldSchema> fields = new List<FieldSchema>();
        }
        
        /// <summary>
        /// Extracts schema from a type.
        /// </summary>
        public static TypeSchema ExtractSchema(Type type)
        {
            var schema = new TypeSchema
            {
                typeName = type.Name,
                description = GetTypeDescription(type)
            };
            
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var fieldSchema = ExtractFieldSchema(field);
                if (fieldSchema != null)
                    schema.fields.Add(fieldSchema);
            }
            
            return schema;
        }
        
        /// <summary>
        /// Extracts schema from a generic type.
        /// </summary>
        public static TypeSchema ExtractSchema<T>() where T : class
        {
            return ExtractSchema(typeof(T));
        }
        
        private static FieldSchema ExtractFieldSchema(FieldInfo field)
        {
            // Skip non-serializable fields
            if (field.IsNotSerialized || field.GetCustomAttribute<NonSerializedAttribute>() != null)
                return null;
                
            var schema = new FieldSchema
            {
                name = field.Name,
                type = GetJsonType(field.FieldType),
                description = GetFieldDescription(field),
                isRequired = true
            };
            
            // Handle ForgeConstraint attribute (takes priority)
            var constraintAttr = field.GetCustomAttribute<ForgeConstraintAttribute>();
            if (constraintAttr != null)
            {
                if (constraintAttr.MinValue != null)
                    schema.minValue = constraintAttr.MinValue;
                if (constraintAttr.MaxValue != null)
                    schema.maxValue = constraintAttr.MaxValue;
                if (constraintAttr.AllowedValues != null && constraintAttr.AllowedValues.Length > 0)
                    schema.enumValues = constraintAttr.AllowedValues;
                schema.isRequired = constraintAttr.Required;
            }
            
            // Handle Range attribute for numeric types (if not already set by ForgeConstraint)
            if (schema.minValue == null || schema.maxValue == null)
            {
                var rangeAttr = field.GetCustomAttribute<RangeAttribute>();
                if (rangeAttr != null)
                {
                    schema.minValue ??= rangeAttr.min;
                    schema.maxValue ??= rangeAttr.max;
                }
            }
            
            // Handle Min attribute
            if (schema.minValue == null)
            {
                var minAttr = field.GetCustomAttribute<MinAttribute>();
                if (minAttr != null)
                {
                    schema.minValue = minAttr.min;
                }
            }
            
            // Handle enums
            if (field.FieldType.IsEnum && (schema.enumValues == null || schema.enumValues.Length == 0))
            {
                schema.enumValues = Enum.GetNames(field.FieldType);
            }
            
            // Set default values based on type
            schema.defaultValue = GetDefaultValue(field.FieldType);
            
            return schema;
        }
        
        private static string GetJsonType(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
                return "integer";
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                return "number";
            if (type == typeof(bool)) return "boolean";
            if (type.IsEnum) return "string";
            if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
                return "array";
            return "object";
        }
        
        private static object GetDefaultValue(Type type)
        {
            if (type == typeof(string)) return "";
            if (type == typeof(int)) return 0;
            if (type == typeof(float)) return 0f;
            if (type == typeof(bool)) return false;
            if (type.IsEnum) return Enum.GetNames(type)[0];
            return null;
        }
        
        private static string GetTypeDescription(Type type)
        {
            var attr = type.GetCustomAttribute<ForgeDescriptionAttribute>();
            return attr?.Description ?? $"A {type.Name} item definition.";
        }
        
        private static string GetFieldDescription(FieldInfo field)
        {
            var attr = field.GetCustomAttribute<ForgeDescriptionAttribute>();
            if (attr != null) return attr.Description;
            
            var tooltip = field.GetCustomAttribute<TooltipAttribute>();
            return tooltip?.tooltip ?? field.Name;
        }
        
        /// <summary>
        /// Generates a JSON template string from the schema.
        /// </summary>
        public static string GenerateJsonTemplate(TypeSchema schema, bool includeDescriptions = false)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            
            for (int i = 0; i < schema.fields.Count; i++)
            {
                var field = schema.fields[i];
                var comma = i < schema.fields.Count - 1 ? "," : "";
                
                if (includeDescriptions && !string.IsNullOrEmpty(field.description))
                {
                    sb.AppendLine($"  // {field.description}");
                }
                
                var value = GetTemplateValue(field);
                sb.AppendLine($"  \"{field.name}\": {value}{comma}");
            }
            
            sb.AppendLine("}");
            return sb.ToString();
        }
        
        /// <summary>
        /// Generates a prompt-friendly schema description.
        /// </summary>
        public static string GenerateSchemaDescription(TypeSchema schema)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Item Type: {schema.typeName}");
            sb.AppendLine($"Description: {schema.description}");
            sb.AppendLine("Fields:");
            
            foreach (var field in schema.fields)
            {
                sb.Append($"  - {field.name} ({field.type})");
                
                if (field.minValue != null || field.maxValue != null)
                {
                    sb.Append($" [range: {field.minValue ?? "any"} to {field.maxValue ?? "any"}]");
                }
                
                if (field.enumValues != null && field.enumValues.Length > 0)
                {
                    sb.Append($" [options: {string.Join(", ", field.enumValues)}]");
                }
                
                if (!string.IsNullOrEmpty(field.description) && field.description != field.name)
                {
                    sb.Append($" - {field.description}");
                }
                
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
        
        private static string GetTemplateValue(FieldSchema field)
        {
            switch (field.type)
            {
                case "string":
                    if (field.enumValues != null && field.enumValues.Length > 0)
                        return $"\"{field.enumValues[0]}\"";
                    return "\"string\"";
                case "integer":
                    if (field.minValue != null)
                        return field.minValue.ToString();
                    return "0";
                case "number":
                    if (field.minValue != null)
                        return field.minValue.ToString();
                    return "0.0";
                case "boolean":
                    return "false";
                case "array":
                    return "[]";
                case "object":
                    return "{}";
                default:
                    return "null";
            }
        }
    }
    
    /// <summary>
    /// Attribute to provide descriptions for types and fields in the schema.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property)]
    public class ForgeDescriptionAttribute : Attribute
    {
        public string Description { get; }
        
        public ForgeDescriptionAttribute(string description)
        {
            Description = description;
        }
    }
    
    /// <summary>
    /// Attribute to specify constraints for field generation.
    /// Used by ForgeSchemaExtractor to communicate constraints to the AI.
    /// <example>
    /// <code>
    /// // Constrain damage to a specific range
    /// [ForgeConstraint(MinValue = 10, MaxValue = 100)]
    /// public int damage;
    /// 
    /// // Limit weapon types to specific values
    /// [ForgeConstraint(AllowedValues = new[] { "Sword", "Axe", "Mace" })]
    /// public string weaponType;
    /// 
    /// // Make a field optional
    /// [ForgeConstraint(Required = false)]
    /// public string optionalNote;
    /// </code>
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ForgeConstraintAttribute : Attribute
    {
        /// <summary>Minimum value for numeric fields.</summary>
        public object MinValue { get; set; }
        
        /// <summary>Maximum value for numeric fields.</summary>
        public object MaxValue { get; set; }
        
        /// <summary>Allowed values for string fields (creates an enum-like constraint).</summary>
        public string[] AllowedValues { get; set; }
        
        /// <summary>Whether this field is required (default: true).</summary>
        public bool Required { get; set; } = true;
        
        public ForgeConstraintAttribute() { }
    }
}
