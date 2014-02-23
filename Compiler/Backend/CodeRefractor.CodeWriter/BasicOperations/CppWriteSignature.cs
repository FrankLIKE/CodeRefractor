using System;
using System.Reflection;
using System.Text;
using CodeRefractor.CodeWriter.Linker;
using CodeRefractor.RuntimeBase;
using CodeRefractor.RuntimeBase.Analyze;
using CodeRefractor.RuntimeBase.MiddleEnd;
using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.Identifiers;

namespace CodeRefractor.CodeWriter.BasicOperations
{
    public static class CppWriteSignature
    {
        public static string GetArgumentsAsTextWithEscaping(this MethodInterpreter interpreter)
        {
            MethodBase method = interpreter.Method;
            var parameterInfos = method.GetParameters();
            var escapingBools = method.BuildEscapingBools();
            var sb = new StringBuilder();
            var index = 0;
            var analyze = interpreter.AnalyzeProperties;
            if (!method.IsStatic)
            {
                var parameterData = analyze.GetVariableData(new ArgumentVariable("_this"));
                if(parameterData!=EscapingMode.Unused)
                {
                    var argumentTypeDescription = UsedTypeList.Set(method.DeclaringType.GetMappedType());
                    var thisText = String.Format("const {0}& _this", argumentTypeDescription.ClrType.ToCppName(true));
                    if (!escapingBools[0])
                    {
                        thisText = String.Format("{0} _this", argumentTypeDescription.ClrType.ToCppName(true, EscapingMode.Pointer));
                    }
                    sb.Append(thisText);
                    index++;
                }
            }
            var isFirst = index == 0;
            for (index = 0; index < parameterInfos.Length; index++)
            {
                var parameterInfo = parameterInfos[index];
                var parameterData = analyze.GetVariableData(new ArgumentVariable(parameterInfo.Name));
                if(parameterData == EscapingMode.Unused)
                    continue;
                
                if (isFirst)
                    isFirst = false;
                else
                {
                    sb.Append(", ");
                }
                var isSmartPtr = escapingBools[index];
                var nonEscapingMode = isSmartPtr ? EscapingMode.Smart : EscapingMode.Pointer;
                var argumentTypeDescription = UsedTypeList.Set(parameterInfo.ParameterType.GetMappedType());
                sb.AppendFormat("{0} {1}",
                    argumentTypeDescription.ClrType.ToCppName(true, nonEscapingMode),
                    parameterInfo.Name);
            }
            return sb.ToString();
        }


        public static string WriteHeaderMethodWithEscaping(this MethodInterpreter interpreter, bool writeEndColon = true)
        {
            MethodBase methodBase = interpreter.Method;
            var retType = methodBase.GetReturnType().ToCppName(true);

            var sb = new StringBuilder();
            var declaringType = methodBase.DeclaringType;
            if (declaringType.IsGenericType)
            {
                var genericTypeCount = declaringType.GetGenericArguments().Length;

                if (genericTypeCount > 0)
                    sb.AppendLine(genericTypeCount.GetTypeTemplatePrefix());
            }

            var arguments = interpreter.GetArgumentsAsTextWithEscaping();

            sb.AppendFormat("{0} {1}({2})",
                            retType, methodBase.ClangMethodSignature(), arguments);
            if (writeEndColon)
                sb.Append(";");

            sb.AppendLine();
            return sb.ToString();
        }

        public static StringBuilder WriteSignature(MethodInterpreter interpreter, bool writeEndColon = false)
        {
            var sb = new StringBuilder();
            if (interpreter == null)
                return sb;
            var text = interpreter.WriteHeaderMethodWithEscaping(writeEndColon);
            sb.Append(text);
            return sb;
        }

    }
}