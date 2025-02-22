using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AnalyzeCode;
using Microsoft.VisualStudio.Text.Editor;
using System.Web.UI.Design;
using VSLangProj; // Asegúrate de agregar esta referencia

namespace GenerateSeqDiagPlantUmlExtension
{
    using EnvDTE;
    using VSLangProj; // Asegúrate de incluir esta referencia
    using System.Collections.Generic;
    using System.IO;

    internal sealed class GenerateSeqDiagPlantUmlCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("548284ac-00a8-4bef-8fe2-ac48dad7e15b");
        private readonly AsyncPackage _package;

        private GenerateSeqDiagPlantUmlCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this._package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandId);
            commandService.AddCommand(menuItem);
        }

        public static GenerateSeqDiagPlantUmlCommand Instance { get; private set; }

        private IAsyncServiceProvider ServiceProvider => this._package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            Instance = new GenerateSeqDiagPlantUmlCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get DTE instance
            var dte = (DTE2)Package.GetGlobalService(typeof(DTE));

            // Get active document
            var activeDocument = dte.ActiveDocument;
            if (activeDocument == null)
            {
                VsShellUtilities.ShowMessageBox(
                    this._package,
                    "No active document found.",
                    "Error",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            // Get the full text of the active document
            var textDocument = (TextDocument)activeDocument.Object("TextDocument");
            string fileContent = textDocument.StartPoint.CreateEditPoint().GetText(textDocument.EndPoint);
            var textSelection = (TextSelection)activeDocument.Selection;

            // Get cursor position (0-based for Roslyn)
            int cursorPosition = textSelection.ActivePoint.AbsoluteCharOffset - 1; // Ajuste para 0-based

            // Parse the document with Roslyn
            var syntaxTree = CSharpSyntaxTree.ParseText(fileContent);
            var root = syntaxTree.GetRoot() as CompilationUnitSyntax;

            if (root == null)
            {
                VsShellUtilities.ShowMessageBox(
                    this._package,
                    "No root found in the syntax tree.",
                    "Method Name",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            // Find the method that contains the cursor position
            MethodDeclarationSyntax methodNode = null;
            var nodeAtCursor = root.FindNode(new TextSpan(cursorPosition, 1)); // Nodo en la posición exacta del cursor
            if (nodeAtCursor != null)
            {
                // Buscar el método más cercano en los ancestros
                methodNode = nodeAtCursor.AncestorsAndSelf()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault();

                // Si no se encuentra un método directamente, verificar si estamos al inicio del cuerpo
                if (methodNode == null)
                {
                    var methodCandidates = root.DescendantNodes()
                        .OfType<MethodDeclarationSyntax>()
                        .Where(m => m.Body != null && m.Body.Span.Start <= cursorPosition && cursorPosition <= m.Body.Span.End)
                        .OrderBy(m => m.Span.Start)
                        .ToList();

                    if (methodCandidates.Any())
                    {
                        methodNode = methodCandidates.Last(); // Tomar el método más profundo que contenga la posición
                    }
                }
            }

            if (methodNode != null)
            {
                // Method name
                string methodName = methodNode.Identifier.Text;

                // Containing class
                var classNode = methodNode.Ancestors()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault();
                string className = classNode?.Identifier.Text;

                // Namespace
                var namespaceNode = classNode?.Ancestors()
                    .OfType<NamespaceDeclarationSyntax>()
                    .FirstOrDefault();
                string namespaceName = namespaceNode?.Name.ToString();

                // Get full file path
                string filePath = activeDocument.FullName;

                // Get the project item associated with the active document
                ProjectItem projectItem = activeDocument?.ProjectItem;

                // Project File List
                List<string> listFilesProject = new List<string>();

                var optionsPage = (OptionsPage)this._package.GetDialogPage(typeof(OptionsPage));

                bool includeReferencedProjectsFromSolution = optionsPage.IncludeReferencedProjectsFromSolution;

                // Obtener el proyecto al que pertenece el documento activo
                EnvDTE.Project project = projectItem?.ContainingProject;

                if (project != null)
                {
                    // Enumerar archivos del proyecto actual
                    EnumerateFiles(project.ProjectItems, listFilesProject);

                    if (includeReferencedProjectsFromSolution)
                    {
                        // List files from referenced projects
                        EnumerateProjectReferences(project, listFilesProject);
                    }
                }

                AnalyzeCode.Converter converter = new AnalyzeCode.Converter();

                string baseDirectory = optionsPage.BaseDirectory;
                int maxDeep = optionsPage.MaxDeep;
                FormatPlantUmlOutputDiagram formatPlantUmlOutputDiagram = optionsPage.FormatPlantUmlOutputDiagram;
                string fullPathPlantUmlJar = optionsPage.FullPathPlantUmlJar;
                bool visualizePlantUml = formatPlantUmlOutputDiagram != FormatPlantUmlOutputDiagram.None;
                Encoding encodingPlantUmlTextFile = optionsPage.EncodingPlantUmlTextFile;
                int plantUmlLimitSize = optionsPage.PlantUmlLimitSize;
                int dpi = optionsPage.Dpi;

                if (maxDeep < 1)
                {
                    VsShellUtilities.ShowMessageBox(
                        this._package,
                        "The Maximum Deep must be greater than 0.",
                        "Current Method Information",
                        OLEMSGICON.OLEMSGICON_WARNING,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                if (plantUmlLimitSize < 200)
                {
                    VsShellUtilities.ShowMessageBox(
                        this._package,
                        "The PlantUML Limit Size must be greater than 200.",
                        "Current Method Information",
                        OLEMSGICON.OLEMSGICON_WARNING,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                if (dpi < 50)
                {
                    VsShellUtilities.ShowMessageBox(
                        this._package,
                        "The DPI size must be greater than 50.",
                        "Current Method Information",
                        OLEMSGICON.OLEMSGICON_WARNING,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                FormatPlantUmlOutput formatPlantUmlOutput = ConvertToFormatPlantUmlOutput(formatPlantUmlOutputDiagram);

                if (visualizePlantUml && string.IsNullOrEmpty(fullPathPlantUmlJar))
                {
                    VsShellUtilities.ShowMessageBox(
                        this._package,
                        "The full path to the PlantUML JAR file has not been specified.",
                        "Current Method Information",
                        OLEMSGICON.OLEMSGICON_WARNING,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return;
                }

                var plantUml = converter.Convert(maxDeep, listFilesProject, filePath, namespaceName, className, methodName, directoryBaseOutput: baseDirectory,
                    visualizePlantUml: visualizePlantUml, fullPathPlantUmlJar: fullPathPlantUmlJar,
                    formatPlantUmlOutput: formatPlantUmlOutput,
                    encodingPlantUmlTextFile: encodingPlantUmlTextFile,
                    plantUmlLimitSize: plantUmlLimitSize,
                    dpi: dpi);
                Console.WriteLine(plantUml);
            }
            else
            {
                VsShellUtilities.ShowMessageBox(
                    this._package,
                    "No method found at the current cursor position.",
                    "Method Name",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
        //private void Execute(object sender, EventArgs e)
        //{
        //    ThreadHelper.ThrowIfNotOnUIThread();

        //    // Get DTE instance
        //    var dte = (DTE2)Package.GetGlobalService(typeof(DTE));

        //    // Get active document
        //    var activeDocument = dte.ActiveDocument;

        //    var textSelection = (TextSelection)activeDocument.Selection;

        //    var codeElement = textSelection.ActivePoint.CodeElement[vsCMElement.vsCMElementFunction];

        //    if (codeElement != null)
        //    {
        //        // Get full file path 
        //        string filePath = activeDocument.FullName;

        //        // Get the project item associated with the active document
        //        ProjectItem projectItem = activeDocument?.ProjectItem;

        //        // Project File List
        //        List<string> listFilesProject = new List<string>();

        //        var optionsPage = (OptionsPage)this._package.GetDialogPage(typeof(OptionsPage));

        //        bool includeReferencedProjectsFromSolution = optionsPage.IncludeReferencedProjectsFromSolution;

        //        // Obtener el proyecto al que pertenece el documento activo
        //        EnvDTE.Project project = projectItem?.ContainingProject;

        //        if (project != null)
        //        {
        //            // Enumerar archivos del proyecto actual
        //            EnumerateFiles(project.ProjectItems, listFilesProject);

        //            if (includeReferencedProjectsFromSolution)
        //            {
        //                // List files from referenced projects
        //                EnumerateProjectReferences(project, listFilesProject);
        //            }
        //        }

        //        // Read file content
        //        var fileContent = File.ReadAllText(filePath);

        //        // Create a syntax tree
        //        var syntaxTree = CSharpSyntaxTree.ParseText(fileContent);

        //        // Get root of tree of syntax
        //        var root = syntaxTree.GetRoot() as CompilationUnitSyntax;

        //        if (root == null)
        //        {
        //            VsShellUtilities.ShowMessageBox(
        //                this._package,
        //                "No root found in the syntax tree.",
        //                "Method Name",
        //                OLEMSGICON.OLEMSGICON_WARNING,
        //                OLEMSGBUTTON.OLEMSGBUTTON_OK,
        //                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        //            return;
        //        }

        //        // Get the cursor position
        //        var cursorPosition = textSelection.ActivePoint.AbsoluteCharOffset;

        //        // Find the method that contains the cursor position
        //        var methodNode = root.DescendantNodes()
        //            .OfType<MethodDeclarationSyntax>()
        //            .FirstOrDefault(m => m.Span.Contains(cursorPosition));

        //        if (methodNode != null)
        //        {
        //            // Method name
        //            string methodName = methodNode.Identifier.Text;

        //            // Containing class
        //            var classNode = methodNode.Ancestors()
        //                .OfType<ClassDeclarationSyntax>()
        //                .FirstOrDefault();
        //            string className = classNode?.Identifier.Text;

        //            // Name space
        //            var namespaceNode = classNode?.Ancestors()
        //                .OfType<NamespaceDeclarationSyntax>()
        //                .FirstOrDefault();
        //            string namespaceName = namespaceNode?.Name.ToString();

        //            AnalyzeCode.Converter converter = new AnalyzeCode.Converter();

        //            string baseDirectory = optionsPage.BaseDirectory;
        //            int maxDeep = optionsPage.MaxDeep;
        //            FormatPlantUmlOutputDiagram formatPlantUmlOutputDiagram = optionsPage.FormatPlantUmlOutputDiagram;
        //            string fullPathPlantUmlJar = optionsPage.FullPathPlantUmlJar;
        //            bool visualizePlantUml = formatPlantUmlOutputDiagram != FormatPlantUmlOutputDiagram.None;
        //            Encoding encodingPlantUmlTextFile = optionsPage.EncodingPlantUmlTextFile;
        //            int plantUmlLimitSize = optionsPage.PlantUmlLimitSize;
        //            int dpi = optionsPage.Dpi;

        //            if (maxDeep < 1)
        //            {
        //                VsShellUtilities.ShowMessageBox(
        //                    this._package,
        //                    "The Maximum Deep must be greater than 0.",
        //                    "Current Method Information",
        //                    OLEMSGICON.OLEMSGICON_WARNING,
        //                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
        //                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        //                return;
        //            }

        //            if (plantUmlLimitSize < 200)
        //            {
        //                VsShellUtilities.ShowMessageBox(
        //                    this._package,
        //                    "The PlantUML Limit Size must be greater than 200.",
        //                    "Current Method Information",
        //                    OLEMSGICON.OLEMSGICON_WARNING,
        //                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
        //                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        //                return;
        //            }

        //            if (dpi < 50)
        //            {
        //                VsShellUtilities.ShowMessageBox(
        //                    this._package,
        //                    "The DPI size must be greater than 50.",
        //                    "Current Method Information",
        //                    OLEMSGICON.OLEMSGICON_WARNING,
        //                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
        //                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        //                return;
        //            }

        //            FormatPlantUmlOutput formatPlantUmlOutput = ConvertToFormatPlantUmlOutput(formatPlantUmlOutputDiagram);

        //            if (visualizePlantUml && string.IsNullOrEmpty(fullPathPlantUmlJar))
        //            {
        //                VsShellUtilities.ShowMessageBox(
        //                    this._package,
        //                    "The full path to the PlantUML JAR file has not been specified.",
        //                    "Current Method Information",
        //                    OLEMSGICON.OLEMSGICON_WARNING,
        //                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
        //                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        //                return;
        //            }

        //            var plantUml = converter.Convert(maxDeep, listFilesProject, filePath, namespaceName, className, methodName, directoryBaseOutput: baseDirectory,
        //                visualizePlantUml: visualizePlantUml, fullPathPlantUmlJar: fullPathPlantUmlJar,
        //                formatPlantUmlOutput: formatPlantUmlOutput,
        //                encodingPlantUmlTextFile: encodingPlantUmlTextFile,
        //                plantUmlLimitSize: plantUmlLimitSize,
        //                dpi: dpi);
        //            Console.WriteLine(plantUml);
        //        }
        //        else
        //        {
        //            VsShellUtilities.ShowMessageBox(
        //                this._package,
        //                "No se encontró un método en la posición actual del cursor.",
        //                "Información del Método Actual",
        //                OLEMSGICON.OLEMSGICON_WARNING,
        //                OLEMSGBUTTON.OLEMSGBUTTON_OK,
        //                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        //        }
        //    }
        //    else
        //    {
        //        VsShellUtilities.ShowMessageBox(
        //            this._package,
        //            "No method found at the current cursor position.",
        //            "Method Name",
        //            OLEMSGICON.OLEMSGICON_WARNING,
        //            OLEMSGBUTTON.OLEMSGBUTTON_OK,
        //            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        //    }
        //}

        /// <summary>
        /// Converts from FormatPlantUmlOutputDiagram to FormatPlantUmlOutput
        /// </summary>
        /// <param name="diagramFormat">The diagram format to convert</param>
        /// <returns>The corresponding FormatPlantUmlOutput value</returns>
        public static FormatPlantUmlOutput ConvertToFormatPlantUmlOutput(FormatPlantUmlOutputDiagram diagramFormat)
        {
            switch (diagramFormat)
            {
                case FormatPlantUmlOutputDiagram.Png:
                    return FormatPlantUmlOutput.Png;
                case FormatPlantUmlOutputDiagram.Svg:
                    return FormatPlantUmlOutput.Svg;
                default:
                    return FormatPlantUmlOutput.None;
            }
        }

        /// <summary>
        /// Recursively enumerates files and subfolders in a project's ProjectItems.
        /// </summary>
        /// <param name="projectItems">The ProjectItems collection to enumerate</param>
        /// <param name="listFilesProject">The list to store the file paths</param>
        private void EnumerateFiles(ProjectItems projectItems, List<string> listFilesProject)
        {
            if (projectItems == null) return;

            foreach (ProjectItem item in projectItems)
            {
                for (short i = 1; i <= item?.FileCount; i++)
                {
                    string filePath = item?.FileNames[i];
                    if (File.Exists(filePath))
                    {
                        listFilesProject.Add(filePath);
                    }
                }

                // Recursive call to handle subfolders and files
                if (item?.ProjectItems != null && item?.ProjectItems.Count > 0)
                {
                    EnumerateFiles(item?.ProjectItems, listFilesProject);
                }
            }
        }

        /// <summary>
        /// Enumerates files from referenced projects within the solution.
        /// </summary>
        /// <param name="project">The source project to check for references</param>
        /// <param name="listFilesProject">The list to store the file paths of referenced projects</param>
        private void EnumerateProjectReferences(EnvDTE.Project project, List<string> listFilesProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project == null) return;

            // Obtener la solución desde DTE
            var dte = (DTE2)Package.GetGlobalService(typeof(DTE));
            if (dte.Solution == null || dte.Solution.Projects == null) return;

            // Recorrer todos los proyectos en la solución
            foreach (EnvDTE.Project solutionProject in dte.Solution.Projects)
            {
                if (solutionProject == null || solutionProject.UniqueName == project.UniqueName) continue;

                // Verificar si este proyecto es una referencia del proyecto actual
                bool isReferenced = IsProjectReferenced(project, solutionProject);
                if (isReferenced)
                {
                    // List files from referenced projects
                    EnumerateFiles(solutionProject.ProjectItems, listFilesProject);

                    // Recursively list files from referenced projects
                    EnumerateProjectReferences(solutionProject, listFilesProject);
                }
            }
        }

        /// <summary>
        /// Check if a project references another project (referenced project).
        /// </summary>
        /// <param name="project">The project to check for references</param>
        /// <param name="referencedProject">The project to check if it is referenced</param>
        /// <returns>True if the project references the referencedProject, otherwise false</returns>
        private bool IsProjectReferenced(EnvDTE.Project project, EnvDTE.Project referencedProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project == null || referencedProject == null) return false;

            // Get the unique name from referenced project
            string referencedUniqueName = referencedProject.UniqueName;

            // Get the service from VSLangProj
            VSProject vsProject = project.Object as VSProject;
            if (vsProject == null) return false;

            try
            {
                // Iterate on current project references
                foreach (Reference reference in vsProject.References)
                {
                    if (reference.SourceProject != null && reference.SourceProject.UniqueName == referencedUniqueName)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception if needed (e.g., corrupted project references)
                System.Diagnostics.Debug.WriteLine($"Error checking project references: {ex.Message}");
                return false;
            }

            return false;
        }
    }
    //internal sealed class GenerateSeqDiagPlantUmlCommand
    //{
    //    public const int CommandId = 0x0100;
    //    public static readonly Guid CommandSet = new Guid("548284ac-00a8-4bef-8fe2-ac48dad7e15b");
    //    private readonly AsyncPackage _package;

    //    private GenerateSeqDiagPlantUmlCommand(AsyncPackage package, OleMenuCommandService commandService)
    //    {
    //        this._package = package ?? throw new ArgumentNullException(nameof(package));
    //        commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

    //        var menuCommandId = new CommandID(CommandSet, CommandId);
    //        var menuItem = new MenuCommand(this.Execute, menuCommandId);
    //        commandService.AddCommand(menuItem);
    //    }

    //    public static GenerateSeqDiagPlantUmlCommand Instance { get; private set; }

    //    private IAsyncServiceProvider ServiceProvider => this._package;

    //    public static async Task InitializeAsync(AsyncPackage package)
    //    {
    //        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

    //        var commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
    //        Instance = new GenerateSeqDiagPlantUmlCommand(package, commandService);
    //    }

    //    /// <summary>
    //    /// This function is the callback used to execute the command when the menu item is clicked.
    //    /// See file:///./Docs/GeneratePlantUmlSequenceDiagram.md for more information.
    //    /// https://mermaid.live/edit#pako:eNptlF1zojAUhv_KmVyroxbBcrE7W79qW20r2p1d9CIDQZiRxAmh6qL_fcMBla7LFcn75M178pURT_iM2GQt6TaEeX_JQX8_XEdRqVZQr3-DB9fZRcoLQQlYjEGFklF_VYAPSPTcEVPQnw8g4omi3GOl3EO5jzL1VPTJwBdeGjOuSqKPxAAJxfYKErZhmhS8BAYIDBHIs4LWK-OHKI9QDqINgy1VYamNUHvMepVhEIiU-99PBfGYE8dfLDnC2J3psgoPT3B1nWKMNk9uT9etGCQHrugelGTnKp8QeMYMUggFIvgP9YzUSzbLiS8pXq4pJkWdqUyEhK1IospKTNBg6g4j7kPMVCh84Lq0Up6i_JpNrsrXaV6v07zhNGcPGp893tDjvVxsXUHEI74Gb0OTpETeEZn9i-QmyZZedn6GmOM6odjpUxEIGdNKLUWWqTjCvEB2VOY-NkzFOReGX1WXKOcXt7y8rOiquq05_XFLezenoRzlYOSf7uDSMy96isai2vgoGqRGYqYri3x9hbJcWhIVauslsfWvzwKabtSSLPlJozRVwjlwj9hKpqxGpEjXIbEDukl0K936-nj1I6qvYnzp3VJO7Izsid2-azSb3a7RbJmtTqdlGmaNHIhdNy2j0TE6Ztfo3luWeWedauSPENqi3TDbTavVNu5b3XbLtKwu-v1GsYjA_EgJOSneAHwKzkEGqJQ5Tn8BSSJI_A
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void Execute(object sender, EventArgs e)
    //    {
    //        ThreadHelper.ThrowIfNotOnUIThread();

    //        // Get DTE instance
    //        var dte = (DTE2)Package.GetGlobalService(typeof(DTE));

    //        // Get active document
    //        var activeDocument = dte.ActiveDocument;

    //        var textSelection = (TextSelection)activeDocument.Selection;

    //        var codeElement = textSelection.ActivePoint.CodeElement[vsCMElement.vsCMElementFunction];

    //        if (codeElement != null)
    //        {
    //            // Get full file path 
    //            string filePath = activeDocument.FullName;

    //            // Get the project item associated with the active document
    //            ProjectItem projectItem = activeDocument?.ProjectItem;

    //            // Project File List
    //            List<string> listFilesProject = new List<string>();

    //            // Obtener el proyecto al que pertenece el documento activo
    //            EnvDTE.Project project = projectItem?.ContainingProject;

    //            if (project != null)
    //            {
    //                // Recursive call to handle subfolders and files
    //                EnumerateFiles(project.ProjectItems, listFilesProject);
    //            }

    //            // Read file content
    //            var fileContent = File.ReadAllText(filePath);

    //            // Create a syntax tree
    //            var syntaxTree = CSharpSyntaxTree.ParseText(fileContent);

    //            // Get root of tree of syntax
    //            var root = syntaxTree.GetRoot() as CompilationUnitSyntax;

    //            if (root == null)
    //            {
    //                VsShellUtilities.ShowMessageBox(
    //                    this._package,
    //                    "No root found in the syntax tree.",
    //                    "Method Name",
    //                    OLEMSGICON.OLEMSGICON_WARNING,
    //                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
    //                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    //                return;
    //            }

    //            // Get the cursor position
    //            var cursorPosition = textSelection.ActivePoint.AbsoluteCharOffset;

    //            // Find the method that contains the cursor position
    //            var methodNode = root.DescendantNodes()
    //                .OfType<MethodDeclarationSyntax>()
    //                .FirstOrDefault(m => m.Span.Contains(cursorPosition));

    //            if (methodNode != null)
    //            {
    //                // Method name
    //                string methodName = methodNode.Identifier.Text;

    //                // Containing class
    //                var classNode = methodNode.Ancestors()
    //                    .OfType<ClassDeclarationSyntax>()
    //                    .FirstOrDefault();
    //                string className = classNode?.Identifier.Text;

    //                // Name space
    //                var namespaceNode = classNode?.Ancestors()
    //                    .OfType<NamespaceDeclarationSyntax>()
    //                    .FirstOrDefault();
    //                string namespaceName = namespaceNode?.Name.ToString();

    //                // Show the information
    //                //string message = $"Namespace: {namespaceName}\nClass: {className}\nMethod: {methodName}\nFile Path: {filePath}";
    //                //VsShellUtilities.ShowMessageBox(
    //                //    this._package,
    //                //    message,
    //                //    "Información del Método Actual",
    //                //    OLEMSGICON.OLEMSGICON_INFO,
    //                //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
    //                //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    //                AnalyzeCode.Converter converter = new AnalyzeCode.Converter();

    //                //8string baseDirectory = @"D:\MyTemp\PlantUmlTemp";
    //                //int maxDeep = 200;
    //                var optionsPage = (OptionsPage)this._package.GetDialogPage(typeof(OptionsPage));

    //                string baseDirectory = optionsPage.BaseDirectory;
    //                int maxDeep = optionsPage.MaxDeep;
    //                FormatPlantUmlOutputDiagram formatPlantUmlOutputDiagram = optionsPage.FormatPlantUmlOutputDiagram;
    //                string fullPathPlantUmlJar = optionsPage.FullPathPlantUmlJar;
    //                bool visualizePlantUml = formatPlantUmlOutputDiagram != FormatPlantUmlOutputDiagram.None;
    //                Encoding encodingPlantUmlTextFile = optionsPage.EncodingPlantUmlTextFile;
    //                int plantUmlLimitSize = optionsPage.PlantUmlLimitSize;
    //                int dpi = optionsPage.Dpi;

    //                if (maxDeep < 1)
    //                {
    //                    VsShellUtilities.ShowMessageBox(
    //                        this._package,
    //                        "The Maximum Deep must be greater than 0.",
    //                        "Current Method Information",
    //                        OLEMSGICON.OLEMSGICON_WARNING,
    //                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
    //                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    //                    return;
    //                }

    //                if (plantUmlLimitSize < 200)
    //                {
    //                    VsShellUtilities.ShowMessageBox(
    //                        this._package,
    //                        "The PlantUML Limit Size must be greater than 200.",
    //                        "Current Method Information",
    //                        OLEMSGICON.OLEMSGICON_WARNING,
    //                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
    //                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    //                    return;
    //                }

    //                if (dpi < 50)
    //                {
    //                    VsShellUtilities.ShowMessageBox(
    //                        this._package,
    //                        "The DPI size must be greater than 50.",
    //                        "Current Method Information",
    //                        OLEMSGICON.OLEMSGICON_WARNING,
    //                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
    //                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    //                    return;
    //                }

    //                FormatPlantUmlOutput formatPlantUmlOutput = ConvertToFormatPlantUmlOutput(formatPlantUmlOutputDiagram);

    //                if(visualizePlantUml && string.IsNullOrEmpty(fullPathPlantUmlJar))
    //                {
    //                    VsShellUtilities.ShowMessageBox(
    //                        this._package,
    //                        "The full path to the PlantUML JAR file has not been specified.",
    //                        "Current Method Information",
    //                        OLEMSGICON.OLEMSGICON_WARNING,
    //                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
    //                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    //                    return;
    //                }

    //                //fullPathPlantUmlJar: @"D:\Proyectos Visual Studio\VS2022\GenerateSequenceDiagramPlantUml\AnalyzeCode\Jars\plantuml-1.2025.0.jar"

    //                var plantUml = converter.Convert(maxDeep, listFilesProject ,filePath, namespaceName, className, methodName,directoryBaseOutput: baseDirectory,
    //                    visualizePlantUml: visualizePlantUml, fullPathPlantUmlJar: fullPathPlantUmlJar, 
    //                    formatPlantUmlOutput: formatPlantUmlOutput,
    //                    encodingPlantUmlTextFile: encodingPlantUmlTextFile,
    //                    plantUmlLimitSize:plantUmlLimitSize,
    //                    dpi:dpi);
    //                Console.WriteLine(plantUml);
    //            }
    //            else
    //            {
    //                // No method found at current location
    //                VsShellUtilities.ShowMessageBox(
    //                    this._package,
    //                    "No se encontró un método en la posición actual del cursor.",
    //                    "Información del Método Actual",
    //                    OLEMSGICON.OLEMSGICON_WARNING,
    //                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
    //                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    //            }
    //        }
    //        else
    //        {
    //            VsShellUtilities.ShowMessageBox(
    //                this._package,
    //                "No method found at the current cursor position.",
    //                "Method Name",
    //                OLEMSGICON.OLEMSGICON_WARNING,
    //                OLEMSGBUTTON.OLEMSGBUTTON_OK,
    //                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
    //        }
    //    }


    //    /// <summary>
    //    /// Convert from FormatPlantUmlOutputDiagram to FormatPlantUmlOutput
    //    /// </summary>
    //    /// <param name="diagramFormat"></param>
    //    /// <returns></returns>
    //    public static FormatPlantUmlOutput ConvertToFormatPlantUmlOutput(FormatPlantUmlOutputDiagram diagramFormat)
    //    {
    //        switch (diagramFormat)
    //        {
    //            case FormatPlantUmlOutputDiagram.Png:
    //                return FormatPlantUmlOutput.Png;
    //            case FormatPlantUmlOutputDiagram.Svg:
    //                return FormatPlantUmlOutput.Svg;
    //            default:
    //                return FormatPlantUmlOutput.None;
    //        }
    //    }


    //    /// <summary>
    //    /// Recursive function to handle subfolders and files
    //    /// </summary>
    //    /// <param name="projectItems"></param>
    //    /// <param name="listFilesProject"></param>
    //    private void EnumerateFiles(ProjectItems projectItems, List<string> listFilesProject)
    //    {
    //        foreach (ProjectItem item in projectItems)
    //        {
    //            for (short i = 1; i <= item.FileCount; i++)
    //            {
    //                string filePath = item.FileNames[i];
    //                if (File.Exists(filePath))
    //                {
    //                    listFilesProject.Add(filePath);
    //                }
    //            }

    //            // Recursive call to handle subfolders and files
    //            if (item.ProjectItems != null && item.ProjectItems.Count > 0)
    //            {
    //                EnumerateFiles(item.ProjectItems, listFilesProject);
    //            }
    //        }
    //    }

    //}
}
