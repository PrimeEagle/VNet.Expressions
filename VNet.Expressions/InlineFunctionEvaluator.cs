﻿using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;

namespace VNet.Expressions
{
    public class InlineFunctionEvaluator
    {
        public static T Evaluate<T>(string methodBody, Dictionary<string, object> parameters)
        {
            var localMethodBody = methodBody.Trim();
            if (!localMethodBody.StartsWith("return ")) localMethodBody = "return " + localMethodBody;

            var codeProvider = new CSharpCodeProvider(new Dictionary<string, string> {{"CompilerVersion", "v4.0"}});
            var compilerParams = new CompilerParameters
            {
                GenerateInMemory = true,
                GenerateExecutable = false
            };

            var parameterAssignments = string.Join(Environment.NewLine, parameters.Select(p => $"var {p.Key} = ({p.Value.GetType().FullName})parameters[\"{p.Key}\"];"));

            var code = $@"
            using System;
            using System.Collections.Generic;

            public static class InlineFunction
            {{
                public static {typeof(T).FullName} Evaluate(Dictionary<string, object> parameters)
                {{
                    {parameterAssignments}
                    return {localMethodBody};
                }}
            }}";

            var compileResult = codeProvider.CompileAssemblyFromSource(compilerParams, code);

            if (compileResult.Errors.HasErrors)
            {
                var errorMessage = string.Join(Environment.NewLine, compileResult.Errors);
                throw new InvalidOperationException($"Error compiling inline function: {errorMessage}");
            }

            var assembly = compileResult.CompiledAssembly;
            var type = assembly.GetType("InlineFunction");
            var method = type.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static);

            try
            {
                var result = (T) method.Invoke(null, new object[] {parameters});

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error evaluating inline function", ex);
            }
        }
    }
}