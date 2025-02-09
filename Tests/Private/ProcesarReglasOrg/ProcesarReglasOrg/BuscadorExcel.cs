using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;

namespace ProcesarReglasOrg
{
    class BuscadorExcel : IDisposable
    {
        private bool _disposedValue;

        public ExcelPackage ExcelPackage { get; private set; } = null;


        public BuscadorExcel(string filaPathArchivoExcel)
        {
            bool resultado = AbrirExcel(filaPathArchivoExcel);
        }

        private bool AbrirExcel(string filaPathArchivoExcel)
        {
            try
            {
                ExcelPackage = new ExcelPackage(new System.IO.FileInfo(filaPathArchivoExcel));
                //{
                //    // Obtener la hoja de Excel (puedes especificar el nombre de la hoja si es necesario)
                //    ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

                //    // Buscar la celda que contiene el valor deseado
                //    var cell = FindCellByValue(worksheet, targetValue);

                //    if (cell != null)
                //    {
                //        // La celda fue encontrada, puedes acceder a sus propiedades
                //        int row = cell.Start.Row;
                //        int column = cell.Start.Column;

                //        Console.WriteLine($"Celda encontrada en la fila {row}, columna {column}");
                //    }
                //    else
                //    {
                //        Console.WriteLine("Valor no encontrado en el archivo de Excel.");
                //    }
                //}
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine($"Error abriendo el archivo:{filaPathArchivoExcel}. Error:{e.Message}");
                return false;
            }
            
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (ExcelPackage != null)
                    {
                        ExcelPackage.Dispose();
                        ExcelPackage = null;
                    }
                }

                _disposedValue = true;
            }
        }

        // TODO: reemplazar el finalizador solo si "Dispose(bool disposing)" tiene código para liberar los recursos no administrados
        ~BuscadorExcel()
        {
            // No cambie este código. Coloque el código de limpieza en el método "Dispose(bool disposing)".
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public ExcelRangeBase FindCellByValue(ExcelWorksheet worksheet, string targetValue)
        {
            foreach (var cell in worksheet.Cells)
            {
                if (cell.Text == targetValue)
                {
                    return cell;
                }
            }
            return null; // Valor no encontrado
        }

        public ExcelRangeBase FindCellByValueCodError(ExcelWorksheet worksheet, string targetValue)
        {
            int rowCount = worksheet.Dimension.Rows;

            for (int row = 1; row <= rowCount; row++)
            {
                string valueInSecondColumn = worksheet.Cells[row, 2].Text; // Segunda columna
                string valueInThirdColumn = worksheet.Cells[row, 3].Text; // Tercera columna

                if (valueInSecondColumn == targetValue)
                {
                    return worksheet.Cells[row, 3];
                }
            }

            return null; // Valor no encontrado en la segunda columna
        }
    }
}
