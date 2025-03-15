// See https://aka.ms/new-console-template for more information
using WorkWithDVgCollapsing;

string inputFile = @"D:\MyTemp\PlantUmlTemp\Output\diagram.svg";

//SvgFormatter10.Format(inputFile, outputFile);
string outputFile = SvgFormatter20.Format(inputFile);

Console.WriteLine($"Output file: {outputFile}");
