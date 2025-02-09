# Generate Sequence Diagram From C# Code

## 🚀 Overview

This tool is designed to analyze **C# source code** using the **Roslyn API**, extracting information about code structure and method calls to automatically generate a **sequence diagram in PlantUML format**.

The main goal is to provide a **global view of the execution flow** without having to manually navigate between methods using options like **"Go to Definition"** or **"Go to Implementation."** Jumping back and forth in the code can be tedious and makes understanding the system difficult. Instead, a visual diagram provides a **quick and clear** overview, helping in debugging and maintaining projects with **hundreds of thousands of lines of code**.

🔍 **A picture is worth a thousand words**, and in development, **a well-structured diagram** can save hours of analysis.

## 🔗 Value Proposition

- **Visual Execution Flow:** Understand interactions between methods without getting lost in code jumps.
- **Easier Debugging:** Especially useful for analyzing large projects where manually following calls is inefficient.
- **Early Stage but Evolving:** C# is an extensive language, and this tool is still under development. It’s not perfect, but the goal is to make life easier when dealing with massive codebases.
- **Open Source and Experimental:** This is not a commercial product; it is a **continuous improvement experiment** to facilitate code analysis.

---

## 🏗️ Components

### 1. **Program Class**
- **Namespace:** `ConsoleAppAnalyzeCode1`
- **Responsibilities:**
  - Reads C# source code from a file.
  - Parses the code into an **Abstract Syntax Tree (AST)**.
  - Obtains the semantic model and processes it with a **syntax walker**.
  - Generates a **PlantUML sequence diagram**.
  - Saves or displays the result.

### 2. **ExtendedControlFlowSyntaxWalker Class**
- **Namespace:** `ConsoleAppAnalyzeCode1`
- **Inheritance:** `CSharpSyntaxWalker`
- **Responsibilities:**
  - Analyzes control structures (`if`, `for`, `while`, `do`, `foreach, switch, try, etc.`).
  - Extracts method call information, identifying:
    - Who is calling (`caller`) and who is being called (`called`).
    - Method names and arguments.
  - Stores extracted data for conversion into **PlantUML**.

### 3. **ConverterToPlantUml Class**
- **Namespace:** `ConsoleAppAnalyzeCode1`
- **Responsibilities:**
  - Converts the **syntax walker** data into **PlantUML**.
  - Defines **participants** and assigns unique aliases.
  - Maps method calls into a **sequence diagram**.

---

## 🔄 Workflow

```mermaid
sequenceDiagram
    participant User
    participant Converter
    participant ExtendedControlFlowSyntaxWalker
    participant ConverterToPlantUml
    participant PlantUML

    User->>Converter: Call Convert()
    Converter->>Converter: Read source code
    Converter->>Converter: Parse syntax tree
    Converter->>Converter: Compile and get SemanticModel
    Converter->>ExtendedControlFlowSyntaxWalker: Instantiate walker
    Converter->>Converter: Locate target method
    Converter->>ExtendedControlFlowSyntaxWalker: Visit target method
    ExtendedControlFlowSyntaxWalker->>ExtendedControlFlowSyntaxWalker: Analyze control flow
    ExtendedControlFlowSyntaxWalker->>Converter: Return actions
    Converter->>ConverterToPlantUml: Instantiate converter
    Converter->>ConverterToPlantUml: Generate PlantUML specification
    ConverterToPlantUml->>Converter: Return PlantUML code
    alt visualizePlantUml is true
        Converter->>PlantUML: Generate diagram
        PlantUML->>Converter: Return diagram file
        Converter->>User: Open diagram file
    end
    Converter->>User: Return PlantUML specification
   ```
🛠️ Usage
##  1️⃣ Read Source Code
```csharp
var sourceCode = File.ReadAllText(@"path\to\Program.cs");
```
## 2️⃣ Parse and Compile
```csharp
var tree = CSharpSyntaxTree.ParseText(sourceCode);
var compilation = CSharpCompilation.Create("MyCompilation")
    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
    .AddSyntaxTrees(tree);
var semanticModel = compilation.GetSemanticModel(tree);
```
### 3️⃣ Traverse the AST
```csharp
var walker = new ExtendedControlFlowSyntaxWalker(semanticModel);
foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
{
    walker.Visit(method);
}
```
## 4️⃣ Generate the PlantUML Diagram
```csharp
var converter = new ConverterToPlantUml();
var plantUmlDiagram = converter.GeneratePlantUml(walker);
Console.WriteLine(plantUmlDiagram);
```
##📌 Notes

Supports Control Flow Statements: Analyzes if, for, while, do, foreach to detect execution patterns.
Method Call Detection: Extracts caller, callee, method name, and parameters.
Unique Aliases in Diagrams: Avoids name conflicts for better clarity.
## 🔮 Future and Limitations
This tool is still in its early stage, meaning:

It does not cover all C# scenarios. Enhancements will be added progressively.
It may struggle with highly complex or dynamic code, especially with reflection or runtime-generated code.
Context analysis is limited, so some interactions might not be fully accurate.
Despite these limitations, this remains a valuable tool to visualize the code’s execution flow quickly and effectively.

## 📜 License
This project is licensed under the following terms:

The software is provided "as is", without any warranty of any kind, express or implied.
The author is not liable for any damages, losses, or issues resulting from the use of this tool.
Users are free to modify, distribute, and use the code for personal or professional purposes.
Any modifications or derivative works should include a reference to the original project.
The tool is not intended for commercial use and remains an open-source initiative for educational and research purposes.
By using this software, you agree to these terms.

## 📢 Contributions & Contact
If you have ideas for improvement, encounter bugs, or just want to collaborate, feel free to contribute! 😃

## 📌 Repository: GitHub - GenerateSequenceDiagramFromCSharp

## 📌 "Understanding code without seeing it is like reading a book with shuffled pages. A sequence diagram is the map that helps you find your way."
This version maintains all essential details but now in English with proper formatting, making it clear and accessible for a global audience. 🚀 Let me know if you want any refinements!