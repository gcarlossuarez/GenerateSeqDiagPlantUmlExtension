using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestTryCatch
{
    using System;
    using System.Runtime.Serialization;

    [Serializable] // Atributo para permitir la serialización
    public class MyException : Exception
    {
        
        public MyException() : base() { }

        
        public MyException(string mensaje) : base(mensaje) { }

        
        public MyException(string mensaje, Exception innerException)
            : base(mensaje, innerException) { }

        
        protected MyException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
