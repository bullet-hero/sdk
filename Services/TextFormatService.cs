using System;
using System.Collections.Generic;
using System.Text;

namespace BH.SDK.Services
{
    /// <summary> Substitutes named variables into a template string, each resolved when it is actually reached
    /// rather than up front. </summary>
    public class TextFormatService
    {
        private readonly Dictionary<string, Func<string>> _variables;
        private readonly StringBuilder _expressionBuilder;
        private readonly StringBuilder _variableBuilder;

        /// <summary> Every member at once, in declaration order. </summary>
        public TextFormatService(int variablesCapacity = 16, 
            int expressionBuilderCapacity = 256, int variableBuilderCapacity = 16)
        {
            _variables = new Dictionary<string, Func<string>>(variablesCapacity);
            _expressionBuilder = new StringBuilder(expressionBuilderCapacity);
            _variableBuilder = new StringBuilder(variableBuilderCapacity);
        }

        /// <summary> Registers a variable and how to resolve it; false when the key is taken. </summary>
        public bool AddVariable(string variableKey, Func<string> method)
        {
            return _variables.TryAdd(variableKey, method);
        }
        /// <summary> Forgets one. </summary>
        public bool RemoveVariable(string variableKey)
        {
            return _variables.Remove(variableKey);
        }
        /// <summary> Forgets all of them. </summary>
        public void Clear()
        {
            _variables.Clear();
            _expressionBuilder.Clear();
            _variableBuilder.Clear();
        }

        /// <summary> Substitutes every registered variable, resolving each only when it is actually reached. </summary>
        public string Process(string expression)
        {
            _expressionBuilder.Clear();
            _variableBuilder.Clear();
            
            var inBrackets = false;
            
            foreach (var currentChar in expression)
            {
                switch (currentChar)
                {
                    case '{':
                        if (inBrackets) 
                            throw new ArgumentException("Brackets already opened");
                        inBrackets = true;
                        break;
                    
                    case '}':
                        if (!inBrackets)
                            throw new ArgumentException("Brackets already closed");
                        inBrackets = false;

                        var variable = _variableBuilder.ToString();
                        _variableBuilder.Clear();
                        var variableValue = _variables[variable]();
                        _expressionBuilder.Append(variableValue);
                        break;
                    
                    default:
                        if (inBrackets)
                            _variableBuilder.Append(currentChar);
                        else _expressionBuilder.Append(currentChar);
                        break;
                }
            }
            
            if (inBrackets)
                throw new ArgumentException("Brackets is not closed");
            
            return _expressionBuilder.ToString();
        }
    }
}