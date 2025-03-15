using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using AnalyzeCode.MoreComplex;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WorkWithDVgCollapsing;

namespace AnalyzeCode
{
    /// <summary>
    /// Enumeration that defines the output format of PlantUML.
    /// </summary>
    public enum FormatPlantUmlOutput
    {
        None = 0,
        Png = 1,
        Svg = 2,
    }


    /// <summary>
    /// Class that converts the source code to PlantUML.
    /// </summary>
    public class Converter
    {
        /// <summary>
        /// Convert the source code to PlantUML
        /// For more details, see https://mermaid.live/edit#pako:eNpdk9ty2jAQhl9lR9eGwRyc2BftJOYQEsxkGpJOa7hQ7Q1oKkseWU4gwLtHSE4H6itp_2-1-0vrPclkjiQia0XLDSyGSwHmm86ni3QqmIZYijdUGlW7WUGBeiPzFbRa3-DG4TfpD6Q5PMlaZWhScnTybfpIVYUXgsu4tXqcxgqpPilFyTjVTIoGiC0wTCeo4QkLKjTLIDEH8AYYWmBku2SUsw9TZic03cJPyv-iarCRxcbpmHFjAv7sYE4LrEqaoQcxp1XlARU5JM6VSxrbpMl-QdXa1HcajGUt8u9Hh0xOyOEXVge4S19YZa7qgl6dY3N5gGm62Cj5DqNthuWZ0Ttb694YFahOd_HIjdnnZNboU6e7zb3dPOzfWFVbzw4u-FdbDxawfc3SmHIOL_-jqwvStJa4AMxOgcQ8pa6VOG-DeKRAVVCWm0HZn-gl0RsscEkis8zxldZcL8lSHA1Kay3NS2Qk0qpGj9RlbmwNGTUjVpDolfLKREsqSLQnWxL5YdgOelf98Droh343uPbIjkStqyBs94OO7w-6nXAQhv2jRz6kNCf47dBEuv1e1x8Mgk7o976KjHKmpTqv8dtmuEaUrNebfyJaNnHTb3-C4ydR3PNh
        /// </summary>
        /// <param name="maxDeep"></param>
        /// <param name="listFilesProject"></param>
        /// <param name="fullPathSourceCode"></param>
        /// <param name="namespaceName"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <param name="directoryBaseOutput"></param>
        /// <param name="visualizePlantUml"></param>
        /// <param name="fullPathPlantUmlJar"></param>
        /// <param name="formatPlantUmlOutput"></param>
        /// <param name="encodingPlantUmlTextFile"></param>
        /// <param name="plantUmlLimitSize"></param>
        /// <param name="dpi"></param>
        /// <param name="collapseNodesInSvg"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public string Convert(int maxDeep, List<string> listFilesProject, string fullPathSourceCode, string namespaceName,
            string className, string methodName, string directoryBaseOutput, bool visualizePlantUml,
            string fullPathPlantUmlJar, FormatPlantUmlOutput formatPlantUmlOutput = FormatPlantUmlOutput.None,
            Encoding encodingPlantUmlTextFile = null,
            int plantUmlLimitSize = 8192,
            int dpi = 300,
            bool collapseNodesInSvg = true)
        {
            var listCSharpFiles = listFilesProject.Where(f => new FileInfo(f).Extension.Trim().ToLower() == ".cs")
                .ToList();
            CSharpCompilation compilation = CompilationHelper.CreateCompilationFromProject(listCSharpFiles);

            var semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
            
            var walkerMoreComplex = new MoreComplex.ExtendedControlFlowSyntaxWalker(compilation, semanticModel, maxDeep);

            // Filter by namespace, class and method
            var targetMethod = FindMethod(compilation, namespaceName, className, methodName);

            if (targetMethod != null)
            {
                walkerMoreComplex.Visit(targetMethod);
            }
            else
            {
                throw new Exception(
                    $"El método {methodName} no se encontró en la clase {className} del espacio de nombres {namespaceName}.");
            }

            MoreComplex.ConverterToPlantUml converterMoreComplexToPlantUml = new MoreComplex.ConverterToPlantUml();

            string plantUmlSpecification =
                converterMoreComplexToPlantUml.GeneratePlantUml(walkerMoreComplex, specifyDpi300: false);
            if (visualizePlantUml)
            {
                VisualizePlantUml(directoryBaseOutput, plantUmlSpecification, formatPlantUmlOutput, fullPathPlantUmlJar,
                    encodingPlantUmlTextFile, plantUmlLimitSize, dpi, collapseNodesInSvg);
            }
            else
            {
                ViewOnlyTextFile(directoryBaseOutput, plantUmlSpecification);
            }

            return plantUmlSpecification;
        }

