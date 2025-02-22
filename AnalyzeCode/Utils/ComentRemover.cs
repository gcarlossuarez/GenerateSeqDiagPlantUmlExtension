using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnalyzeCode.Utils
{
    public class CommentRemover
    {
        public static string RemoveComments(string input)
        {
            // Regular expression to remove comments from a line (//)
            string lineCommentPattern = @"//.*?$";
            // Regular expression to remove multi-line comments (/* ... */)
            string blockCommentPattern = @"/\*.*?\*/";

            // Remove comments from a line
            string result = Regex.Replace(input, lineCommentPattern, "", RegexOptions.Multiline);
            // Delete multi-line comments
            result = Regex.Replace(result, blockCommentPattern, "", RegexOptions.Singleline);

            return result;
        }

        private static void TestForDebug()
        {
            string code = @"
using System;

public class Program
{
    // One line comment
    public static void Main()
    {
        /* This is a several lines
comment */
		   int i = 0; // This is a comment
		   // Comment line
		   ++i; // Another comment line
        Console.WriteLine(""Hello, wordl!"");
    }
}";

            string cleanedCode = RemoveComments(code);
            Console.WriteLine(cleanedCode);
        }
    }



}
