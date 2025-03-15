using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            ClassA classA = new ClassA();
            //classA.Print();
            string a = classA.Get(1);
            Console.WriteLine(a);
            Test();
        }

        static void Test()
        {
            ClassA classA = new ClassA();
            Console.WriteLine(classA.Process1(1));
        }
    }
}