        /// <summary>
        /// Method that visualizes only the PlantUml text file with the PlantUml specification.
        /// </summary>
        /// <param name="directoryBaseOutput"></param>
        /// <param name="plantUmlSpecification"></param>
        private void ViewOnlyTextFile(string directoryBaseOutput, string plantUmlSpecification)
        {
            // Get the output directory
            var outputDirectory = GetOutputDirectory(directoryBaseOutput);

            // Copy the PlantUml specification in a common txt file
            string inputFilePathTxt = Path.Combine(outputDirectory, "diagram.txt");
            File.WriteAllText(inputFilePathTxt, plantUmlSpecification);

            // Open the file text with PlantUml specification
            OpenFileWithDefaultProgram(inputFilePathTxt);
        }

        /// <summary>
        /// Method that finds the method which we want to work.
        /// </summary>
        /// <param name="compilation"></param>
        /// <param name="namespaceName"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public MethodDeclarationSyntax FindMethod(CSharpCompilation compilation, string namespaceName, string className, string methodName)
        {
            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var root = syntaxTree.GetRoot() as CompilationUnitSyntax;

                // Buscar la declaración del espacio de nombres
                var namespaceDeclaration = root?
                    .DescendantNodes()
                    .OfType<NamespaceDeclarationSyntax>()
                    .FirstOrDefault(ns => ns.Name.ToString() == namespaceName);

                if (namespaceDeclaration != null)
                {
                    // Buscar la declaración de la clase dentro del espacio de nombres
                    var classDeclaration = namespaceDeclaration.Members
                        .OfType<ClassDeclarationSyntax>()
                        .FirstOrDefault(c => c.Identifier.Text == className);

                    if (classDeclaration != null)
                    {
                        // Buscar la declaración del método dentro de la clase
                        var methodDeclaration = classDeclaration.Members
                            .OfType<MethodDeclarationSyntax>()
                            .FirstOrDefault(m => m.Identifier.Text == methodName);

                        if (methodDeclaration != null)
                        {
                            return methodDeclaration;
                        }
                    }
                }
            }

