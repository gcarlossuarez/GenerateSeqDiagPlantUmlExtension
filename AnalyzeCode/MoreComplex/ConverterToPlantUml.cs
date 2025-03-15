using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnalyzeCode.MoreComplex.Token;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AnalyzeCode.MoreComplex
{
    class ConverterToPlantUml
    {
        /// <summary>
        /// Generate PlantUML code from the given syntax walker.
        /// 1) Define participants with aliases if necessary.
        /// 2) Ensure that each participant has a unique alias.
        /// 3) Add all actions directly into the diagram.
        /// 4) Return the PlantUML code as a string.
        /// For more details see: https://mermaid.live/edit#pako:eNq9Vm1v2jAQ_iueP7VSigjQhkUTGlu7qVo7TeuqSVO-uPEB1oydOZdRWvW_72JDgZJ1fZnqD4nx-Z57e47LNc-tBJ7yEn5VYHI4VGLsxDQzjFYhHKpcFcIgOy_BbZ--t-Y3OAT3zX7RdHA-1duXji4RjARJl9FZ_UHb2dncoLj8LvTPJtQzdMqM31VKyybxocpRWSPcPDNBWju3Nxg0eJOyj2DACYTlyc7MW90Nmg0qBLThQMqOjUIltLqCoLQh3r4-LAqK90QZ2Mn42xLJ9Yrywu81uQpq3d563EOtRAnl80CO5TJnny0C0zBCZkdNcCxlR6asHDCcCGQg8slGFd5cuMFElEywyigiDxO1fwFbW1uwkXVBKxdaM2VYSHxr6H1c3Px7LI3FPCu0wgB4MWcZZ3sDeq6ghEbvZNk6ATPGCRuweCV9pDHirRN5MAeOCSPDFjYRa5uvtuvUqukulCk_wXwnQOxuKtZrVbE75RtKuTQ8UxTIdpLX18MZuV5CCSNVs8QaX6xGJ0nvieHCk8KFlw0X7gv39offPLht6lhqhopAdCaVgxz1nHoALXUT-N6R6_-0mw0TFP9ryywgX7ppokWWI989U8CJlYdAPNF3qvrwii4i8SwRgXvb4GtVBV3C82zt3s-IR00D2v57FtTjLGVfAStnWDg_PWGlx80Mj_gU3FQoSXP7usbJOJFqChlPaUssF5XGusA3dFVUaGna5jxFV0HEna3GE56OBKUl4lUhaTQuhv7tKXULT6_5JU_jXr_V7nT7ST_pdJN4vxvxOU_34v1OK3mddDqdfXp12-2Dm4hfWUsQcSvpdeNechDHB3G_3Yv7Hu-HFwYXQCq07jR8ePjvj6UjR16y8OPmDyuF4GI
        /// </summary>
        /// <param name="walker"></param>
        /// <param name="specifyDpi300"></param>
        /// <returns></returns>
        public string GeneratePlantUml(ExtendedControlFlowSyntaxWalker walker, bool specifyDpi300)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("@startuml");
            stringBuilder.AppendLine(@"skinparam sequence {
    LifeLineBackgroundColor lightblue
}");

            if (specifyDpi300)
            {
                // This will help messages in diagrams not get cut off and scale correctly.
                stringBuilder.AppendLine("skinparam dpi 300");
                stringBuilder.AppendLine("skinparam maxMessageSize 100");
            }

            // Define participants with aliases if necessary
            Dictionary<string, string> participantAliases = new Dictionary<string, string>();
            int participantId = 1;
            string[] separators =
            {
                ExtendedControlFlowSyntaxWalker.THROW_EXCEPTION_ARROW,
                ExtendedControlFlowSyntaxWalker.DOUBLE_ARROW, 
                ExtendedControlFlowSyntaxWalker.ARROW,
                //ExtendedControlFlowSyntaxWalker.NOTE_OVER,
            };
            foreach (var callToken in walker.Actions)
            {
                if (callToken.GetType().Name == new ComplexToken().GetType().Name)
                {
                    ComplexToken complexToken = callToken as ComplexToken;
                    string caller = complexToken?.BaseTokenType.Caller;
                    string called = complexToken?.BaseTokenType.Called;
                    participantId = CreateParticipantIds(caller, called);
                }
                else if (callToken.GetType().Name == new StringToken().GetType().Name)
                {
                    string call = callToken is StringToken ? callToken.ToString() : string.Empty;

                    for (int i = 0; i < separators.Length; ++i)
                    {
                        var parts = call.Split(new string[] { separators[i] }, StringSplitOptions.None);
                        if (parts.Length > 1)
                        {
                            var caller = parts[0].Trim();
                            var called = parts[1].Substring(0, parts[1].IndexOf(":", StringComparison.Ordinal)).Trim();

                            participantId = CreateParticipantIds(caller, called);

                            break;
                        }
                    }
                }
            }
            int CreateParticipantIds(string caller, string called)
            {
                if (!participantAliases.ContainsKey(caller))
                {
                    participantAliases[caller] = $"P{participantId++}";
                    stringBuilder.AppendLine($"participant {participantAliases[caller]} as \"{caller}\"");
                }

                if (!participantAliases.ContainsKey(called))
                {
                    participantAliases[called] = $"P{participantId++}";
                    stringBuilder.AppendLine($"participant {participantAliases[called]} as \"{called}\"");
                }

                return participantId;
            }

            // Add all actions directly into the diagram
            // Replace the caller and callee with it's aliases for every call
            foreach (var actionToken in walker.Actions)
            {
                if (actionToken.GetType().Name == new ComplexToken().GetType().Name)
                {
                    ComplexToken complexToken = actionToken as ComplexToken;
                    if (complexToken != null)
                    {
                        stringBuilder.AppendLine(
                            $"{participantAliases[complexToken.BaseTokenType.Caller]} {complexToken.BaseTokenType.Arrow} {participantAliases[complexToken.BaseTokenType.Called]}: {complexToken.BaseTokenType.Invocated}");
                    }
                }
                else if (actionToken.GetType().Name == new SymplexToken().GetType().Name)
                {
                    SymplexToken symplexToken = actionToken as SymplexToken;
                    if (symplexToken != null)
                    {
                        stringBuilder.AppendLine(
                            $"{ExtendedControlFlowSyntaxWalker.NOTE_OVER} {participantAliases[symplexToken.NoteOverTokenType.Left]} {symplexToken.NoteOverTokenType.Operator} {symplexToken.NoteOverTokenType.Right}");
                    }
                }
                else if (actionToken.GetType().Name == new StringToken().GetType().Name)
                {
                    string action = actionToken.ToString();
                    bool founded = false;
                    
                    // Replace the callee with it's alias if action is activate
                    var parts = action.Split(new string[] { ExtendedControlFlowSyntaxWalker.ACTIVATE }, StringSplitOptions.None);
                    if (parts.Length > 1 && action.StartsWith(ExtendedControlFlowSyntaxWalker.ACTIVATE)) // To avoid a confussion with "deactivate"
                    {
                        founded = true;
                        var called = parts[1];
                        stringBuilder.AppendLine(
                            $"{ExtendedControlFlowSyntaxWalker.ACTIVATE}{participantAliases[called]}");
                    }

                    if (!founded)
                    {
                        // Replace the callee with it's alias if action is deactivate
                        parts = action.Split(new string[] { ExtendedControlFlowSyntaxWalker.DEACTIVATE }, StringSplitOptions.None);
                        if (parts.Length > 1 && action.StartsWith(ExtendedControlFlowSyntaxWalker.DEACTIVATE))
                        {
                            founded = true;
                            var called = parts[1];
                            stringBuilder.AppendLine($"{ExtendedControlFlowSyntaxWalker.DEACTIVATE}{participantAliases[called]}");
                        }
                    }

                    if (!founded)
                    {
                        if (action.Trim().StartsWith(ExtendedControlFlowSyntaxWalker.DESTROY))
                        {
                            founded = true;
                            String called = action.Trim().Replace(ExtendedControlFlowSyntaxWalker.DESTROY, string.Empty);
                            stringBuilder.AppendLine(
                                $"{ExtendedControlFlowSyntaxWalker.DESTROY} {participantAliases[called.Trim()]}");
                        }
                    }

                    if (!founded) stringBuilder.AppendLine($"{action}");
                }
                
            }

            stringBuilder.AppendLine("@enduml");
            return stringBuilder.ToString();

        }

    }
}
