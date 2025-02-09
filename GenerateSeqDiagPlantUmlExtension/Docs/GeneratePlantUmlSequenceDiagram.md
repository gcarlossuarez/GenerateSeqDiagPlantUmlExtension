## Method GenerateDsPlantUmlCommand explained
The `Execute` method in the `GenerateDsPlantUmlCommand` class performs the following steps when executed:

1. **Switch to the UI thread**:
 ```csharp
 ThreadHelper.ThrowIfNotOnUIThread();
 ```

2. **Get the DTE instance**:
   
   ```csharp
   var dte = (DTE2)Package.GetGlobalService(typeof(DTE));
   ```

3. **Get the active document**:
   ```charp
   var activeDocument = dte.ActiveDocument;
   ```

4. **Get the text selection in the active document**:
   
   ```csharp
   var textSelection = (TextSelection)
   ```
   
   
5. **Get the code element at the active point of the selection**:
   
   ```csharp
	var codeElement = textSelection.ActivePoint.CodeElement[vsCMElement.vsCMElementFunction];
   ```   
   
6. **Get the full file path**:
   
   ```csharp
   string filePath = activeDocument.FullName;
   ```
   
7. **Check if a code element was found**:
   
   ```csharp
   if (codeElement != null)
   ```
   
8. **Read the file content**:
   
   ```csharp
   var fileContent = File.ReadAllText(filePath);
   ```

9. **Create a syntax tree from the file content**:
   
   ```csahrp
   var syntaxTree = CSharpSyntaxTree.ParseText(fileContent);
   ```

10. **Get the root of the syntax tree**:
    
	   ```csharp
	   var root = syntaxTree.GetRoot() as CompilationUnitSyntax;
	   ```
11. **Check if the root of the syntax tree was found**:
    ```csharp
		if (root == null) 

12. **Get the current cursor position**:
   		```csharp
	 var cursorPosition = textSelection.ActivePoint.AbsoluteCharOffset;
	 ```
	
13. **Find the method node that contains the cursor position**:
    
   ```csharp
   ```var methodNode = root.DescendantNodes() .OfType<MethodDeclarationSyntax>() .FirstOrDefault(m => m.Span.Contains(cursorPosition));
   ```		

14. **Check if a method node was found**:
    
   ```csharp
   if (methodNode != null)
   ```
   
15. **Get the method name**:
    
   ```csharp
   string methodName = methodNode.Identifier.Text;
   `````

16. **Get the containing class**:
    
   ```csharp
   var classNode = methodNode.Ancestors() .OfType<ClassDeclarationSyntax>() .FirstOrDefault(); string className = classNode?.Identifier.Text;
   ```
   
17. **Get the containing namespace**:
    
   ```csharp
   var namespaceNode = classNode?.Ancestors() .OfType<NamespaceDeclarationSyntax>
   ```
   
18. **Show the information**:
    
   ```csharp
   string message = $"Namespace: {namespaceName}\nClass: {className}\nMethod: {methodName}\nFile Path: {filePath}"; VsShellUtilities.ShowMessageBox( this._package, message, "Información del Método Actual", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
   ```
   
19. **Handle the case where no method is found at the current cursor position**:
    
   
   ```csharp
   else { VsShellUtilities.ShowMessageBox( this._package, "No se encontró un método en la posición actual del cursor.", "Información del Método Actual", OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST); }

   ```
20. **Handle the case where no code element is found**:
    
   ```csharp
   else { VsShellUtilities.ShowMessageBox( this._package, "No method found at the current cursor position.", "Method Name", OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST); }
   ```
   
Here is a Mermaid diagram to illustrate the flow:

```mermaid
graph TD
    A[Start] --> B[Switch to UI thread]
    B --> C[Get DTE instance]
    C --> D[Get active document]
    D --> E[Get text selection]
    E --> F[Get code element]
    F --> G[Get file path]
    G --> H{Code element found?}
    H -->|Yes| I[Read file content]
    I --> J[Create syntax tree]
    J --> K[Get root of syntax tree]
    K --> L{Root found?}
    L -->|Yes| M[Get cursor position]
    M --> N[Find method node]
    N --> O{Method node found?}
    O -->|Yes| P[Get method name]
    P --> Q[Get containing class]
    Q --> R[Get containing namespace]
    R --> S[Show information]
    O -->|No| T[Show warning: No method found]
    L -->|No| U[Show warning: No root found]
    H -->|No| V[Show warning: No code element found]
    S --> W[End]
    T --> W
    U --> W
    V --> W
```

```mermaid
sequenceDiagram
    participant Main as Main Thread
    participant InitializeAsync as InitializeAsync
    participant GenerateDsPlantUmlCommand as GenerateDsPlantUmlCommand
    participant Execute as Execute
    participant DTE as DTE2
    participant ActiveDocument as ActiveDocument
    participant TextSelection as TextSelection
    participant SyntaxTree as SyntaxTree
    participant MethodNode as MethodNode
    participant VsShellUtilities as VsShellUtilities

    Main->>InitializeAsync: Call InitializeAsync(package)
    InitializeAsync->>ThreadHelper: SwitchToMainThreadAsync()
    ThreadHelper-->>InitializeAsync: Return to Main Thread
    InitializeAsync-->>package: GetServiceAsync(IMenuCommandService)
    package-->>InitializeAsync: Return OleMenuCommandService
    InitializeAsync->>GenerateDsPlantUmlCommand: new GenerateDsPlantUmlCommand(package, commandService)
    GenerateDsPlantUmlCommand->>Execute: Execute(sender, e)
    Execute->>ThreadHelper: ThrowIfNotOnUIThread()
    Execute->>DTE: GetGlobalService(typeof(DTE))
    DTE-->>Execute: Return DTE2 instance
    Execute->>ActiveDocument: Get active document
    ActiveDocument-->>Execute: Return active document
    Execute->>TextSelection: Get text selection
    TextSelection-->>Execute: Return text selection
    Execute->>TextSelection: Get code element at cursor position
    TextSelection-->>Execute: Return code element
    Execute->>ActiveDocument: Get full file path
    ActiveDocument-->>Execute: Return file path
    Execute->>File: ReadAllText(filePath)
    File-->>Execute: Return file content
    Execute->>SyntaxTree: ParseText(fileContent)
    SyntaxTree-->>Execute: Return syntax tree
    Execute->>SyntaxTree: Get root of syntax tree
    SyntaxTree-->>Execute: Return root
    Execute->>TextSelection: Get cursor position
    TextSelection-->>Execute: Return cursor position
    Execute->>SyntaxTree: Find method node containing cursor position
    SyntaxTree-->>Execute: Return method node
    Execute->>MethodNode: Get method name
    MethodNode-->>Execute: Return method name
    Execute->>MethodNode: Get containing class
    MethodNode-->>Execute: Return class node
    Execute->>MethodNode: Get containing namespace
    MethodNode-->>Execute: Return namespace node
    Execute->>VsShellUtilities: ShowMessageBox(package, message)
    VsShellUtilities-->>Execute: Return
    Execute->>VsShellUtilities: Show warning if no method found
    VsShellUtilities-->>Execute: Return

```