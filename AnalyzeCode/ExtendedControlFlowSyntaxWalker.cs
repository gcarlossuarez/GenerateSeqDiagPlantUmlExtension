using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AnalyzeCode
{
    public class ExtendedControlFlowSyntaxWalker : CSharpSyntaxWalker
    {
        public int IfCount { get; private set; } = 0;
        public int LoopCount { get; private set; } = 0;
        public List<string> MethodCalls { get; private set; } = new List<string>();

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            IfCount++;
            base.VisitIfStatement(node);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            LoopCount++;
            base.VisitForStatement(node);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            LoopCount++;
            base.VisitWhileStatement(node);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            LoopCount++;
            base.VisitDoStatement(node);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            LoopCount++;
            base.VisitForEachStatement(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var caller = "Unknown";
            var argumentList = new StringBuilder();

            // Determinar el caller basado en el contexto
            var parentMethod = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (parentMethod != null)
            {
                var className = parentMethod.Parent is ClassDeclarationSyntax classDecl ? classDecl.Identifier.ToString() : "Unknown";
                caller = parentMethod.Modifiers.Any(SyntaxKind.StaticKeyword) ? className : className + " instance";
            }

            var callee = caller;  // Default to caller if no specific callee is detected
            var methodCallee = string.Empty;
            var expressionParts = node.Expression.ToString().Split('.');

            if (expressionParts.Length == 1)
            {
                methodCallee = expressionParts[0];  // Method of the current class
            }
            else
            {
                var firstPart = expressionParts[0];
                callee = (firstPart == "this" || firstPart == "base") ? caller : firstPart;
                methodCallee = expressionParts.Last();
            }

            foreach (var argument in node.ArgumentList.Arguments)
            {
                argumentList.Append($"{argument}, ");
            }

            if (argumentList.Length > 0)
            {
                argumentList.Remove(argumentList.Length - 2, 2);
            }

            var methodCallWithArguments = $"{caller} -> {callee}: {methodCallee}({argumentList})";
            MethodCalls.Add(methodCallWithArguments);

            base.VisitInvocationExpression(node);
        }

    }
}