            // Si no se encuentra el método, retornar null o manejar el caso según sea necesario
            return null;
        }

        /// <summary>
        /// Method that visualizes the PlantUml Diagram and the text file with teh PlantUml specification (for the case,
        /// which the user needs a better PlantUml visualizer if the PlantUml is very big).
        /// </summary>
        /// <param name="directoryBaseOutput"></param>
        /// <param name="plantUmlSpecification"></param>
        /// <param name="formatPlantUmlOutput"></param>
        /// <param name="plantUmlJarFullPath"></param>
        /// <param name="encodingPlantUmlTextFile"></param>
        /// <param name="plantUmlLimitSize"></param>
        /// <param name="dpi"></param>
        /// <param name="collapseNodesInSvg"></param>
        private void VisualizePlantUml(string directoryBaseOutput, string plantUmlSpecification,
            FormatPlantUmlOutput formatPlantUmlOutput, string plantUmlJarFullPath,
            Encoding encodingPlantUmlTextFile = null,
            int plantUmlLimitSize = 8192,
            int dpi = 300, 
            bool collapseNodesInSvg = true)
        {
            const string logFileName = "log_file.txt";
            // Get the output directory
            var outputDirectory = GetOutputDirectory(directoryBaseOutput);

            string logFilePath = Path.Combine(outputDirectory, logFileName);

            if (File.Exists(logFilePath))
            {
                // Get file information
                FileInfo fileInfo = new FileInfo(logFilePath);

                // Get the date the file was last modified
                DateTime lastModified = fileInfo.LastWriteTime;

                // Calculate the last date (one week ago)
                DateTime oneWeekAgo = DateTime.Now.AddDays(-7);

                // Check if the file is older than one week
                if (lastModified < oneWeekAgo)
                {
                    // Delete the file
                    File.Delete(logFilePath);
                    Console.WriteLine($"The file '{logFilePath}' was deleted because it is older than one week.");
                }
            }
            File.AppendAllText(logFilePath, $"FormatPlantUmlOutput.None: {FormatPlantUmlOutput.None}\n");

            if (formatPlantUmlOutput == FormatPlantUmlOutput.None) return;

            // Path to the text file with the diagram
            string inputFilePath = Path.Combine(outputDirectory, "diagram.puml");

            // Write the PlantUML specification to the file
            File.WriteAllText(inputFilePath, plantUmlSpecification);

            // Copy the PlantUml specification in a common txt file
            string inputFilePathTxt = Path.Combine(outputDirectory, "diagram.txt");
            File.WriteAllText(inputFilePathTxt, plantUmlSpecification);

            // Output path for the image or SVG
            string outputFilePath =
                formatPlantUmlOutput == FormatPlantUmlOutput.Png
                    ? Path.Combine(outputDirectory, "diagram.png")
                    : Path.Combine(outputDirectory, "diagram.svg");

            // Command to run PlantUML
            string javaArguments = $"-jar \"{plantUmlJarFullPath}\" \"{inputFilePath}\"";

            // Increase maximum size and DPI
            javaArguments += $" -DPLANTUML_LIMIT_SIZE={plantUmlLimitSize} -dpi {dpi}";

            // Optionally, split the diagram into multiple images in the output directory.
            //javaArguments += " -split";

            // Optionally, generate SVG instead of PNG
            if (formatPlantUmlOutput == FormatPlantUmlOutput.Svg)
            {
                javaArguments += " -tsvg";
            }

            if(encodingPlantUmlTextFile == null) encodingPlantUmlTextFile = Encoding.UTF8;
            javaArguments += $" -charset {encodingPlantUmlTextFile.WebName}";

            // Configurar el proceso
            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = javaArguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            bool success = false;
            
            File.AppendAllText(logFilePath, $"Java Path: {processInfo.FileName}\n");
            File.AppendAllText(logFilePath, $"PlantUML JAR: {plantUmlJarFullPath}\n");
            File.AppendAllText(logFilePath, $"Arguments: {javaArguments}\n");

            using (Process process = new Process())
            {
                process.StartInfo = processInfo;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                File.AppendAllText(logFilePath, $"Exit Code: {process.ExitCode}\n");
                File.AppendAllText(logFilePath, $"Standard Output: {output}\n");
                File.AppendAllText(logFilePath, $"Standard Error: {error}\n");

                if (process.ExitCode == 0)
                {
                    success = true;
                }
                else
                {
                    Console.WriteLine("Error generating diagram.");
                }
            }


            if (success)
            {
                if (formatPlantUmlOutput == FormatPlantUmlOutput.Svg && collapseNodesInSvg)
                {
                    string inputFile = outputFilePath;
                    outputFilePath = WorkWithDVgCollapsing.SvgFormatter20.Format(inputFile);
                }

                // Open the PlantUml File Formatted
                OpenFileWithDefaultProgram(outputFilePath);

                // Open the file text with PlantUml specification
                OpenFileWithDefaultProgram(inputFilePathTxt);
            }
        }

        /// <summary>
        /// Method that gets the output directory
        /// </summary>
        /// <param name="directoryBaseOutput"></param>
        /// <returns></returns>
        private static string GetOutputDirectory(string directoryBaseOutput)
        {
            string outputDirectory = Path.Combine(directoryBaseOutput, "Output");
            if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);
            return outputDirectory;
        }

        
        /// <summary>
        /// Method that opens a file with the default program
        /// </summary>
        /// <param name="fullFilePath"></param>
        private static void OpenFileWithDefaultProgram(string fullFilePath)
        {
            ProcessStartInfo processInfo;
            if (System.IO.File.Exists(fullFilePath))
            {
                // Open file with default program
                processInfo = new ProcessStartInfo
                {
                    FileName = fullFilePath, 
                    UseShellExecute = true  // Use the system shell to open the file
                };

                try
                {
                    Process.Start(processInfo);
                    Console.WriteLine("File opened successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error loading file :'" + fullFilePath + "'" + ex.Message);
                }
            }
            else
            {
                Console.WriteLine($"File '{fullFilePath}' not exists.");
            }
        }
    }
}
