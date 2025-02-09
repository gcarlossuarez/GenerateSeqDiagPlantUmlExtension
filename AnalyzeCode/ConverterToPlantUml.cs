using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzeCode
{
    class ConverterToPlantUml
    {
        public string GeneratePlantUml(ExtendedControlFlowSyntaxWalker walker)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("@startuml");

            // Definir participantes con alias si es necesario
            Dictionary<string, string> participantAliases = new Dictionary<string, string>();
            int participantId = 1;
            foreach (var call in walker.MethodCalls)
            {
                var parts = call.Split(new string[] { " -> " }, StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    var caller = parts[0].Trim();
                    var callee = parts[1].Substring(0, parts[1].IndexOf(":")).Trim();

                    // Asegurar que cada participante tenga un alias único
                    if (!participantAliases.ContainsKey(caller))
                    {
                        participantAliases[caller] = $"P{participantId++}";
                        stringBuilder.AppendLine($"participant {participantAliases[caller]} as \"{caller}\"");
                    }
                    if (!participantAliases.ContainsKey(callee))
                    {
                        participantAliases[callee] = $"P{participantId++}";
                        stringBuilder.AppendLine($"participant {participantAliases[callee]} as \"{callee}\"");
                    }
                }
            }

            // Agregar las llamadas utilizando los alias
            foreach (var call in walker.MethodCalls)
            {
                var parts = call.Split(new string[] { " -> " }, StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    var caller = parts[0].Trim();
                    var callee = parts[1].Substring(0, parts[1].IndexOf(":")).Trim();
                    var methodDetails = parts[1].Substring(parts[1].IndexOf(":") + 1).Trim();

                    stringBuilder.AppendLine($"{participantAliases[caller]} -> {participantAliases[callee]}: {methodDetails}");
                }
            }

            stringBuilder.AppendLine("@enduml");
            return stringBuilder.ToString();
        }

    }
}
