using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleConsoleApp
{
    internal class ClassA
    {
        public void Print()
        {
            Console.WriteLine("Hello from ClassAcs");
        }

        public string Get(int i)
        {
            if (i > 0)
            {
                return "Hello from ClassA";
            }
            else
            {
                return "Goodbye from ClassA";
            }
        }

        public string Process1(int i)
        {
            if (i > 0)
            {
                return new ClassB().Process(i);
            }
            return string.Empty;
        }
    }
}
