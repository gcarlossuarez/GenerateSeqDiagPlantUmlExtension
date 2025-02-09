using System;
using System.Data;
using System.Linq;
using System.Text;

namespace AnalyzeCode.MoreComplex
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System.Collections.Generic;
    using System.Diagnostics.Eventing.Reader;
    using System.Runtime.Remoting.Messaging;

    /// <summary>
    /// This diagram illustrates the flow of the ExtendedControlFlowSyntaxWalker class as it processes a C# syntax tree.
    /// It starts by visiting a MethodDeclarationSyntax node, pushes the method name onto a context stack, and then
    /// recursively visits its child nodes. When it encounters an InvocationExpressionSyntax node (which represents a
    /// method call), it determines the caller and callee, adds the method call with arguments to the Actions list, and
    /// if the called method is defined within the same syntax tree, it recursively visits that method's declaration. For
    /// control flow statements like if, for, while, do-while, and foreach, it adds corresponding actions to the Actions
    /// list and processes their child nodes accordingly. The context stack is used to keep track of the current position
    /// in the syntax tree and is popped as the walker backtracks after visiting nodes.
    /// https://mermaid.live/edit#pako:eNqFVNFu2jAU_RXLz4AIhIbmYRMk0NIVSgeatCU8WLEh1hIb2c5Ki_j3OZekSjvQeEDge865557r5IgTSRn28U6RfYrWYSyQ_YyilSHKbFC7_QWNox9cc4PmzKSShizJiCKGS7F6FYYc0MIqbM68MRCCaFnotMKjBckZksJIFNhvdjDIaie_K0YAjLBqEaQ8oyCoq3oI9clxphtFRASaiT8yARuTw14xrd8NfT2dqRNLRT-ZBoVp1eE6rTnHFDh3UcgMUzkXDAUky5hCY6IZtdPUs1T4O8Dff8aXRmmdA5zQinAPhFk0oh_q6IWbFI3UrsiZMBrZ0EZJaVajR67rbjMgP0AmIFpLhGxre1PEBTIpQ6sy-Wq4tWKsDuahGcy3_2xXbj822TQ0FhIkHqOJHdMC1ynXaElMumlsoALNP68QIlQyQ9NMvpR3wrBy6NrkvGlyUd-Pi5Tm5haAf4JgA6nshvdSUC52VZBXIn0C2vLfawjzX-xaMZfAfI6Wcn_xgs8bGVxHPUP9exmkPYkFbuHc3iTCqX02jyUmxnalOYuxb39StiVFZmIci5OFksJIu64E-0YVrIWVLHYp9rck0_ZfsafWcsiJfcbz99M9Edg_4gP2Ha8zHPY81x14Xs_p9rpuC79iv93v3nQct-e4g9vBrdt33ZtTC79JaTV6nf7Q6zqe03W9vuM63gAEf0Hx7IFRbqSan98u8JKpnUygUhk5_QU92W6f
    /// </summary>
    public class ExtendedControlFlowSyntaxWalker : CSharpSyntaxWalker
    {
        private const string UNKNOWN = "UNKNOWN";
        private const string UNRESOLVED = "[UNRESOLVED]";

        public const string THROW_EXCEPTION_ARROW = "--[#red]->";

        public const string DESTROY = "destroy";

        public const string DOUBLE_ARROW = "-->";

        public const string ARROW = "->";

        public const string ACTIVATE = "activate ";

        public const string DEACTIVATE = "deactivate ";

        public const string NOTE_OVER = "note over ";

        private const string TRY_GROUP = "group try";

        private const string CATCH_GROUP = "group catch";

        private const string FINALLY_GROUP = "group finally";

        private const string ALT = "alt";

        private const string ALT_CASE = "alt case";

        private const string LOCK_GROUP = "group lock";

        /// <summary>
        /// The maximum depth to visit in the syntax tree.
        /// </summary>
        private int _maxDept;

        /// <summary>
        /// The current depth in the syntax tree.
        /// </summary>
        private int _countDept = 0;

        /// <summary>
        /// The initial method visited.
        /// </summary>
        private MethodDeclarationSyntax _initialMethodDeclarationSyntax;

        /// <summary>
        /// Set to true when a method is visited to avoid infinite loops.
        /// </summary>
        private HashSet<MethodSignature> _visitedMethods = new HashSet<MethodSignature>();

        /// <summary>
        /// Stack to keep track of the current switch statement being visited.
        /// </summary>
        private Stack<SwitchStatementInfo> _switchStatementStack = new Stack<SwitchStatementInfo>();

        /// <summary>
        /// Stack to keep track of the current assignment being visited.
        /// </summary>
        private StackAsignmentInfo _asignmentInfoStack = new StackAsignmentInfo();

        /// <summary>
        /// Stack to keep track of the current invocation expression being visited.
        /// </summary>
        private Stack<Tuple<InvocationExpressionSyntax, string, MethodDeclarationSyntax>>
            _invocationExpressionSyntaxesStack =
                new Stack<Tuple<InvocationExpressionSyntax, string, MethodDeclarationSyntax>>();

        private Stack<Tuple<CatchClauseSyntax, string>> _rethrownExceptionsStack =
            new Stack<Tuple<CatchClauseSyntax, string>>();

        /// <summary>
        /// Stack to keep track of the current try statement being visited.
        /// </summary>
        private Stack<Tuple<Tuple<InvocationExpressionSyntax, string, MethodDeclarationSyntax>, TryStatementSyntax>>
            _tryStatementSyntaxesStack =
                new Stack<Tuple<Tuple<InvocationExpressionSyntax, string, MethodDeclarationSyntax>,
                    TryStatementSyntax>>();

        /// <summary>
        /// The actions performed by the syntax walker, whose will be translated to PlantUml
        /// </summary>
        public List<string> Actions { get; private set; } = new List<string>();

        /// <summary>
        /// The compilation object used to resolve method calls.
        /// </summary>
        private readonly CSharpCompilation _compilation;

        /// <summary>
        /// The semantic model used to resolve method calls.
        /// </summary>
        private readonly SemanticModel _semanticModel;


        public ExtendedControlFlowSyntaxWalker(CSharpCompilation compilation, SemanticModel semanticModel, int maxDept)
        {
            _compilation = compilation;
            _semanticModel = semanticModel;
            _maxDept = maxDept;
        }

        /// <summary>
        /// Normally, all the process inits here.
        /// </summary>
        /// <param name="node"></param>
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Download all the assignments in Stack to Actions list.
            var caller = GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1);
            DownloadStackAssignmentToActionListAndAssignCaller(caller: caller != UNKNOWN ? caller : string.Empty);

            if(_initialMethodDeclarationSyntax == null) _initialMethodDeclarationSyntax = node;

            //var methodName = node.Identifier.Text;
            (string methodName, string parameters) = GetMethodSignature(node);
            (int line, int column) = GetNodeLocation(node);
            MethodSignature methodSignature = new MethodSignature(methodName, parameters, line, column);
            if (_visitedMethods.Contains(methodSignature))
            {
                // Avoid recursive calls that generate infinite loops
                //return;
            }

            _visitedMethods.Add(methodSignature);

            // Base method is called to avoid interrupt the syntax Analisys
            base.VisitMethodDeclaration(node);
        }

        /// <summary>
        /// Called when the visitor visits a InvocationExpressionSyntax node.
        /// See https://mermaid.live/edit#pako:eNqNVGtP2zAU_SuWPxUpVEtaFIg0JqAFiigUCp20ZUImdhqLxI4cp-X533d9WxLYYFq_1Mo959xzH_YTTTQXNKJprpdJxowlV4NYEfjtdWI6tfAlphtkc3OX7P-M6UEmkjtiRFKbSmpFuChtRjozYdIHqebkJtG1sgMhyo2Y_loJ7Tv282uktGT3K7kp2L07P5MDUL0UtjbqD8Iwr8QzGUL4SFhSCJtpDvmSnBlmIXcDH6K7w8adVKlU0orWZgM9ROVR1WC-gYE3IXLog8xIJUYUQlnSmm4lfAQeAW4grDCFVIIkLM-FaTBHCDnGyiqdL9YATsB8VTWwY4SNnJJeqlwzTiAs58rlrkhqdEEqy5K7hjFCxsn7njgGgwaKBnaCsFOA7XFOrCY3C1lBtXyMjNbBKQLHAJzUVQZdWegEm0u0Atr75GMEnwF4v5Y5eDXzGruUy6rtzxmiztGiEjAq8erT9QBmUhpRAev9DM-RNVk7ZomVi5UPd4S_pYQ1Qx31oZDnqpzkTNnr8elnSSaY5MItilO4ZZXozlxjRk3dw3vHdDvTUXAx2iW-QO4lcJHxwT56q4GtA-tWwsw7q9lveITIlLAFkzm7zdthXaLydF06F38X_x-lTVHk6rNVYnMmW_AVgq_d1HX5dugfbNw1Yme47f-4FTOEfYcnY6g4PBixoh4t4HYwyeF1eXKomNoMBGIawZGLlNU5CMTqBaCstnr6oBIaWVMLj9Ylh-YNJJsbVtAoZfAYeLRkikZP9J5GO9vdrX7YC8PA39oOtvqBRx9otOn3_bAbwLfgy04IAf_Fo49ag0Kv6wd-2AuA1At7fb_3mmPIpdXmbYofSFj5MLqeZ01QIHa8ejHx4Xz5DTuUryE
        /// </summary>
        /// <param name="node"></param>
        public override void VisitInvocationExpression(Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax node)
        {
            if (_countDept >= _maxDept)
            {
                return;
            }
            
            var methodDeclarationCurrentInvocation = HelperSyntaxWalker.GetMethodDeclarationSyntax(node, _compilation);

            if (IsRecursiveInfiniteCall(node, methodDeclarationCurrentInvocation)) return;

            ++_countDept;

            // Determine caller
            var caller = GetCaller(node);

            // Resolve the called class
            var called = caller;  // Default to caller if no specific callee is detected
            var methodCallee = GetMethodCalled(node, caller, ref called);
            
            // Download all the assignments in Stack to Actions list and assign the new caller.
            DownloadStackAssignmentToActionListAndAssignCaller(caller);

            (string methodName, string parameters, List<TypeInfo> argumentsTypeInfo) = GetMethodSignature(node);
            (int line, int column) = GetNodeLocation(node);
            MethodSignature methodSignature = new MethodSignature(methodName, parameters, line, column);
            if (_visitedMethods.Contains(methodSignature))
            {
                //
            }

            _visitedMethods.Add(methodSignature);

            _invocationExpressionSyntaxesStack.Push(Tuple.Create(node, called, methodDeclarationCurrentInvocation));

            //// Base method is called to avoid interrupt the syntax Analysis
            //base.VisitInvocationExpression(node);

            var argumentList = new StringBuilder();

            // (1*) Set the arguments of the method call
            int i = 0;
            foreach (var argument in node.ArgumentList.Arguments)
            {
                var unresolvedArgument = argumentsTypeInfo[i++].ConvertedType;

                // NOTE. - If one of the arguments could not be resolved, you will not be able to find its definition
                // in the syntax tree, since the data type will be unknown; therefore, you will not be able to visit
                // its method declaration to analyze it. It was decided to mark it as [UNRESOLVED], to take it into
                // account. This can happen if a data type is in a file of a project or external DLL.
                string strUnresolved = string.IsNullOrEmpty(unresolvedArgument?.Name) ? UNRESOLVED : string.Empty;
                argumentList.Append($"{strUnresolved}{argument}, ");
            }

            if (argumentList.Length > 0)
            {
                argumentList.Remove(argumentList.Length - 2, 2); // Artifice to avoid problems
            }

            var methodCallWithArguments = $"{caller} {ARROW} {called}: {methodCallee}({DeleteEnterAndCarriageReturnCharacters(argumentList.ToString())})";
            Actions.Add(methodCallWithArguments);
            Actions.Add($"{ACTIVATE}{called}");

            // Base method is called to avoid interrupt the Syntax Analisys
            base.VisitInvocationExpression(node);

            // Resolve the called method
            // NOTE. - See (1*) Set the arguments of the method call)
            if (methodDeclarationCurrentInvocation != null)
            {
                Visit(methodDeclarationCurrentInvocation);
            }
            
            Actions.Add($"{DEACTIVATE}{called}");

            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(caller);

            _invocationExpressionSyntaxesStack.Pop();

            --_countDept;
        }

        /// <summary>
        /// Check if the method is a recursive infinite call.
        /// See https://mermaid.live/edit#pako:eNp1U8Fum0AQ_ZXRnFoJW5jYDnBo1QTiuFJzqHtq6GEFQ0CFXbQsblzjf8-wNLaVuHtYrWbee_N2dnaPqcoIQ8wr9ScthDbwI0ok8PryIcGN4UiCH2Ey-QQ3jwneFpT-hjKH1ojh0ALVjdkl-Gsk3QzIPh5iPdwy4TuZTkvIRdXSG9SDMiO7h4iRa0NaGAK1JQ2Npm2puhZKuVWpMKWS7ZEeWTt9_Fy2pr0I7WMWXJGBmkyhMsgorYS2OVD5JcpRPB69dVX1mX2dxeBuuL-qG6HpVVeKmk6-7iw1KvOcNElz4tsErM74rep0SjA0H6p3F1xZoQ2LH5Msdn9qp9EdvUFfKPuvTw8KasU1LzZq_b83urfkrzwEscx4BMboeowmEh2sSdeizHh49kMyQVNQzQIhHzPKRVfx6CTywFDRGbXZyRTDwbmDXZPxU0eleNKixtBWdrAREsM9PmPoToPAn7vzxcL3g2WwcH0HdxhOrr35dLZ0Z8sr1_fda29xcPCvUizhTWfe0g-8K8_3lrwHr0XirDRKn9f4aQmjEa26p-KYJIv9Nv4I-zEOL8YU_gI
        /// </summary>
        /// <param name="node"></param>
        /// <param name="methodDeclarationCurrentInvocation"></param>
        /// <returns></returns>
        private bool IsRecursiveInfiniteCall(InvocationExpressionSyntax node,
            MethodDeclarationSyntax methodDeclarationCurrentInvocation)
        {
            if (_invocationExpressionSyntaxesStack.Count > 0)
            {
                foreach (var previousInvocation in _invocationExpressionSyntaxesStack)
                {
                    var methodDeclarationPreviosInvocation = previousInvocation.Item3;
                    if (methodDeclarationPreviosInvocation is null || methodDeclarationCurrentInvocation is null)
                    {
                        continue;
                    }

                    // Comparación por nombre del método
                    if (methodDeclarationPreviosInvocation.Identifier.Text != methodDeclarationCurrentInvocation.Identifier.Text)
                    {
                        continue;
                    }

                    // Comparación por ubicación del nodo
                    var prevSpan = previousInvocation.Item1.GetLocation().GetLineSpan();
                    var currentSpan = node.GetLocation().GetLineSpan();

                    // If you have method overloading (same name but different parameters), there will be no confusion
                    // because each one will have a different location in the source code. Since you are validating
                    // that it is the same file with prevSpan.Path == currentSpan.Path and
                    // since GetLocation().GetLineSpan() returns the exact location of the node in the file, two
                    // methods with the same name but different signatures will never be on the same line, so the
                    // detection remains accurate.
                    if (prevSpan.Path == currentSpan.Path &&
                        prevSpan.StartLinePosition == currentSpan.StartLinePosition &&
                        prevSpan.EndLinePosition == currentSpan.EndLinePosition)
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// Get the caller of the method or the instruction.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method determines the caller based on the context of the given node.
        /// It first sets the caller to UNKNOWN.
        /// Then, it gets the parent method of the node.
        /// If the parent method is not null, it sets the caller to the result of GetCallerFromMethod with the parent method.
        /// Finally, it returns the caller.
        /// Method GetCaller(node)
        ///     Set caller to UNKNOWN
        ///     Get the parent method of the node
        ///     If parent method is not null
        ///         Set caller to the result of GetCallerFromMethod with the parent method
        ///     Return caller
        /// </remarks>
        private string GetCaller(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxNode node)
        {
            var caller = UNKNOWN;
            
            // Determine caller based on context
            var parentMethod = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (parentMethod != null)
            {
                caller = GetCallerFromMethod(parentMethod);
            }

            return caller;
        }


        /// <summary>
        /// Checks if the method signature of a method A declaration matches the method signature of a method B declaration.
        /// </summary>
        /// <param name="methodDeclarationSyntaxMethodA"></param>
        /// <param name="methodSignatureMethodB"></param>
        /// <param name="argumentsTypeInfoMethodB"></param>
        /// <returns></returns>
        private bool AreMethodsEquivalent(MethodDeclarationSyntax methodDeclarationSyntaxMethodA,
            MethodSignature methodSignatureMethodB, List<TypeInfo> argumentsTypeInfoMethodB)
        {
            // Checks identifiers, parameters, and return types.
            if (methodDeclarationSyntaxMethodA.Identifier.ValueText != methodSignatureMethodB.Name)
            {
                return false;
            }

            var parameterTypesA =
                methodDeclarationSyntaxMethodA.ParameterList.Parameters.Select(arg =>
                    $"{GetArgumentModifier(arg)}{arg.Type}");
            //var parameterTypesB = methodB.Parameters.Split(',').ToList().Select(x=> x.Trim());
            //var t = methodDeclarationSyntaxMethodA.ParameterList.Parameters[0].Modifiers.GetType();

            //if (methodDeclarationSyntaxMethodA.ParameterList.Parameters[0].Modifiers.FirstOrDefault().Text == "out")
            //{

            //}
            //bool equivalent = parameterTypesA.SequenceEqual(parameterTypesB);
            //return equivalent;

            if (!ArgumentsTypeAreEquals(methodDeclarationSyntaxMethodA, argumentsTypeInfoMethodB)) return false;

            return true;
        }

        private bool ArgumentsTypeAreEquals(MethodDeclarationSyntax methodDeclarationSyntaxMethodA,
            List<TypeInfo> argumentsTypeInfoMethodB)
        {
            int posParameter = 0;
            foreach (var typeSyntaxA in methodDeclarationSyntaxMethodA.ParameterList.Parameters.Select(x => x.Type))
            {
                bool sw = false;
                foreach (var typeInfoB in argumentsTypeInfoMethodB.Select(x => x.Type)
                             .Skip(posParameter++))
                {
                    if (AreTypesEquivalent(typeSyntaxA, typeInfoB)) sw = true;

                    break;
                }

                if (!sw) return false;
            }

            posParameter = 0;
            foreach (var typeInfoB in argumentsTypeInfoMethodB.Select(x => x.Type))
            {
                bool sw = false;
                foreach (var typeSyntaxA in methodDeclarationSyntaxMethodA.ParameterList.Parameters.Select(x => x.Type)
                             .Skip(posParameter++))
                {
                    if (AreTypesEquivalent(typeSyntaxA, typeInfoB)) sw = true;

                    break;
                }

                if (!sw) return false;
            }

            return true;
        }

        private SemanticModel GetSemanticModel(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxNode referenceNode)
        {
            var syntaxTree = referenceNode.SyntaxTree;
            var semanticModel = _compilation.GetSemanticModel(syntaxTree);
            return semanticModel;
        }

        private bool AreTypesEquivalent(Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax typeSyntax1,
            Microsoft.CodeAnalysis.ITypeSymbol type2)
        {
            var typeInfo1 = GetSemanticModel(typeSyntax1).GetTypeInfo(typeSyntax1);

            if (typeInfo1.Type == null || type2 == null)
            {
                return false;
            }

            return typeInfo1.Type.Equals(type2);
        }


        private (string, string) GetMethodSignature(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            var methodName = methodDeclarationSyntax.Identifier.Text;
            var argumentTypes = methodDeclarationSyntax.ParameterList.Parameters
                .Select(arg => $"{GetArgumentModifier(arg)}{arg.Type}")
                .ToArray();
            return (methodName, string.Join(", ", argumentTypes));
        }

        /// <summary>
        /// Gets the signature of a method.
        /// NOTE.- For a more precise analysis, "SemanticModel" is used.
        /// </summary>
        /// <param name="invocation"></param>
        /// <returns></returns>
        private (string, string, List<TypeInfo>) GetMethodSignature(
            Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax invocation)
        {
            var methodName = invocation.Expression.ToString();
            var argumentTypes = invocation.ArgumentList.Arguments
                .Select(arg => $"{GetArgumentModifier(arg)}{GetTypeOfExpression(arg.Expression)}")
                .ToArray();
            //var argumentTypeInfo= invocation.ArgumentList.Arguments
            //    .Select(arg => _semanticModel.GetTypeInfo(arg))
            //    .ToArray();
            List<TypeInfo> listTypeInfo = new List<TypeInfo>();
            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                listTypeInfo.Add(GetSemanticModel(argument).GetTypeInfo(argument.Expression));
            }

            return (methodName, string.Join(", ", argumentTypes), listTypeInfo);
        }

        private string GetArgumentModifier(Microsoft.CodeAnalysis.CSharp.Syntax.ArgumentSyntax argument)
        {
            return GetArgumentModifier(argument.RefOrOutKeyword.Kind());
        }

        private string GetTypeOfExpression(Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax expression)
        {
            // Assume 'referenceNode' is the SyntaxNode you want to analyze
            var syntaxTree = expression.SyntaxTree;
            var semanticModel = _compilation.GetSemanticModel(syntaxTree);

            var typeInfo = semanticModel.GetTypeInfo(expression);
            var symbolInfo = semanticModel.GetSymbolInfo(expression);

            if (typeInfo.Type != null)
            {
                return typeInfo.Type.ToDisplayString();
            }

            if (symbolInfo.Symbol is ILocalSymbol localSymbol)
            {
                return localSymbol.Type.ToDisplayString();
            }

            return "UnknownType";
        }


        //private string GetArgumentModifier(ArgumentSyntax argument)
        //{
        //    return GetArgumentModifier(argument.RefOrOutKeyword);
        //    //switch (argument.RefOrOutKeyword.Kind())
        //    //{
        //    //    case SyntaxKind.RefKeyword:
        //    //        return "ref ";
        //    //    case SyntaxKind.OutKeyword:
        //    //        return "out ";
        //    //    default:
        //    //        return string.Empty;
        //    //}
        //}

        private string GetArgumentModifier(Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax argument)
        {
            var arg = argument.Modifiers.FirstOrDefault();

            return GetArgumentModifier(arg.Kind());
            //switch (arg.Text.ToLower().Trim())
            //{
            //    case "ref":
            //        return "ref ";
            //    case "out":
            //        return "out ";
            //    default:
            //        return string.Empty;
            //}
        }

        private static string GetArgumentModifier(SyntaxKind arg)
        {
            switch (arg)
            {
                case SyntaxKind.RefKeyword:
                    return "ref ";
                case SyntaxKind.OutKeyword:
                    return "out ";
                default:
                    return string.Empty;
            }
        }


        private (int Line, int Column) GetNodeLocation(SyntaxNode node)
        {
            var lineSpan = node.GetLocation().GetLineSpan();
            var linePosition = lineSpan.StartLinePosition;
            return (linePosition.Line + 1, linePosition.Character + 1); // +1 para que las líneas y columnas empiecen desde 1 en lugar de 0
        }

        /// <summary>
        /// Get the method called and the caller.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="caller"></param>
        /// <param name="called"></param>
        /// <returns></returns>
        /// <remarks>
        /// GetMethodCalled(node, caller, ref called) method
        ///     Split node.Expression into parts using dot as separator
        ///     If length of parts is 1
        ///         Return first part(method of current class)
        ///     Call GetCallerFromExpression with node and caller to get value of called
        ///     Return last part of parts(name of called method)
        /// </remarks>
        private string GetMethodCalled(Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax node,
            string caller, ref string called)
        {
            var expressionParts = node.Expression.ToString().Split('.');

            if (expressionParts.Length == 1)
            {
                return expressionParts[0]; // Method of the current class
            }

            called = GetCallerFromExpression(node, caller);

            return expressionParts.Last();
        }

        /// <summary>
        /// Get the caller of the method from the method invocation.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="caller"></param>
        /// <returns></returns>
        private string GetCallerFromExpression(InvocationExpressionSyntax node, string caller)
        {
            var expressionParts = node.Expression.ToString().Split('.');
            var firstPart = expressionParts[0];
            if (firstPart.Trim().Length == 0)
            {
                firstPart = ResolveFirstPart(node, firstPart);
            }

            return (firstPart == "this" || firstPart == "base") ? caller : firstPart;
        }


        /// <summary>
        /// Resolve the first part of the expression.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="firstPart"></param>
        /// <returns></returns>
        private string ResolveFirstPart(Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax node, string firstPart)
        {
            if (node.Expression is MemberBindingExpressionSyntax memberBindingExpressionSyntax)
            {
                foreach (var syntaxNode in memberBindingExpressionSyntax.Ancestors()
                             .Where(x => x is Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalAccessExpressionSyntax))
                {
                    string expression = ProcessExpression((Microsoft.CodeAnalysis.CSharp.Syntax.ConditionalAccessExpressionSyntax)syntaxNode);
                    string[] arrExpression = expression.Split('.');
                    if (arrExpression.Length > 1)
                    {
                        firstPart = arrExpression[0].Substring(0, arrExpression[0].Length - 1);
                    }

                    break;
                }
            }
            else if (node.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
            {
                foreach (var syntaxNode in memberAccessExpressionSyntax.Ancestors()
                             .Where(x => x is ConditionalAccessExpressionSyntax))
                {
                    string expression = ProcessExpression((ConditionalAccessExpressionSyntax)syntaxNode);
                    string[] arrExpression = expression.Split('.');
                    if (arrExpression.Length > 1)
                    {
                        firstPart = arrExpression[0].Substring(0, arrExpression[0].Length - 1);
                    }

                    break;
                }
            }

            return firstPart;
        }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            // To avoid problems when rendering enters, for multi-line conditions, it is replaced with white space
            Actions.Add($"{ALT} {DeleteEnterAndCarriageReturnCharacters(node.Condition.ToString())}"); // Marcar inicio del bloque 'if'

            // Visitar el bloque 'then'
            Visit(node.Statement);

            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            if (node.Else != null)
            {
                Actions.Add("else"); // Marcar inicio del bloque 'else'

                // Visitar el bloque 'else'
                Visit(node.Else.Statement);

                // Download all the assignments in Stack to Actions list.
                DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));
            }

            Actions.Add("end"); // Fin del bloque 'if' o 'else'
        }

        /// <summary>
        /// Called when the visitor visits a WhileStatementSyntax node.
        /// </summary>
        /// <param name="node"></param>
        public override void VisitContinueStatement(ContinueStatementSyntax node)
        {
            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            var (className, methodName) = GetMethodAndClass(node);
            var loopType = GetLoopType(node); // Determinar en qué bucle está el continue

            // Agregar al diagrama de secuencia
            //Actions.Add($"{className}.{methodName} : continue in {loopType}");
            Actions.Add($"{className} {DOUBLE_ARROW} {className} : Continue next iteration in {loopType}");

            base.VisitContinueStatement(node);
        }

        /// <summary>
        /// Get loop type from CSharpSyntaxNode  
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private string GetLoopType(CSharpSyntaxNode node)
        {
            var loop = node.Ancestors().FirstOrDefault(n =>
                n is ForStatementSyntax ||
                n is WhileStatementSyntax ||
                n is DoStatementSyntax ||
                n is ForEachStatementSyntax);

            if (loop is ForStatementSyntax)
            {
                return "for-loop";
            }

            if (loop is WhileStatementSyntax)
            {
                return "while-loop";
            }

            if (loop is DoStatementSyntax)
            {
                return "do-while-loop";
            }

            if (loop is ForEachStatementSyntax)
            {
                return "foreach-loop";
            }
            
            return "Unknown loop";
        }

        /// <summary>
        /// Called when the visitor visits a PrefixUnaryExpression node.
        /// Visit node is aborted if PrefixUnaryExpressionSyntax was visited before here or in
        /// "VisitAssignmentExpression" which call to "AddNoteToVisited", for left part of the assignment
        /// </summary>
        /// <param name="node"></param>
        /// <see cref="VisitAssignmentExpression">Posibily previously called, if the assignment statement was complex</see>
        /// <see cref="RegisterLeftRightPartsAsVisitedIfCorresp">Posibily previously called, if the assignment statement
        /// was complex</see>
        /// <see cref="AddNoteToVisited">Posibily previously called, if the assignment statement was complex</see>
        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            // To avoid problems when rendering enters, for multi-line conditions, it is replaced with white space
            string assignment = $"{DeleteEnterAndCarriageReturnCharacters(node.ToFullString().Trim())}";

            //var ancestors = node.Ancestors().ToArray();
            if (VisitNode(node))
            {
                ProcessVisitAssignmentExpression(node, assignment);
            }

            base.VisitPrefixUnaryExpression(node);
        }

        /// <summary>
        /// Returns a value than specify if it must visit current node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool VisitNode(ExpressionSyntax node)
        {
            bool visitNode = true;
            foreach (var keyValuePair in _visitedNodes.OrderByDescending(x=> x.Value))
            {
                var visitedNode = keyValuePair.Key;
                if (visitedNode is ForStatementSyntax forStatementSyntax)
                {
                    if (forStatementSyntax.Incrementors.Contains(node))
                    {
                        visitNode = false;
                        break;
                    }
                }
                else if (visitedNode is LocalDeclarationStatementSyntax localDeclarationStatementSyntax)
                {
                    foreach (var v in localDeclarationStatementSyntax.Declaration.Variables)
                    {
                        if (v.Initializer.Value == node)
                        {
                            visitNode = false;
                            break;
                        }
                    }
                }
                else if (visitedNode is AssignmentExpressionSyntax assignmentExpressionSyntax)
                {
                    if (assignmentExpressionSyntax.Left is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                    {
                        if (memberAccessExpressionSyntax.Expression is ElementAccessExpressionSyntax
                            elementAccessExpressionSyntax)
                        {
                            if (elementAccessExpressionSyntax.ArgumentList.Contains(node))
                            {
                                visitNode = false;
                                break;
                            }
                        }
                    }

                    if (ProcessExpressionLeftRightAlreadyProcessed(assignmentExpressionSyntax.Right, node))
                    {
                        visitNode = false;
                        break;
                    }
                }
            }

            return visitNode;
        }

        /// <summary>
        /// Check if node has already processed in a mor complex assignment.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool ProcessExpressionLeftRightAlreadyProcessed(ExpressionSyntax expression, ExpressionSyntax node)
        {
            switch (expression)
            {
                case BinaryExpressionSyntax binaryExpr:
                    if (ProcessExpressionLeftRightAlreadyProcessed(binaryExpr.Left, node))
                    {
                        return true;
                    }

                    if (ProcessExpressionLeftRightAlreadyProcessed(binaryExpr.Right, node))
                    {
                        return true;
                    }

                    return false;

                case PrefixUnaryExpressionSyntax prefixUnaryExpr:

                    if (prefixUnaryExpr == node)
                    {
                        return true;
                    }
                    if (ProcessExpressionLeftRightAlreadyProcessed(prefixUnaryExpr.Operand, node))
                    {
                        return true;
                    }

                    return false;

                case PostfixUnaryExpressionSyntax postfixUnaryExpr:
                    if (postfixUnaryExpr == node)
                    {
                        return true;
                    }
                    if (ProcessExpressionLeftRightAlreadyProcessed(postfixUnaryExpr.Operand, node))
                    {
                        return true;
                    }

                    return false;

                case LiteralExpressionSyntax literalExpr:
                    return false;

                case IdentifierNameSyntax identifierExpr:
                    return false;

                default:
                    return false;

            }
        }

        /// <summary>
        /// Called when the visitor visits a PostfixUnaryExpressionSyntax node.
        /// </summary>
        /// <param name="node"></param>
        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            // If this node has already been visited in another larger structure, we ignore it.
            //if (!_visitedNodes.Add(node)) return;

            var (className, methodName) = GetMethodAndClass(node); // Get context

            //string operatorNode = node.OperatorToken.ToString(); // ++ o --
            //string variable = node.Operand.ToString(); // La variable afectada

            // To avoid problems when rendering enters, for multi-line conditions, it is replaced with white space
            string assignment = $"{DeleteEnterAndCarriageReturnCharacters(node.ToFullString().Trim())}";

            if(VisitNode(node)) ProcessVisitAssignmentExpression(node, assignment);

            base.VisitPostfixUnaryExpression(node);
        }

        /// <summary>
        /// Set to store already visited expressions.
        /// </summary>
        private readonly Dictionary<SyntaxNode, long> _visitedNodes = new Dictionary<SyntaxNode, long>();


        /// <summary>
        /// Called when the visitor visits a AssignmentExpressionSyntax node.
        /// For a graphical view, see https://www.mermaidchart.com/play?utm_source=mermaidgpt&utm_medium=GPT&utm_campaign=playground#pako:eNp10MFuwjAMBuBXsXKiEmx3DpNKG3ZFMO1CdgiNKZGKU8UWQ5r27phMlNNytPzZv_NjuhTQLM1xSN_dyWeBj9YR6KtnzuxEK85UsFi8wWrvjL1K9p3AgEeZQxoxe0l5Dp4C5NiftPnrj6-KadQ0fhhgk1OHzJ-Ro9TMsaczktjrmLUaE02uKa5Vt8U-smAuy17LdBg1D4NnuNwHYZhYW5hVVocApEeBpEcXMD6D2dK5fgQ7eMaXf2PN7pOqya6LfdefsaS7K0eOzO8NqmNoVg
        /// Process flow:
        /// 1. Start
        /// 2. Get the components of the assignment (left, operator, right).
        /// 3. Call ProcessVisitAssignmentExpression with the assignment.
        /// 4. Register left and right parts as visited with RegisterLeftRightPartsAsVisitedIfCorresp.
        /// 5. Add the node to the visited list with AddNoteToVisited.
        /// 6. Call base.VisitAssignmentExpression(node) to continue the analysis.
        /// 7. End.
        /// </summary>
        /// <param name="node"></param>
        /// <see cref="ProcessVisitAssignmentExpression"/>
        /// <see cref="RegisterLeftRightPartsAsVisitedIfCorresp"/>
        /// <see cref="AddNoteToVisited"/>
        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            var left = node.Left.ToString();
            string operatorNode = node.OperatorToken.ToString();
            var right = node.Right.ToString();
            string assignment = $"{left} {operatorNode} {right};";

            ProcessVisitAssignmentExpression(node, assignment);

            RegisterLeftRightPartsAsVisitedIfCorresp(node.Right);

            AddNoteToVisited(node);

            base.VisitAssignmentExpression(node);
        }

        /// <summary>
        /// Add a node in the visited nodes set, but previously remove possible former instance.
        /// </summary>
        /// <param name="node"></param>
        private void AddNoteToVisited(SyntaxNode node)
        {
            _visitedNodes.Remove(node);
            _visitedNodes.Add(node, Autoincrement.GetNextValue());
        }


        /// <summary>
        /// Register Left Right Parts As Visited If corresp.
        /// For a graphical vie, see https://www.mermaidchart.com/play?utm_source=mermaidgpt&utm_medium=GPT&utm_campaign=playground#pako:eNqtU8tOwzAQ_JWVD6g59AeQKOr7lT7UAgcIh6jZtBaRHdnb0qrl33FsNQ5VkRCQQ6zdzOzOTOQjW8kE2S1LM_m-2sSK4KETCTBPsxaxJRWdBa65JlQhprTg6w3NTVc39RPXnDAZpm2pFOq81t3n5tRciuVBULwHLBtBxAKo1xvQOkbM44AOOd5H7MPtdO9WATy1uIjV4QTtl4idFUBmJMANqEIF5IUMuGsYgaut0nyH0I6z7Od6i2mBGfdbvpVhnL063W1rsGP10lYJsAtA5qhikgou4FWzc4Up3z86x10zoY-eWDK6dkHPfG4mCZCEnVNUAvoWMPAKziNmxSk8sGeB_Wq00kH-kmfFhbP5xaLU5D0O_3t1ZbqPeGhtjr4PbGQB44vARDK7jL7qJDQjVJydIPQ8km8oYBdnW7zKGSYoiKcc1QkmnsbLNhDu6Sp1RpuCNfWsdGui8XcLYg2aFBfrC37Huns2F7lb_PvAdQeu64pxtQirxaRaTM8F-_gELsthdA
        /// Process flow:
        /// ----------------------------
        /// 1. Start
        /// 2. Evaluate the type of the expression:
        ///     - If it is BinaryExpressionSyntax:
        ///         - Obtain and recursive call to register for the left and right parts.
        ///         - Return the expression with its operator in the format "(left operator right)".
        ///     - If it is PrefixUnaryExpressionSyntax:
        ///         - Add to the visited nodes.
        ///         - Recursive call to register the operand with its operator (operatorOperand).
        ///     - If it is PostfixUnaryExpressionSyntax:
        ///         - Recursive call to register the operand.
        ///         - Add to the visited nodes.
        ///         - Return operandOperator.
        ///     - If it is LiteralExpressionSyntax, return the value of the token.
        ///     - If it is IdentifierNameSyntax, return the name of the identifier.
        ///     - If it is another type, return the text representation of the expression.
        /// 3. end
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private string RegisterLeftRightPartsAsVisitedIfCorresp(ExpressionSyntax expression)
        {
            switch (expression)
            {
                case BinaryExpressionSyntax binaryExpr:
                    string left = RegisterLeftRightPartsAsVisitedIfCorresp(binaryExpr.Left);
                    string operador = binaryExpr.OperatorToken.ToString();
                    string right = RegisterLeftRightPartsAsVisitedIfCorresp(binaryExpr.Right);
                    return $"({left} {operador} {right})";

                case PrefixUnaryExpressionSyntax prefixUnaryExpr:
                    string operadorPrefix = prefixUnaryExpr.OperatorToken.ToString();
                    AddNoteToVisited(prefixUnaryExpr);
                    string operandPrefix = RegisterLeftRightPartsAsVisitedIfCorresp(prefixUnaryExpr.Operand);
                    return $"{operadorPrefix}{operandPrefix}";

                case PostfixUnaryExpressionSyntax postfixUnaryExpr:
                    string operandPostfix = RegisterLeftRightPartsAsVisitedIfCorresp(postfixUnaryExpr.Operand);
                    AddNoteToVisited(postfixUnaryExpr);
                    string operadorPostfix = postfixUnaryExpr.OperatorToken.ToString();
                    return $"{operandPostfix}{operadorPostfix}";

                case LiteralExpressionSyntax literalExpr:
                    return literalExpr.Token.ValueText;

                case IdentifierNameSyntax identifierExpr:
                    return identifierExpr.Identifier.Text;

                default:
                    return expression.ToFullString().Trim();
            }
        }


        /// <summary>
        /// Processes an assignment expression by determining the caller, avoiding duplications, 
        /// and pushing formatted assignment parts onto the stack.
        /// For a graphical view, see https://www.mermaidchart.com/play?utm_source=mermaidgpt&utm_medium=GPT&utm_campaign=playground#pako:eNpd0MFOwzAMBuBXMT4gOIwHQALEWtg6JIQYF0QQilpvq0jtqQn0sO3dcZKpIHp0v_-34h3W0hBe4srJUG9sH-ClNAz63Z4ZXAadGDyHyeQapm8GSwrUdy0T1NY56g2-Zz2NZF-kIWgRr6m52UOxM5glnFzBh_XtmjviUPFKtLz-vCiOPYfcU2gPvJJPG8u4UQZ2Yhs4Betj_P_mMtHZn_ij5JGm76XvbDgm4-IxNktmruaZOvkmYBqcPsyPYp5EpWK5dW2AloPAVg_yS6pEFkqevvwm_wSJzsfHjW6R3INe9I4bvadhPPwA5BlzRQ
        /// Flow of the process:
        /// --------------------
        /// 1. Determine the caller using GetCallerClassFromMethodContainerNode.
        /// 	- If the caller is different from _asignmentInfoStack.Caller, download and assign the caller.
        /// 2. Format the assignment:
        /// 	- Remove newline characters.
        /// 	- Split the assignment into parts.
        /// 	- Push each part onto _asignmentInfoStack.StackAsignment.
        /// 3. End.
        /// </summary>
        /// <param name="node">The syntax node representing the assignment expression.</param>
        /// <param name="assignment">The assignment expression as a string.</param>
        /// <see cref="DownloadStackAssignmentToActionListAndAssignCaller"/>
        /// <see cref="DeleteEnterAndCarriageReturnCharacters"/>
        private void ProcessVisitAssignmentExpression(CSharpSyntaxNode node, string assignment)
        {
            // Determine caller
            var caller = GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1);
            if (caller != _asignmentInfoStack.Caller)
            {
                DownloadStackAssignmentToActionListAndAssignCaller(caller);
            }

            // Avoid duplications in subexpressions (e.g. ++colExcel inside assignment)
            if (node is AssignmentExpressionSyntax nodeAssignment1 &&
                nodeAssignment1.Right is PrefixUnaryExpressionSyntax prefixExpr)
            {
                //if (!_visitedNodes.Add(prefixExpr)) return;
            }

            if (node is AssignmentExpressionSyntax nodeAssignment2 &&
                nodeAssignment2.Right is PrefixUnaryExpressionSyntax posfixExpr)
            {
                //if (!_visitedNodes.Add(posfixExpr)) return;
            }

            // To avoid problems when rendering enters, for multi-line conditions, it is replaced with white space
            string str = DeleteEnterAndCarriageReturnCharacters(assignment, allocatingOnStack:true);
            string[] parts = str.Split('\n');
            foreach(var s in parts)
            {
                _asignmentInfoStack.StackAsignment.Push(s);
            }
        }

        /// <summary>
        /// Called when the visitor visits a LocalDeclarationStatementSyntax node.
        /// </summary>
        /// <param name="node"></param>
        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            // A foreach loop is needed, in case there are several variables declared on the same line.
            // Example: int a = 9, b = 10, c = 11;
            //foreach (var variable in node.Declaration.Variables) 
            //{
            //    //
            //}

            var caller = GetCaller(node); // GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1);
            if (caller != _asignmentInfoStack.Caller)
            {
                DownloadStackAssignmentToActionListAndAssignCaller(caller);
            }
            
            // To avoid problems when rendering enters, for multi-line conditions, it is replaced with white space
            string str = DeleteEnterAndCarriageReturnCharacters(node.ToFullString(), allocatingOnStack: true);
            string[] parts = str.Split('\n');
            foreach (var s in parts)
            {
                _asignmentInfoStack.StackAsignment.Push(s);
            }
            AddNoteToVisited(node);

            base.VisitLocalDeclarationStatement(node);
        }

        /// <summary>
        /// Download all the assignments in Stack to Actions list and to assign the new caller.
        /// </summary>
        /// <param name="caller"></param>
        private void DownloadStackAssignmentToActionListAndAssignCaller(string caller)
        {
            if (_asignmentInfoStack.StackAsignment.Count > 0 && !string.IsNullOrEmpty(_asignmentInfoStack.Caller))
            {
                string strAssignments = $"{NOTE_OVER}" + _asignmentInfoStack.Caller + " : " +
                                        string.Join("\\n", _asignmentInfoStack.StackAsignment.Reverse());
                Actions.Add(strAssignments);
            }
            _asignmentInfoStack.StackAsignment.Clear();

            if(!string.IsNullOrEmpty(caller)) _asignmentInfoStack.Caller = caller;
        }

        /// <summary>
        /// Called when the visitor visits a SwitchStatementSyntax node.
        /// </summary>
        /// <param name="node"></param>
        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            _switchStatementStack.Push(new SwitchStatementInfo { Expression = node.Expression.ToString(), FirstCaseStatement = true});

            // Switch statement is visited
            base.VisitSwitchStatement(node);

            _switchStatementStack.Pop();

            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            Actions.Add("end");
        }

        /// <summary>
        /// Called when the visitor visits a SwitchSectionSyntax node.
        /// </summary>
        /// <param name="node"></param>
        public override void VisitSwitchSection(SwitchSectionSyntax node)
        {
            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            // To avoid problems when rendering enters, for multi-line conditions, it is replaced with white space
            var switchStatementInfo = _switchStatementStack.Peek();
            if (switchStatementInfo.FirstCaseStatement)
            {
                var caseLabel = node.Labels.OfType<CaseSwitchLabelSyntax>().FirstOrDefault();
                if (caseLabel != null)
                {
                    var caseValue = caseLabel.Value.ToString();
                    Actions.Add($"{ALT_CASE} " + DeleteEnterAndCarriageReturnCharacters(switchStatementInfo.Expression + " == " + caseValue));
                }
                else
                {
                    Actions.Add($"{ALT_CASE} " + DeleteEnterAndCarriageReturnCharacters(switchStatementInfo.Expression + " == default"));
                }
                switchStatementInfo.FirstCaseStatement = false;
            }
            else
            {
                var caseLabel = node.Labels.OfType<CaseSwitchLabelSyntax>().FirstOrDefault();
                if (caseLabel != null)
                {
                    var caseValue = caseLabel.Value.ToString();
                    Actions.Add("else case " + DeleteEnterAndCarriageReturnCharacters(switchStatementInfo.Expression + " == " + caseValue));
                }
                else
                {
                    Actions.Add("else case default");
                }
            }

            // Every case statement (including default statement) from switch statement
            base.VisitSwitchSection(node);
        }

        /// <summary>
        /// Called when the visitor visits a ForStatementSyntax node.
        /// </summary>
        /// <param name="node"></param>
        public override void VisitForStatement(ForStatementSyntax node)
        {
            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            // To avoid problems when rendering enters, for multi-line conditions, it is replaced with white space
            Actions.Add("loop For:" + node.Declaration + "; " +
                        DeleteEnterAndCarriageReturnCharacters(node.Condition.ToString()) + " ; " + node.Incrementors);

            AddNoteToVisited(node);

            // Base method is called to avoid interrupt the syntax Analisys
            base.VisitForStatement(node);

            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            _visitedNodes.Remove(node);

            Actions.Add("end");
        }

        public override void VisitBreakStatement(BreakStatementSyntax node)
        {
            //if (node.Parent is BlockSyntax block && block.Parent is IfStatementSyntax ifStatement &&
            //    (ifStatement.Parent is ForStatementSyntax || ifStatement.Parent is ForEachStatementSyntax ||
            //     ifStatement.Parent is WhileStatementSyntax // || ifStatement.Parent is LoopStatementSyntax)) // Only in visual Basic LoopStatementSyntax
            //        ))
            //{
            //    var caller = GetCaller(node);
            //    Actions.Add($"{caller} {ARROW} {caller} (exit loop)");
            //    Actions.Add("return");
            //}
            if (node.Parent is BlockSyntax block &&
                (block.Parent is IfStatementSyntax || block.Parent is ForStatementSyntax || 
                 block.Parent is ForEachStatementSyntax || block.Parent is WhileStatementSyntax))
                //(ifStatement.Parent is ForStatementSyntax || ifStatement.Parent is ForEachStatementSyntax ||
                // ifStatement.Parent is WhileStatementSyntax // || ifStatement.Parent is LoopStatementSyntax)) // Only in visual Basic LoopStatementSyntax
                //    ))
            {
                var caller = GetCaller(node);
                Actions.Add($"{caller} {ARROW} {caller} : break (Exit loop)");
                Actions.Add("return");
            }

            base.VisitBreakStatement(node);
        }


        public override void VisitReturnStatement(ReturnStatementSyntax node)
        {
            var caller = GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1);

            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(caller:caller);

            Tuple<string, string> classAndMethodNodeBelongs = GetMethodAndClass(node);
            
            // To avoid problems when rendering enters, for multi-line conditions, it is replaced with white space
            Actions.Add(
                $"{classAndMethodNodeBelongs.Item1} {DOUBLE_ARROW} {caller} : return {DeleteEnterAndCarriageReturnCharacters(node.Expression != null ? node.Expression.ToFullString() : string.Empty)}");
            
            base.VisitReturnStatement(node);
        }

        /// <summary>
        /// <summary>
        /// Gets the name of the class that calls the method to which a given node belongs.
        /// </summary>
        /// <param name="node">The C# syntax node for which you want to get the calling class.</param>
        /// <param name="positionAncestorAnalyzed">The position of the ancestor to analyze in the invocation stack. The default is 2.</param>
        /// <returns>The name of the class that calls the method to which the given node belongs.</returns>
        /// <remarks>
        /// This method uses an invocation stack (_invocationExpressionSyntaxesStack) to track method invocations.
        /// If the stack is not empty, the ancestor at the specified position is obtained and its class name is returned.
        /// If the stack is empty, the GetCaller method is called to get the class name.
        /// </remarks>
        private string GetCallerClassFromMethodContainerNode(CSharpSyntaxNode node, int positionAncestorAnalyzed = 2)
        {
            if (_invocationExpressionSyntaxesStack.Count > 0)
            {
                var ancestors = _invocationExpressionSyntaxesStack.Reverse().ToArray();
                return ancestors.Length - positionAncestorAnalyzed >= 0
                    ? ancestors[ancestors.Length - positionAncestorAnalyzed].Item2
                    : GetCaller(node);
            }
            
            // Is the root method. The first called. We don´t know who is the caller either the instance name.
            return GetCaller(node);
        }

        /// <summary>
        /// Get the caller of the method to which a given node belongs.
        /// </summary>
        /// <param name="methodDeclaration"></param>
        /// <returns></returns>
        /// <remarks>
        /// Method GetCallerFromMethod(methodDeclaration)
        ///    If _invocationExpressionSyntaxesStack is not empty
        ///        Return the second item of the top element in _invocationExpressionSyntaxesStack
        ///    Get the class name from the parent of methodDeclaration
        ///    If methodDeclaration has the static modifier
        ///        Return the class name
        ///    Else
        ///        Return the class name concatenated with " instance"
        /// </remarks>
        private string GetCallerFromMethod(MethodDeclarationSyntax methodDeclaration)
        {
            if (_invocationExpressionSyntaxesStack.Count > 0)
            {
                return _invocationExpressionSyntaxesStack.Peek().Item2;
            }

            return GetCallerNotUsingStackInvocationExpression(methodDeclaration);
        }

        /// <summary>
        /// Get the caller of the method to which a given node belongs without using
        /// "_invocationExpressionSyntaxesStack".
        /// </summary>
        /// <param name="methodDeclaration"></param>
        /// <returns></returns>
        private string GetCallerNotUsingStackInvocationExpression(MethodDeclarationSyntax methodDeclaration)
        {
            var className = methodDeclaration.Parent is ClassDeclarationSyntax classDecl
                ? classDecl.Identifier.ToString()
                : UNKNOWN;
            string caller = methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword)
                ? className
                : className + " instance";
            return caller;
        }


        /// <summary>
        /// Gets the method and name of the class to which a given node belongs.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private Tuple<string, string> GetMethodAndClass(CSharpSyntaxNode node)
        {
            var method = node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            var classNode = method?.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();

            string className = classNode?.Identifier.Text ?? "UnknownClass";
            string methodName = method?.Identifier.Text ?? "UnknownMethod";

            return Tuple.Create(className, methodName);
        }

        /// <summary>
        /// Called when the visitor visits a WhileStatementSyntax node.
        /// </summary>
        /// <param name="node"></param>
        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            string condition = node.Condition.ToString(); //ProcessExpression(node.Condition);
            // To avoid problems when rendering enters, for multi-line conditions, it is replaced with white space
            Actions.Add($"loop While: {DeleteEnterAndCarriageReturnCharacters(condition)}");

            // Base method is called to avoid interrupt the syntax Analisys
            base.VisitWhileStatement(node);

            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            Actions.Add("end");
        }

        /// <summary>
        /// Process an expression and returns a string representation of it.
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private string ProcessExpression(ExpressionSyntax expression)
        {
            switch (expression)
            {
                case BinaryExpressionSyntax binaryExpression:
                    {
                        string left = ProcessExpression(binaryExpression.Left);
                        string right = ProcessExpression(binaryExpression.Right);
                        return $"{left} {binaryExpression.OperatorToken} {right}";
                    }
                case InvocationExpressionSyntax invocationExpression:
                    {
                        string methodName = invocationExpression.Expression.ToString();
                        string arguments = string.Join(", ", invocationExpression.ArgumentList.Arguments.Select(arg => ProcessExpression(arg.Expression)));
                        return $"{methodName}({arguments})";
                    }
                case ConditionalAccessExpressionSyntax conditionalAccess:
                    {
                        string expressionPart = ProcessExpression(conditionalAccess.Expression);
                        string whenNotNull = ProcessExpression(conditionalAccess.WhenNotNull);
                        return $"{expressionPart}?{whenNotNull}";
                    }
                case MemberBindingExpressionSyntax memberBinding:
                    {
                        return $"?.{memberBinding.Name}";
                    }
                case IdentifierNameSyntax identifierName:
                    {
                        return identifierName.Identifier.Text;
                    }
                case LiteralExpressionSyntax literalExpression:
                    {
                        return literalExpression.Token.ValueText;
                    }
                case MemberAccessExpressionSyntax memberAccess:
                    {
                        string expressionPart = ProcessExpression(memberAccess.Expression);
                        return $"{expressionPart}.{memberAccess.Name}";
                    }
                case ElementAccessExpressionSyntax elementAccess:
                    {
                        string expressionPart = ProcessExpression(elementAccess.Expression);
                        string arguments = string.Join(", ", elementAccess.ArgumentList.Arguments.Select(arg => ProcessExpression(arg.Expression)));
                        return $"{expressionPart}[{arguments}]";
                    }
                case PrefixUnaryExpressionSyntax prefixUnaryExpression:
                    {
                        string operand = ProcessExpression(prefixUnaryExpression.Operand);
                        return $"{prefixUnaryExpression.OperatorToken}{operand}";
                    }
                case ParenthesizedExpressionSyntax parenthesizedExpression:
                    {
                        string innerExpression = ProcessExpression(parenthesizedExpression.Expression);
                        return $"({innerExpression})";
                    }
                case ConditionalExpressionSyntax conditionalExpression:
                    {
                        string condition = ProcessExpression(conditionalExpression.Condition);
                        string whenTrue = ProcessExpression(conditionalExpression.WhenTrue);
                        string whenFalse = ProcessExpression(conditionalExpression.WhenFalse);
                        return $"{condition} ? {whenTrue} : {whenFalse}";
                    }
                default:
                    return expression.ToString();
            }
        }

        /// <summary>
        /// Called when the visitor visits a DoStatementSyntax node.
        /// </summary>
        /// <param name="node"></param>
        public override void VisitDoStatement(DoStatementSyntax node)
        {
            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            string condition = node.Condition.ToString();// ProcessExpression(node.Condition);
            // To avoid problems when rendering enters, for multi-line conditions, it is replaced with white space
            Actions.Add($"loop Do-While: {DeleteEnterAndCarriageReturnCharacters(condition)}");

            // Base method is called to avoid interrupt the syntax Analisys
            base.VisitDoStatement(node);

            Actions.Add("end");
        }

        /// <summary>
        /// Called when the visitor visits a ForEachStatementSyntax node.
        /// </summary>
        /// <param name="node"></param>
        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            // To avoid problems when rendering enters, for multi-line conditions, it is replaced with white space
            Actions.Add($"loop Foreach: {DeleteEnterAndCarriageReturnCharacters(node.Expression.ToString())}");

            // Base method is called to avoid interrupt the syntax Analisys
            base.VisitForEachStatement(node);

            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            Actions.Add("end");
        }

        /// <summary>
        /// Delete Enter and Carriage Return characters from a string.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="allocatingOnStack"></param>
        /// <returns></returns>
        private string DeleteEnterAndCarriageReturnCharacters(string str, bool allocatingOnStack = false)
        {
            const int NULL_POSITION = -1;
            int position = NULL_POSITION;
            List<Tuple<int, int>> listPositions = new List<Tuple<int, int>>();
            if (allocatingOnStack)
            {
                position = str.IndexOf("//", StringComparison.Ordinal);
                char[] arr = str.ToCharArray();
                while (NULL_POSITION != position && position < arr.Length)
                {
                    int i = position;
                    while (i < arr.Length && arr[i] != '\n')
                    {
                        ++i;
                    }

                    if (i < str.Length)
                    {
                        listPositions.Add(Tuple.Create(position, i));
                        position = str.IndexOf("//", position + 1, StringComparison.Ordinal);
                    }
                }

                for (int j = 0; j < arr.Length; ++j)
                {
                    if ((arr[j] == '\n' || arr[j] == '\r' ) && !listPositions.Exists(x => x.Item2 == j))
                    {
                        arr[j] = ' ';
                    }
                }

                str = string.Concat(arr);
            }
            else
            {
                RemoveCharacter('\r');
                RemoveCharacter('\n');
            }

            return str;

            void RemoveCharacter(char character)
            {
                position = str.IndexOf(character);
                while (NULL_POSITION != position)
                {
                    str = str.Substring(0, position) + " " + str.Substring(position + 1).Trim();

                    position = str.IndexOf(character);
                }
            }
        }


        #region Create Destroy Instances

        //private Stack<Tuple<string, string>> _stackInstances = new Stack<Tuple<string, string>>();
        //public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        //{
        //    // Buscar el nombre de la variable que almacena la instancia
        //    string instanceName = GetInstanceName(node);
        //    string className = node.Type.ToString();

        //    if (!string.IsNullOrEmpty(instanceName))
        //    {
        //        _stackInstances.Push(Tuple.Create(className, instanceName));
        //    }

        //    base.VisitObjectCreationExpression(node);
        //}

        //private string GetInstanceName(ObjectCreationExpressionSyntax node)
        //{
        //    // Buscar la declaración de variable que contiene el "new"
        //    var variableDecl = node.Parent as VariableDeclaratorSyntax;
        //    if (variableDecl != null)
        //        return variableDecl.Identifier.Text;

        //    // Si el "new" está en una asignación (ej: obj = new Clase()), obtener el identificador
        //    if (node.Parent is AssignmentExpressionSyntax assignment &&
        //        assignment.Left is IdentifierNameSyntax leftIdentifier)
        //    {
        //        return leftIdentifier.Identifier.Text;
        //    }

        //    return "AnonymousInstance"; // Si no hay variable explícita
        //}

        #endregion

        #region Try Catch Throw
        public override void VisitTryStatement(TryStatementSyntax node)
        {
            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            Actions.Add(TRY_GROUP);

            if (_invocationExpressionSyntaxesStack.Count > 0)
            {
                _tryStatementSyntaxesStack.Push(Tuple.Create(_invocationExpressionSyntaxesStack.Peek(), node));
            }
            else
            {
                // Try was in the root node analized. There is no invocation
                _tryStatementSyntaxesStack.Push(
                    Tuple.Create<Tuple<InvocationExpressionSyntax, string, MethodDeclarationSyntax>, TryStatementSyntax>(null, node));
            }

            base.VisitTryStatement(node);

            _tryStatementSyntaxesStack.Pop();
            
            Actions.Add("end");
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            string additionalMessage = _rethrownExceptionsStack.Count > 0
                ? $" (rethrown {_rethrownExceptionsStack.Peek().Item1.Declaration?.Type})"
                : string.Empty;
            
            if (node.Declaration != null) Actions.Add($"{CATCH_GROUP} {node.Declaration.Type}{additionalMessage}");
            else Actions.Add($"{CATCH_GROUP}{additionalMessage}");
            
            base.VisitCatchClause(node);

            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            Actions.Add("end");
        }

        /// <summary>
        /// Called when the visitor visits a ThrowStatementSyntax node.
        /// </summary>
        /// <param name="node"></param>
        public override void VisitThrowStatement(ThrowStatementSyntax node)
        {
            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            ProcessVisitThrowStatement(node);

            //Console.WriteLine($"Throw founded in line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");
            base.VisitThrowStatement(node);
        }

        /// <summary>
        /// Process the visit of a throw statement.
        /// For a graphical view, see https://www.mermaidchart.com/play?utm_source=mermaidgpt&utm_medium=GPT&utm_campaign=playground#pako:eNptU8Fy2jAQ_ZUdHzIw0_QDeuhMAENIgGQKPZVORpHXWIOQPNIacOv-e1dy4poUH3xYPb339q32dyJthsmXJNf2JAvhCDaTrQH-7gbbZE1c2SZDuL39CqMf22RcoNyDyuGFXM2nhAc0tK4NiTN6LoRTD3goqd4mP1umUbjepKHWwJhZvuFOeUIHWu0RKlMIk2nMAM8SS1LWgDLwrIWh78sF-BKlypUU8eQGHFLlTMc-juae-1IrS62FBiYsNzeKlNDqF8JROCVeNfru-iReTxk2QbZ0UAaBpWSxqUvsUGnk3RTOnuCkqLAV9dwSQxuYMsdUmQykdQ59aU2mzK4lg1dtOZrBWGgNM6RxKE6ti4xdjgPDwxgOP8PcFOgUeUgvNCB39gChfkWi8zqNHd1fN_6f6xm7Ts_khKSeRofqWGcta4gzY6sqr3lIR_s2FW8rJ__FdR_B8wDmTLk7sEceN_KvfgtEalF5_NQ_4CcF_j0LH97AC886WDddDu0b64TmUeih_zJb-kP4o7_SyENMZBlRua1M1sDjh-lrjW4j3A6pu_UYhRZXgOvL1hcRuGTgXZZBdA-cbd_BMkJWDBk5FHvQ1pYf7K1s20ED816joSz7xp_6y3Rtj26gdFai99CaFhdGntrV4UVPTcZr3lZX7wuV_PkLt-leEQ
        /// Flow of the process:
        /// --------------------
        /// 1. Start
        /// 2. Check if _tryStatementSyntaxesStack is empty:
        ///     - If empty, register like unhandled exception in PlantUML specification and return.
        ///     - Otherwise, proceed.
        /// 3. Initialize variables:
        ///     - found = false
        ///     - Determine the catchType:
        ///         - If throw is used without an exception type, find the corresponding catch block
        ///           (Call GetCatchForThrowStatement(node)). In other words, inherits Exception type
        ///           from its corresponding catch.
        ///         - Otherwise, extract the type from the thrown exception.
        /// 4. Identify the invocation source from _invocationExpressionSyntaxesStack (if available).
        /// 5. Iterate over try statements:
        ///     - For each catch block founded for every try in _rethrownExceptionsStack:
        ///         - If it matches catchType or is a general catch, mark it as found.
        ///         - Determine callerTarget (method handling the exception).
        ///         - Determine callerSource (method throwing the exception).
        ///         - Add an action for the throw in the PlantUML specification.
        ///         - Break loop if a match is found.
        /// 6. If no matching catch was found, register an unhandled exception and add process termination.
        /// 7. End.
        /// </summary>
        /// <param name="node"></param>
        private void ProcessVisitThrowStatement(ThrowStatementSyntax node)
        {
            const string GENERAL_EXCEPTION = "Exception";
            var arrayTryStatementSyntaxes = _tryStatementSyntaxesStack.ToArray();
            if (arrayTryStatementSyntaxes.Length > 0)
            {
                bool found = false;
                string catchType;

                int skipLevel = 0;
                bool needsRethrow = false;
                CatchClauseSyntax catchForThrowStatement = null;
                // Case of the "Throw" that propagates the original exception. "Throw" without specific type.
                if (((ObjectCreationExpressionSyntax)node.Expression) == null)
                {
                    catchForThrowStatement = GetCatchForThrowStatement(node);
                    catchType = catchForThrowStatement.Declaration.Type.ToFullString().Trim();
                    skipLevel++;
                    needsRethrow = true;
                }
                else
                {
                    catchType = ((ObjectCreationExpressionSyntax)node.Expression).Type.ToFullString().Trim();
                }

                // Check if the exception expression is an object creation (new Exception(...))
                string parametersStr = GetStrConcatParametersException(node);

                var invocationExpressionSyntaxesSource = _invocationExpressionSyntaxesStack.Count > 0
                    ? _invocationExpressionSyntaxesStack.Peek()
                    : Tuple.Create<InvocationExpressionSyntax, string, MethodDeclarationSyntax>(null, GetCaller(node),
                        null);
                
                var tuplesTryStatementSyntaxes = _rethrownExceptionsStack.Any()
                    ? arrayTryStatementSyntaxes.Skip(_rethrownExceptionsStack.Count())
                    : arrayTryStatementSyntaxes.Skip(skipLevel);

                foreach (var tuple in tuplesTryStatementSyntaxes)
                {
                    foreach (var catchClauseSyntax in tuple.Item2.Catches)
                    {
                        if (catchClauseSyntax.Declaration is null // Exception without declaration, catch anything
                            || catchClauseSyntax.Declaration.Type.ToFullString().Trim() == catchType
                            || catchClauseSyntax.Declaration.Type.ToFullString().Trim() == GENERAL_EXCEPTION) // General Exception, catch anything
                        {
                            found = true;
                            string callerTarget = string.Empty;

                            if (tuple.Item1 == null)
                            {
                                // Try was in the root node analized. There is no invocation
                                var parentMethod = catchClauseSyntax.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                                if (parentMethod != null)
                                {
                                    callerTarget = GetCallerNotUsingStackInvocationExpression(parentMethod);
                                }
                            }
                            else
                            {
                                var invocationExpressionSyntaxesTarget = tuple.Item1;
                                callerTarget = invocationExpressionSyntaxesTarget.Item2;
                            }

                            string callerSource = _rethrownExceptionsStack.Any()
                                ? _rethrownExceptionsStack.Peek().Item2
                                : invocationExpressionSyntaxesSource != null ? 
                                    invocationExpressionSyntaxesSource.Item2 : GetCaller(node);

                            // It constructs the method call with arguments.
                            string parametersFormattedStr = !string.IsNullOrEmpty(parametersStr) ? $"({parametersStr})" : string.Empty;
                            var methodCallWithArguments =
                                $"{callerSource} {THROW_EXCEPTION_ARROW} {callerTarget}: <font color=red>throw {catchType}{parametersFormattedStr}";
                            Actions.Add(methodCallWithArguments);

                            //if (needsRethrow)
                            //{
                            //    _rethrownExceptionsStack.Push(Tuple.Create(catchClauseSyntax, callerSource));
                            //    VisitCatchClause(catchClauseSyntax);
                            //    _rethrownExceptionsStack.Pop();
                            //}
                            break;
                        }
                    }

                    if (found)
                    {
                        break;
                    }
                }

                if (!found)
                {
                    // No catch found for the exception thrown, so we must bue register the process cancelation.
                    string callerTarget = GetCallerNotUsingStackInvocationExpression(_initialMethodDeclarationSyntax);
                    string callerSource = _rethrownExceptionsStack.Any()
                        ? _rethrownExceptionsStack.Peek().Item2
                        : invocationExpressionSyntaxesSource.Item2;

                    string parametersFormattedStr = !string.IsNullOrEmpty(parametersStr) ? $"({parametersStr})" : string.Empty;
                    var methodCallWithArguments = $"{callerSource} {THROW_EXCEPTION_ARROW} {callerTarget}: <font color=red>throw {catchType}{parametersFormattedStr}";
                    Actions.Add(methodCallWithArguments);
                    Actions.Add($"{DESTROY} {callerTarget}");
                }
                
                //Console.WriteLine($"Throw founded in line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");
            }
            else
            {
                // No catch found for the exception thrown, so we must be register the process cancelation.
                string callerTarget = GetCallerNotUsingStackInvocationExpression(_initialMethodDeclarationSyntax);
                string callerSource = GetCaller(node);

                string parametersStr = GetStrConcatParametersException(node);
                var methodCallWithArguments = $"{callerSource} {THROW_EXCEPTION_ARROW} {callerTarget}: <font color=red>throw {((ObjectCreationExpressionSyntax)node.Expression).Type.ToFullString().Trim()}({parametersStr})";
                Actions.Add(methodCallWithArguments);
                Actions.Add($"{DESTROY} {callerTarget}");
            }
        }

        private static string GetStrConcatParametersException(ThrowStatementSyntax node)
        {
            if (node.Expression is ObjectCreationExpressionSyntax objectCreation)
            {
                var arguments = objectCreation.ArgumentList?.Arguments;

                if (arguments != null && arguments.Value.Any())
                {
                    // Convertir los argumentos en una cadena
                    var parametersStr = string.Join(", ", arguments.Value.Select(arg => arg.ToString()));
                    return parametersStr;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// It gets the closer Catch statement that contains the current Throw statement.
        /// For a graphical view, see https://www.mermaidchart.com/play?utm_source=mermaidgpt&utm_medium=GPT&utm_campaign=playground#pako:eNp9kcFOwzAMhl_F5LQd9gJIDLGu3cqhSLALIjuU1KVR20RKUgZa9u646WgHEuRof_9nJzkyoQtk16xs9EFUuXGwW3MFdO5mnD05qnA2h8ViCasXzhKpClCYG7REmk8CHLaoCNoPsVXPempBqTtV3HqIjpy5CxSubkB1TcPZaYhEFIFMhyFrGvKIrjPqzOwvmGe0AYoJSh0aEoJ-RwMid6KC10aL2kJpdAs_BoZNsBhlcbD4DD_cFK2_MZ-QPqpQ1CBLcJXRh0klLUhlZYFDcFQm4dq0oIfNdIXARE3eWfxFZtpD_O822cNu3Ai25KQXavu2VG9nLrRH8TZ40j8ecBO69_SpcZ-ZD9V0qHLFTl9WZ6T-
        /// Process flow:
        /// ----------------------------
        /// 1. Start
        /// 2. Get the closest try block that contains the throw.
        ///     - If there is no try, return null.
        /// 3. Iterate over the catch blocks inside the found try.
        /// 4. Check if the throw is inside the catch:
        ///     - If yes, return that catch.
        ///  - If not, continue with the next catch.
        /// 5. If no catch handles the throw, return null.
        /// 6. End.
        /// </summary>
        /// <param name="throwStatement"></param>
        /// <returns></returns>
        public CatchClauseSyntax GetCatchForThrowStatement(ThrowStatementSyntax throwStatement)
        {
            // Get all ancestor TryStatementSyntax
            var tryStatement = throwStatement.Ancestors().OfType<TryStatementSyntax>().FirstOrDefault();

            if(tryStatement != null)
            {
                // Recorrer los bloques catch dentro del TryStatementSyntax actual
                foreach (var catchClause in tryStatement.Catches)
                {
                    // Check if the Throw Statement Syntax is inside the body of this catch
                    if (catchClause.DescendantNodes().Contains(throwStatement))
                    {
                        return catchClause;
                    }
                }
            }

            // No catch was found that contains the ThrowStatementSyntax
            return null;
        }

        /// <summary>
        /// Called when the visitor visits a FinallyClauseSyntax node.
        /// </summary>
        /// <param name="node"></param>
        public override void VisitFinallyClause(FinallyClauseSyntax node)
        {
            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(
                GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            var s = node.ToFullString();
            Actions.Add($"{FINALLY_GROUP}");

            base.VisitFinallyClause(node);

            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            Actions.Add("end");
        }

        #endregion

        #region Lock
        /// <summary>
        /// Called when the visitor visits a LockStatementSyntax node.
        /// </summary>
        /// <param name="node"></param>
        public override void VisitLockStatement(LockStatementSyntax node)
        {
            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            Actions.Add($"{LOCK_GROUP}({node.Expression?.ToFullString()})");

            base.VisitLockStatement(node);

            // Download all the assignments in Stack to Actions list.
            DownloadStackAssignmentToActionListAndAssignCaller(GetCallerClassFromMethodContainerNode(node, positionAncestorAnalyzed: 1));

            Actions.Add("end");
        }
        #endregion
    }


}
