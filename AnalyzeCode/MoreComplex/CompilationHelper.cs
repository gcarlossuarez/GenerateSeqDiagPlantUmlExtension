using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalyzeCode.MoreComplex
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    public class CompilationHelper
    {
        public static CSharpCompilation CreateCompilationFromProject(List<string> listSourceFiles,
            string projectDirectory = null)
        {
            // 1. Obtener todos los archivos .cs en el directorio del proyecto y subdirectorios
            string[] sourceFiles =
                !string.IsNullOrEmpty(projectDirectory)
                    ? Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                    : listSourceFiles.ToArray();

            // 2. Crear una lista de SyntaxTree a partir de los archivos fuente
            var syntaxTrees = new List<SyntaxTree>();
            foreach (var file in sourceFiles)
            {
                var sourceCode = File.ReadAllText(file);
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                syntaxTrees.Add(syntaxTree);
            }

            // 3. Agregar referencias necesarias
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                // Agrega otras referencias necesarias aquí
            };

            // 4. Crear la compilación
            var compilation = CSharpCompilation.Create("MyCompilation")
                .AddSyntaxTrees(syntaxTrees)
                .AddReferences(references)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return compilation;
        }
    }

}
