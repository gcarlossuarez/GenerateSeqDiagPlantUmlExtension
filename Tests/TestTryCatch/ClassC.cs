using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TestTryCatch
{
    internal class ClassC
    {
        public void CallCTest1(bool sw)
        {
            //throw new FileNotFoundException("File not found");
            if (sw) throw new FileNotFoundException("File not found");
            throw new Exception("Error in ClassC.CallCTest1");
        }

        public void CallCTest3(bool b)
        {
            if (b) throw new FileNotFoundException("File not found");
            //throw new Exception("Error in ClassC.CallCTest1");
        }

        public void CallCTest4(bool b)
        {
            if (b) throw new FileNotFoundException("File not found");
            //throw new Exception("Error in ClassC.CallCTest1");
        }

        public void CallCTest5(bool b)
        {
            if (b) throw new FileNotFoundException("File not found");
            //throw new Exception("Error in ClassC.CallCTest1");
        }

        public void CallCTest6(bool b)
        {
            if (b) throw new FileNotFoundException("File not found");
            //throw new Exception("Error in ClassC.CallCTest1");
        }

        public void CallCTest7()
        {
            bool b = true;
            if (b) throw new MyException("MyException was thrown");
            //throw new Exception("Error in ClassC.CallCTest1");
        }
        public void CallCTest7(bool b)
        {
            if (b) throw new MyException("MyException was thrown");
            //throw new Exception("Error in ClassC.CallCTest1");
        }

        public void CallCTest8(bool b)
        {
            if (b) throw new MyException("MyException was thrown");
            //throw new Exception("Error in ClassC.CallCTest1");
        }
    }
}
