# Limitation in Sequence Diagram Generation

## Identified Issue

During the analysis of the source code for generating sequence diagrams in PlantUML, a limitation has been identified regarding the recognition and registration of certain "low-level" instructions within methods. Specifically, when assigning the value of a property from an external class (not included in the analyzed project files), instructions dependent on that property may not be recorded in the generated diagram.

### Example of the Issue

Given the following C# code snippet:

```csharp
string cellValue = tableCell.InnerText;
// colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, tableCell.InnerText);
colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, cellValue);
```

It has been observed that the commented line, which directly uses `tableCell.InnerText`, is not registered in the generated sequence diagram. This happens because `tableCell` belongs to a class that is not included in the analyzed files, preventing access to events like `VisitLocalDeclarationStatement` and `VisitMethodDeclaration`.

### Root Cause

The tool used to analyze the source code and extract method calls seems to rely on the availability of the referenced class context. If a property from an external class is used directly in a method call without first being assigned to a local variable, the instruction may not be detected correctly.

This behavior suggests that the parser cannot resolve references to external classes, preventing the identification of certain method executions when parameters come from those classes.

## Proposed Solution

To mitigate this issue and ensure the correct representation of all method calls in the sequence diagram:

1. **Include Related Classes**  
   If possible, include the files containing the definitions of external classes used in method calls in the analysis.

2. **Assign to Local Variables**  
   To ensure the instruction is properly registered, it is recommended to assign property values from external classes to local variables before using them in method calls.

   ```csharp
   string cellValue = tableCell.InnerText;
   colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, cellValue);
   ```

3. **Manually Document Omitted Cases**  
   As an alternative, manually document any instructions that might have been omitted due to this limitation in the generated sequence diagrams.

## Potential Future Improvements

- Implement a mechanism in the parser to register all property assignments from external classes, even when `VisitLocalDeclarationStatement` is not accessed.
- Improve compatibility with semantic analysis to correctly identify references to external classes without requiring their definitions to be included in the analyzed file set.
- Enable the option to place the cursor right on the line where the method starts and not as now, which must be placed at least 2 lines from where the method starts.
- Improve and clean up the code; since we are aware that there are data structures that could have been omitted, and code that could have been simplified, with better handling of Rosylin.


### Demos
https://youtu.be/gASkEk-3bIg

https://youtu.be/sa9iUfAhmlM