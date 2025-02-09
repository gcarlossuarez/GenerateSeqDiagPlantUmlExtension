using System;

namespace Traductor
{
    class Token
    {
        public string Id { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public string DescripciónBloque { get; set; } = string.Empty;
        public int PosFinBloque { get; set; } = int.MinValue;

        public Token(string id, string valor, string descripciónBloque, int posFinBloque)
        {
            Id = id;
            Valor = valor;
            DescripciónBloque = descripciónBloque;
            PosFinBloque = posFinBloque;
        }
    }
}
