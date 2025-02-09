using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzeCode.MoreComplex
{
    using System;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.MSBuild;

    public class InvocationAnalyzer
    {
        private readonly Compilation _compilation;

        public InvocationAnalyzer(Compilation compilation)
        {
            _compilation = compilation;
        }

        public ITypeSymbol GetInvocationReturnType(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            {
                return methodSymbol.ReturnType;
            }
            return null;
        }

        public MethodDeclarationSyntax FindMethodInProject(string nameSpaceName, string methodName, string className,
            Compilation compilation)
        {
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var root = syntaxTree.GetRoot();
                var method = root.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m =>
                        m.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString() ==
                        nameSpaceName &&
                        m.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.Text == className &&
                        m.Identifier.Text == methodName);

                if (method != null)
                {
                    return method;
                }
            }

            return null;
        }
    }

}
