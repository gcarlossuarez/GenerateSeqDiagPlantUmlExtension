using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AnalyzeCode.MoreComplex
{
    internal class HelperSyntaxWalker
    {

        /*
         Code explanation:
           Get the symbol of the invoked method:
           
           SemanticModel.GetSymbolInfo(invocation) is used to get the symbol (IMethodSymbol) associated with the invocation expression.
           
           Find the method declaration:
           
           We use methodSymbol.Locations to get the location of the symbol in the source code.
           
           We search for the MethodDeclarationSyntax in the syntax tree that matches the name of the method and its parameters.
           
           Check the parameter match:
           
           We compare the parameters in the method declaration with those in the symbol, including the type and whether they are out or ref.
           
           Print the method declaration:
           
           If the declaration is found, the method name, class, and full declaration are printed.
           
           Considerations:
           Overloaded methods: The code verifies that the parameters match in type and quantity, which allows distinguishing between overloaded methods.
           
           Out and ref parameters: Parameters are checked for being out or ref using SyntaxKind.OutKeyword and SyntaxKind.RefKeyword.
           
           External methods: If the method is not defined in the source code (for example, it is a method in an external library), methodSymbol.Locations will not return a location in the source code, and GetMethodDeclaration will return null.
           
           This approach allows you to get the declaration of the exact method being called, even in cases of overloading or out/ref parameters.
                      
         */

        /// <summary>
        /// Method to obtain the method declaration from an invocation expression.
        /// See the full specification in https://mermaid.live/edit#pako:eNp1U9Fu2jAU_RXPT1QCFAhLINKGWqAUWqZp9GVb9uAlF2LJsZHjtFDg33exQ2BaFylSfH1yfM6513uaqBRoRFdCvSYZ04Y8j2NJ8LltxHRpsBLTG9JqfSZ3P2M6BbMAk6l0DIlgmhmu5HInDds2uHxRiS00SaLyDRd2cRPTX47w7sRy-AZGc3gBklseUuzy30ocyAjZZ0W1JEySmTtoaQvDmmWELOSLsoru8R_kK7UkshTib8x3KCxojKARE4K8p72RXx3yH91jSzNx7s-yReWVcElMBmTFBZDXDHRtrMoDUgJbXpii5pvYHJ7OBCtVynR4INN9TGvWD58qR0f3z_Ta9VXpbPLBJeGSdQ151gCYIyYMTCcZnqNJenFey3mwBLNzSrcaviIETYAuFswkGZfrxiWNmVV_gZD8hEH9c9Rv8cNa9fxf1fNr1Y-X_lWhvSfw3mKfcBwnMsVhdNVHV40lbdIcdM54imO8P23GFDuSQ0wj_ExhxUqBQxzLI0JZaRTmk9DI6BKaVKtyndFoxUSBq3KTYsfGnK3RXl3dMEmjPd3SaBC2e92gH3j9vt8ddPrdJt3RqOV7g3bHD4NOGPpB6PnBx2OTvimFFN221wt9v-PjG3g9z-tZvh9200mAlBulF-4e2ut4FjKxO5WO4x-zOCbf
        /// </summary>
        /// <param name="invocation"></param>
        /// <param name="compilation"></param>
        /// <returns></returns>
        public static MethodDeclarationSyntax GetMethodDeclarationSyntax(InvocationExpressionSyntax invocation, CSharpCompilation compilation)
        {
            // Get the symbol of the invoked method
            var semanticModel = compilation.GetSemanticModel(invocation.SyntaxTree);
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);

            if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
            {
                // Find the method declaration using the compilation (not just the current SemanticModel)
                var methodDeclaration = GetMethodDeclaration(methodSymbol, compilation);
                return methodDeclaration;
            }

            return null;
        }

        /// <summary>
        /// Method to get method declaration from symbol.
        /// See specification in https://mermaid.live/edit#pako:eNp9k1tvmzAUx7-K56dEIlGBEAjSVrW59ZZpGn3ZRh88OARUY0eO6Zql-e47GMKQVo0X4Jz_z-fqI01kCjSkGZe_kpwpTR4XsSD4XA1iGmm0xHRIRqNP5PpHTNegSQk6lynhMmG6kIJkSpatMTqUPyWP6VNzxHXNvT10QlmJ9PKNzI8x7egPH4moODKnhpkjQz5LE3GBEb-CrpRoNU89zTfYG9GyEakCXoBEB6HZ66MCaNI6h-nQpWFWbSlKSk0EdoDIrMd26pVRr1t1BCUTukg2CHCsRr2HrA1yg0gETCW50bUtSyHhTDV1F-I9-sbQt0ivEMOK1OG_MKlpiyQ5JM9noWAlECZSskM52kDtSck0pjKYM87JlYIvnWdTOwqxHQxNovVB-PvcY4ddcrdmnIboZnmHszSWy26Cd_0J9kzngd3_neq_pXXBFkb7gEu4FCmuYGO9b6yxoBYtQZWsSHF5j7UzpjqHElsZ4mcKGas4rm4sTihllZbYsISGWlVg0WqXMg2Lgm2xShpmjO_RumOChkf6SkN76o9923ZmbuD73nTmOhY90HDkBe7Ym04mwWzmOEEQeCeL_pYSj7DH_sRxvQvPD9yLwMb3OcoyLbRU_SDfDdFkomS1zTsnGO2muZLmZp7-AMLqJPw
        /// </summary>
        /// <param name="methodSymbol">Símbolo del método</param>
        /// <param name="compilation">Compilación completa</param>
        /// <returns>Declaración del método</returns>
        private static MethodDeclarationSyntax GetMethodDeclaration(IMethodSymbol methodSymbol,
            CSharpCompilation compilation)
        {
            // Get symbol location (file and position). The file where method is really defined, it could be a
            // differente file of the current method invocation is.
            var location = methodSymbol.Locations.FirstOrDefault(l => l.IsInSource);
            if (location == null)
            {
                return null; // Declaration not found in source code
            }

            // Get the syntax tree and the root node, from the file where the method invocated is defined.
            var syntaxTree = location.SourceTree;
            var root = syntaxTree.GetRoot();

            // Get the SemanticModel for the syntax tree where the method is defined
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            // Find the method declaration in the syntax tree
            var methodDeclaration = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == methodSymbol.Name &&
                                     AreParametersMatching(m, methodSymbol, semanticModel));

            return methodDeclaration;
        }

        /// <summary>
        /// Method to check if the declaration parameters match the symbol.
        /// See https://mermaid.live/edit#pako:eNp9kk1P40AMhv_KyCeQ2rJp81FyWNQ2pZQVHCgX2NnDkDgkIjNTTSdiS9v_vs5EpBVCm9PIfp_Xju0dpDpDiCGv9HtaCGPZY8IVo29yxmFlKcLhnPX7P9n0N4cFWrYWRki0aDYsN1oyehc6SzCtKGFLrTj8aS2mjpv9l1tt5YuuOmTmkISQmZZE4BFjqa6V7ZRJo9zflRspbFrs2ZyYB7S1USwX1Qa_ClvVNamWZCYsMlsYXb8WJ411zLVrY_FtG3a7xqNy8bWNkzC7aQwKTN-Y1FmZl82_n-naXhjMzzuLm-8tXJgtyWJSVafTc7KrDl86_F53YBt4ws2e3R6HYk19nMncef-iFc9VRgtuo7dtlCuuoAcSjRRlRsexa9IcbIGSLGJ6ZpiLuqJlcHUgqaitXm1VCnFTpQf1OqMBJ6V4paY_g2uhIN7BX4i9KBxEnueNw_AyjMZDrwdbiPvBKBgEoe-HfnAZjaLx-NCDD63JwBtE_nAU_AiHESW90Pc-a8yz0moDsVu5q_HsiLak22-XRKe9aw_e3f3hH8He7eU
        /// </summary>
        /// <param name="methodDeclaration">Declaración del método</param>
        /// <param name="methodSymbol">Símbolo del método</param>
        /// <param name="semanticModel">Modelo semántico asociado al SyntaxTree del método</param>
        /// <returns>True si los parámetros coinciden, False en caso contrario</returns>
        private static bool AreParametersMatching(MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol, SemanticModel semanticModel)
        {
            // Getting the parameters of the statement
            var declarationParameters = methodDeclaration.ParameterList.Parameters;

            // Get symbol parameters
            var symbolParameters = methodSymbol.Parameters;

            // Check that the number of parameters matches
            if (declarationParameters.Count != symbolParameters.Length)
            {
                return false;
            }

            // Verificar que los tipos de los parámetros coincidan
            for (int i = 0; i < declarationParameters.Count; i++)
            {
                var declarationParameter = declarationParameters[i];
                var symbolParameter = symbolParameters[i];

                // Get the full type of the parameter in the declaration
                var declarationTypeSymbol = semanticModel.GetTypeInfo(declarationParameter.Type).Type;
                var declarationType = declarationTypeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty;

                // Get the full type of the parameter in the symbol
                var symbolType = symbolParameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // Check that parameter types match
                if (declarationType != symbolType)
                {
                    return false;
                }

                // Check if parameter is 'out' or 'ref'
                if (declarationParameter.Modifiers.Any(SyntaxKind.OutKeyword) != (symbolParameter.RefKind == RefKind.Out) ||
                    declarationParameter.Modifiers.Any(SyntaxKind.RefKeyword) != (symbolParameter.RefKind == RefKind.Ref))
                {
                    return false;
                }
            }

            return true;
        }
        
    }
 
}
