namespace ProcesarReglasOrg
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using DocumentFormat.OpenXml.Packaging;
    using DocumentFormat.OpenXml.Wordprocessing;
    using OfficeOpenXml;
    using OfficeOpenXml.Style;
    using Traductor;

    class Program
    {
        private const string REGISTRO_PRINCIPAL = "RegistroCabeceraComprobante";
        private const string REGISTRO_ÍTEMS = "RegistroDetalleComprobante";

        static void Main()
        {
			// Solamente, para pruebas
			int x = 10;
			int y = 2, z = 4; // valores de y y z, para pruebas
			x += ++y * z--;
			
            string filePathWordIn = ConfigurationManager.AppSettings["FilePathWordIn"];
            string filePathExcelOut = ConfigurationManager.AppSettings["FilePathExcelOut"];

            string filePathExcelValidacionesExcluyentesEmpresa = ConfigurationManager.AppSettings["FilePathExcelValidacionesExcluyentesCAEA"];
            string filePathExcelValidacionesNoExcluyentesEmpresa = ConfigurationManager.AppSettings["FilePathExcelValidacionesNoExcluyentesCAEA"];

            string filePathExcelValidacionesExcluyentesEmpresa_Ítems = ConfigurationManager.AppSettings["FilePathExcelValidacionesExcluyentesCAEA_Ítems"];
            string filePathExcelValidacionesNoExcluyentesEmpresa_Ítems = ConfigurationManager.AppSettings["FilePathExcelValidacionesNoExcluyentesCAEA_Ítems"];

            string filePathCasosEspecialesEmpresa = ConfigurationManager.AppSettings["FilePathCasosEspeciales"];

			// Si se especificó el archivo word y si dicho archivo existe
            if (string.IsNullOrEmpty(filePathWordIn) || !File.Exists(filePathWordIn))
            {
                Console.WriteLine($"Archivo Word de entrada:'{filePathWordIn}' no especificado o no existe.");
                return;
            }

            if (string.IsNullOrEmpty(filePathExcelOut))
            {
                Console.WriteLine($"Archivo Excel de salida:'{filePathExcelOut}' no especificado o no existe.");
                return;
            }

            #region Documentos Excel con las Reglas ORG para la Cabecera"
            if (!string.IsNullOrEmpty(filePathExcelValidacionesExcluyentesEmpresa) && !File.Exists(filePathExcelValidacionesExcluyentesEmpresa))
            {
                Console.WriteLine($"Archivo Excel de entrada de validaciones excluyentes:'{filePathExcelValidacionesExcluyentesEmpresa}' especificado en el config, no existe.");
                return;
            }

            if (!string.IsNullOrEmpty(filePathExcelValidacionesNoExcluyentesEmpresa) && !File.Exists(filePathExcelValidacionesNoExcluyentesEmpresa))
            {
                Console.WriteLine($"Archivo Excel de entrada de validaciones no excluyentes:'{filePathExcelValidacionesNoExcluyentesEmpresa}' especificado en el config, no existe.");
                return;
            }
            #endregion

            #region Documentos Excel con las Reglas ORG para el detalle (Ítems)"
            if (!string.IsNullOrEmpty(filePathExcelValidacionesExcluyentesEmpresa_Ítems) && !File.Exists(filePathExcelValidacionesExcluyentesEmpresa_Ítems))
            {
                Console.WriteLine($"Archivo Excel de entrada de validaciones excluyentes:'{filePathExcelValidacionesExcluyentesEmpresa_Ítems}' especificado en el config, no existe.");
                return;
            }

            if (!string.IsNullOrEmpty(filePathExcelValidacionesNoExcluyentesEmpresa_Ítems) && !File.Exists(filePathExcelValidacionesNoExcluyentesEmpresa_Ítems))
            {
                Console.WriteLine($"Archivo Excel de entrada de validaciones no excluyentes:'{filePathExcelValidacionesNoExcluyentesEmpresa_Ítems}' especificado en el config, no existe.");
                return;
            }
            #endregion

            if (!string.IsNullOrEmpty(filePathCasosEspecialesEmpresa) && !File.Exists(filePathCasosEspecialesEmpresa))
            {
                Console.WriteLine($"Archivo Excel de entrada de validaciones no excluyentes:'{filePathExcelValidacionesNoExcluyentesEmpresa}' especificado en el config, no existe.");
                return;
            }

            List<BuscadorExcel> listBuscadorExcelValidacionesExcluyentesEmpresa = new List<BuscadorExcel>();
            List<BuscadorExcel> listBuscadorExcelValidacionesNoExcluyentesEmpresa = new List<BuscadorExcel>();
            BuscadorExcel buscadorExcelPathCasosEspecialesEmpresa = null;

            #region Cargar los Excel, con las validaciones excluyentes
            if (!string.IsNullOrEmpty(filePathExcelValidacionesExcluyentesEmpresa))
            {
                listBuscadorExcelValidacionesExcluyentesEmpresa.Add(new BuscadorExcel(filePathExcelValidacionesExcluyentesEmpresa));
            }

            if (!string.IsNullOrEmpty(filePathExcelValidacionesExcluyentesEmpresa_Ítems))
            {
                listBuscadorExcelValidacionesExcluyentesEmpresa.Add(new BuscadorExcel(filePathExcelValidacionesExcluyentesEmpresa_Ítems));
            }
            #endregion


            #region Cargar los Excel, con las validaciones no excluyentes
            if (!string.IsNullOrEmpty(filePathExcelValidacionesNoExcluyentesEmpresa))
            {
                listBuscadorExcelValidacionesNoExcluyentesEmpresa.Add(new BuscadorExcel(filePathExcelValidacionesNoExcluyentesEmpresa));
            }

            if (!string.IsNullOrEmpty(filePathExcelValidacionesNoExcluyentesEmpresa_Ítems))
            {
                listBuscadorExcelValidacionesNoExcluyentesEmpresa.Add(new BuscadorExcel(filePathExcelValidacionesNoExcluyentesEmpresa_Ítems));
            }
            #endregion

            if (!string.IsNullOrEmpty(filePathCasosEspecialesEmpresa))
            {
                buscadorExcelPathCasosEspecialesEmpresa = new BuscadorExcel(filePathCasosEspecialesEmpresa);
            }

            string wordDocumentPath = filePathWordIn;
            string excelOutputPath = filePathExcelOut;

            ProcesarUsandoMétodo2(listBuscadorExcelValidacionesExcluyentesEmpresa, listBuscadorExcelValidacionesNoExcluyentesEmpresa, buscadorExcelPathCasosEspecialesEmpresa,
                                wordDocumentPath, excelOutputPath);

            Console.WriteLine("Proceso terminado. Pulse una tecla, para continuar...");
            Console.ReadKey();
        }

        private static void ApplyBoldFormatToRow(ExcelWorksheet worksheet, int rowNumber)
        {
            using (ExcelRange row = worksheet.Cells[rowNumber, 1, rowNumber, worksheet.Dimension.Columns])
            {
                row.Style.Font.Bold = true;
            }
        }

        private static void AppplyAutoFilter(ExcelWorksheet worksheet)
        {
            // Aplicar autofiltro a cada columna en la primera fila (excluyendo la última columna)
            int lastColumnIndex = worksheet.Dimension.End.Column;
            worksheet.Cells[1, 1, worksheet.Dimension.End.Row, lastColumnIndex - 1].AutoFilter = true;
        }

        private static void ApplyCellFormat(ExcelWorksheet worksheet)
        {
            // Aplicar formato de alineación a la columna 4 (D)
            int column4Index = 4; // Cambiar a la columna que desees (base 1)
            ApplyAlignmentFormatToColumn(worksheet, column4Index);

            // Aplicar formato de alineación a la columna 9 (I)
            int column9Index = 9; // Cambiar a la columna que desees (base 1)
            ApplyAlignmentFormatToColumn(worksheet, column9Index);

            int lastColumnIndex = worksheet.Dimension.End.Column;
            for (int columnIndex = 1; columnIndex <= lastColumnIndex; columnIndex++)
            {
                if( (columnIndex != column4Index) && (columnIndex != column9Index))
                {
                    ApplyAlignmentFormatTopToColumn(worksheet, columnIndex);
                }
            }

            // Se aplica formato, a las filas del encabezado
            lastColumnIndex = worksheet.Dimension.End.Column;
            for (int columnIndex = 1; columnIndex <= lastColumnIndex; columnIndex++)
            {
                ExcelRange column = worksheet.Cells[1, columnIndex, 1, columnIndex];
                column.Style.HorizontalAlignment = ExcelHorizontalAlignment.Justify;
                column.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }
        }

        private static void ApplyAlignmentFormatToColumn(ExcelWorksheet worksheet, int columnIndex)
        {
            int lastRowIndex = worksheet.Dimension.End.Row;
            using (ExcelRange column = worksheet.Cells[1, columnIndex, lastRowIndex, columnIndex])
            {
                column.Style.HorizontalAlignment = ExcelHorizontalAlignment.Justify;
                column.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }
        }

        private static void ApplyAlignmentFormatTopToColumn(ExcelWorksheet worksheet, int columnIndex)
        {
            int lastRowIndex = worksheet.Dimension.End.Row;
            using (ExcelRange column = worksheet.Cells[1, columnIndex, lastRowIndex, columnIndex])
            {
                column.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
            }
        }

        private static void SetColumnWidth(ExcelWorksheet worksheet)
        {
            worksheet.Column(2).Width = 2 * worksheet.Column(2).Width;

            // Establecer el ancho de las columnas desde la tercera hasta la penúltima
            int startColumnIndex = 3; // Cambiar al número de columna que desees (base 1)
            int endColumnIndex = worksheet.Dimension.End.Column - 1; // Penúltima columna

            SetColumnWidth(worksheet, startColumnIndex, endColumnIndex);
        }
        private static void SetColumnWidth(ExcelWorksheet worksheet, int startColumnIndex, int endColumnIndex)
        {
            List<int> listColumnIndexBy3 = new List<int>() { 3, 9, 10 };
            for (int columnIndex = startColumnIndex; columnIndex <= endColumnIndex; columnIndex++)
            {
                //worksheet.Column(columnIndex).Width = width;
                if (listColumnIndexBy3.Contains(columnIndex))
                {
                    worksheet.Column(columnIndex).Width = worksheet.Column(columnIndex).Width * 3;
                }
                else
                {
                    if(columnIndex == 4)
                    {
                        worksheet.Column(columnIndex).Width = worksheet.Column(columnIndex).Width * 4;
                    }
                    else if (columnIndex == 5)
                    {
                        worksheet.Column(columnIndex).Width = worksheet.Column(columnIndex).Width + (worksheet.Column(columnIndex).Width / 2);
                    }
                    else
                    {
                        worksheet.Column(columnIndex).Width = worksheet.Column(columnIndex).Width * 2;
                    }
                }
            }
        }


        /// <summary>
        /// En el ejemplo worksheet.View.FreezePanes(2, 1);, se establece que la fila 2 y la columna 1 (es decir, la columna A) serán el punto de 
        /// congelación. Esto significa que la primera fila (fila 1) permanecerá visible y se inmovilizará, así como la primera columna (columna A). 
        /// Todas las filas por encima de la fila 2 y todas las columnas a la izquierda de la columna 1 se mantendrán visibles mientras se desplaza 
        /// por la hoja de Excel, lo que facilita la visualización de encabezados o etiquetas importantes mientras se trabaja con el resto de la hoja.
        /// </summary>
        /// <param name="worksheet"></param>
        private static void InmovilizarPrimerFila(ExcelWorksheet worksheet)
        {
            // Inmovilizar la fila superior
            worksheet.View.FreezePanes(2, 1); // Inmovilizar la primera fila (fila 1)
        }

        private static void ProcesarUsandoMétodo2(List<BuscadorExcel> listBuscadorExcelValidacionesExcluyentesEmpresa, List<BuscadorExcel> listBuscadorExcelValidacionesNoExcluyentes2904,
            BuscadorExcel buscadorExcelCasosEspeciales2904, string wordDocumentPath, string excelOutputPath)
        {
			// Solamente, para pruebas
			int x = 10;
			int y = 2;
			x += ++y * 3;
			
            using (WordprocessingDocument doc = WordprocessingDocument.Open(wordDocumentPath, false))
            {
                // Obtener la tabla de Word
                //Table wordTable = doc.MainDocumentPart.Document.Body.Elements<Table>().FirstOrDefault();
                List<Table> listTable = new List<Table>();
                int i = 0;
                foreach (Table table in doc.MainDocumentPart.Document.Body.Elements<Table>())
                {
                    var xName = table.XName;
                    ++i;
                    if (i == 5 || i == 6)
                    {
                        listTable.Add(table);
                        if (i == 6)
                        {
                            break;
                        }
                    }
                }

                if (listTable.Count() > 1)
                {
                    // Para no tener problemas de licencia con el Excel
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;


                    // Crear un nuevo archivo de Excel
                    using (ExcelPackage package = new ExcelPackage())
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Tabla");

                        int row = 1;
                        int contWordTable = 0, contListValidaciones = -1; ;
                        foreach (Table wordTable in listTable)
                        {
                            ++contWordTable;
                            ++contListValidaciones;
                            string nombreRegistro = REGISTRO_PRINCIPAL;
                            if(contWordTable == 2)
                            {
                                nombreRegistro = REGISTRO_ÍTEMS;
                            }
                            // Recorrer las filas y columnas de la tabla de Word
                            foreach (TableRow tableRow in wordTable.Elements<TableRow>())
                            {
                                bool incrementarFila = true;
                                string cellTextDescripción = string.Empty;
                                int col = 0, colExcel = 0;
                                foreach (TableCell tableCell in tableRow.Elements<TableCell>())
                                {
                                    ++col;
                                    if ((row == 1) && (col == 1) && (nombreRegistro == REGISTRO_PRINCIPAL))
                                    {
                                        // Caso especial, fila cabecera
                                        colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "ID");
                                        colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "NOMBRE_REGISTRO");
                                    }

                                    if ((row > 1) && (col == 1))
                                    {
                                        colExcel = EscribirValorDeIdDeFila(worksheet, row, colExcel);
                                        colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, nombreRegistro);
                                    }

                                    if ((col < 6) || (col > 7))
                                    {
                                        continue;
                                    }

                                    // Obtener el texto de la celda de Word y copiarlo a la hoja de Excel
                                    string cellValue = tableCell.InnerText;
									colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, tableCell.InnerText);
									//colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, cellValue)

                                    if ((row == 1) && (nombreRegistro == REGISTRO_PRINCIPAL))
                                    {
                                        // Caso especial, fila cabecera

                                        if (col < 7)
                                        {
                                            continue;
                                        }
                                        // Escribe las coluimnas que falten del encabezado
                                        colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "COD_ERROR");
                                        colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "TIPO ERROR ORG");
                                        colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "EXCLUYENTE_S_N_I");
                                        colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "COD ERROR ORG");
                                        colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "DESCRIP ERROR ORG");
                                        colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "DISPARA EXCEPCIÓN S_N");
                                        colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "(*)");

                                        continue;
                                    }

                                    if (col == 6)
                                    {
                                        cellTextDescripción = tableCell.InnerText;
                                        colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, tableCell.InnerText);
                                        continue;
                                    }

                                    if (col == 7)
                                    {
                                        if (cellTextDescripción.Trim().ToLower() == "Fecha Servicio Desde".ToLower())
                                        {

                                        }
                                        string cellText = Util.GetCellText(tableCell);

                                        if (!string.IsNullOrWhiteSpace(cellText))
                                        {
                                            Traductor traductor = new Traductor();
                                            List<Bloque> bloques = 
                                                (nombreRegistro == REGISTRO_PRINCIPAL) ? 
                                                traductor.Traducir(cellText, Traductor.EParteAnalizada.Cabecera) : 
                                                (nombreRegistro == REGISTRO_ÍTEMS) ? 
                                                traductor.Traducir(cellText, Traductor.EParteAnalizada.Detalle) : 
                                                throw new Exception("Parte del comprobante a analizar, no definida");
                                            bool esLaPrimerFilaDeBloque = true;
                                            foreach (Bloque bloque in bloques)
                                            {
                                                if (esLaPrimerFilaDeBloque)
                                                {
                                                    colExcel = 3;
                                                    esLaPrimerFilaDeBloque = false;
                                                }
                                                else
                                                {
                                                    colExcel = 0;
                                                    colExcel = EscribirValorDeIdDeFila(worksheet, row, colExcel);
                                                    colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, nombreRegistro);
                                                    colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, cellTextDescripción);
                                                }

                                                colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, bloque.DescripciónBloque);
                                                colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, bloque.ValorErrorNatura);
                                                colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, bloque.TipoErrorAfip);
                                                switch (bloque.TipoErrorAfip)
                                                {
                                                    case Traductor.ERROR_ORG_EXCLUYENTE:
                                                        colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "S");
                                                        break;

                                                    case Traductor.ERROR_ORG_NO_EXCLUYENTE:
                                                        colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "N");
                                                        break;

                                                    case Traductor.ERROR_ORG_INDEFINIDO:
                                                        colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "I");
                                                        break;

                                                    default:
                                                        colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "");
                                                        break;

                                                }
                                                colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, bloque.ValorErrorAfip);
                                                colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel,
                                                    GetDescripciónErrorAfip(bloque, listBuscadorExcelValidacionesExcluyentesEmpresa[contListValidaciones], listBuscadorExcelValidacionesNoExcluyentes2904[contListValidaciones]));

                                                bool encontróEnCasosEspeciales = false;
                                                string disparaExcepcionCasoEspecial =
                                                    GetDisparaExcepciónSíNoCasoEspecial(nombreRegistro, cellTextDescripción, bloque, buscadorExcelCasosEspeciales2904,
                                                                                    ref encontróEnCasosEspeciales);
                                                if (encontróEnCasosEspeciales)
                                                {
                                                    colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, disparaExcepcionCasoEspecial);
                                                }
                                                else
                                                {
                                                    switch (bloque.TipoErrorAfip)
                                                    {
                                                        case Traductor.ERROR_ORG_EXCLUYENTE:
                                                            colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "S");
                                                            break;

                                                        case Traductor.ERROR_ORG_NO_EXCLUYENTE:
                                                            colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "N");
                                                            break;

                                                        case Traductor.ERROR_ORG_INDEFINIDO:
                                                            colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "S");
                                                            break;

                                                        default:
                                                            colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, "S");
                                                            break;

                                                    }
                                                }

                                                ++row;
                                                incrementarFila = false;
                                            }

                                        }
                                    }
                                }
                                if (incrementarFila)
                                {
                                    ++row;
                                }
                            }
                        }
                        

                        if (row > 1)
                        {
                            // Aplicar formato de negrita a la fila de encabezado (por ejemplo, fila 1)
                            int headerRowNumber = 1;
                            ApplyBoldFormatToRow(worksheet, headerRowNumber);

                            // Aplicar autofiltrado a todas las columnas, excepto la última
                            AppplyAutoFilter(worksheet);

                            // Aplicar formato a las celdas más necesitadas de formato
                            ApplyCellFormat(worksheet);

                            // Aplicar anhco de columnas
                            SetColumnWidth(worksheet);

                            // Inmovilizar la primer fila
                            InmovilizarPrimerFila(worksheet);
                        }

                        // Guardar el archivo de Excel
                        package.SaveAs(new System.IO.FileInfo(excelOutputPath));
                    }

                    Console.WriteLine("Tabla de Word exportada a Excel exitosamente.");
                }
                else
                {
                    Console.WriteLine("No se encontró una tabla en el documento de Word.");
                }

                while(listBuscadorExcelValidacionesExcluyentesEmpresa?.Count() > 0)
                {
                    listBuscadorExcelValidacionesExcluyentesEmpresa[0].Dispose();
                    listBuscadorExcelValidacionesExcluyentesEmpresa.RemoveAt(0);
                }

                while (listBuscadorExcelValidacionesNoExcluyentes2904?.Count() > 0)
                {
                    listBuscadorExcelValidacionesNoExcluyentes2904[0].Dispose();
                    listBuscadorExcelValidacionesNoExcluyentes2904.RemoveAt(0);
                }

                
                buscadorExcelCasosEspeciales2904?.Dispose();
            }
        }

        private static string GetDisparaExcepciónSíNoCasoEspecial(string nombreRegistro, string cellTextDescripción, Bloque bloque, 
                                    BuscadorExcel buscadorExcelPathCasosEspeciales2904, ref bool encontróEnCasosEspeciales2904)
        {
            encontróEnCasosEspeciales2904 = false;
            string disparaExcepcionCasoEspecial = string.Empty;
            if (buscadorExcelPathCasosEspeciales2904 == null)
            {
                return string.Empty;
            }
            ExcelWorksheet worksheet = buscadorExcelPathCasosEspeciales2904.ExcelPackage.Workbook.Worksheets[0];
            int rowCount = worksheet.Dimension.Rows;

            for (int row = 1; row <= rowCount; row++)
            {
                string valueInSecondColumn = worksheet.Cells[row, 2].Text; // Segunda columna
                string valueInThirthColumn = worksheet.Cells[row, 3].Text; // Tercera columna
                string valueInFourthColumn = worksheet.Cells[row, 4].Text; // Cuarta columna
                string valueInFifthColumn = worksheet.Cells[row, 5].Text; // Quinta columna
                string valueInSixthColumn = worksheet.Cells[row, 6].Text; // Sexta columna
                string valueInEighthColumn = worksheet.Cells[row, 8].Text; // Octava columna
                string valueInTenthColumn = worksheet.Cells[row, 10].Text; // Décima columna

                if (
                    (nombreRegistro == valueInSecondColumn) &&
                    (cellTextDescripción == valueInThirthColumn) &&
                    (bloque.DescripciónBloque == valueInFourthColumn) &&
                    (bloque.ValorErrorNatura == valueInFifthColumn) &&
                    (bloque.TipoErrorAfip == valueInSixthColumn) &&
                    (bloque.ValorErrorAfip == valueInEighthColumn)
                    )
                {
                    disparaExcepcionCasoEspecial = valueInTenthColumn;
                    encontróEnCasosEspeciales2904 = true;
                    break;
                }
            }
      
            return disparaExcepcionCasoEspecial;
        }


        private static int EscribirValorDeIdDeFila(ExcelWorksheet worksheet, int row, int colExcel)
        {
            colExcel = EscribirEnCeldaExcel(worksheet, row, colExcel, (row - 1).ToString());
            return colExcel;
        }

        private static string GetDescripciónErrorAfip(BloqueCod_error bloqueCod_Error, BuscadorExcel buscadorExcelValidacionesExcluyentes2904, 
                                            BuscadorExcel buscadorExcelValidacionesNoExcluyentes2904)
        {
            string descripciónReglaAfip = string.Empty;

            if(bloqueCod_Error.DescripAfipRule == BloqueCod_error.ERROR_ORG_EXCLUYENTE)
            {
                if(buscadorExcelValidacionesExcluyentes2904 != null && bloqueCod_Error.ValorAfipRule != BloqueCod_error.VALOR_ERROR_INDEFINIDO)
                {
                    return buscadorExcelValidacionesExcluyentes2904.FindCellByValueCodError(buscadorExcelValidacionesExcluyentes2904.ExcelPackage.Workbook.Worksheets[0], 
                                                                                bloqueCod_Error.ValorAfipRule)?.Text;
                }
            }
            else if (bloqueCod_Error.DescripAfipRule == BloqueCod_error.ERROR_ORG_NO_EXCLUYENTE)
            {
                if (buscadorExcelValidacionesNoExcluyentes2904 != null && bloqueCod_Error.ValorAfipRule != BloqueCod_error.VALOR_ERROR_INDEFINIDO)
                {
                    return buscadorExcelValidacionesNoExcluyentes2904.FindCellByValueCodError(buscadorExcelValidacionesNoExcluyentes2904.ExcelPackage.Workbook.Worksheets[0],
                                                                                bloqueCod_Error.ValorAfipRule)?.Text;
                }
            }
            else if (bloqueCod_Error.DescripAfipRule == BloqueCod_error.ERROR_ORG_INDEFINIDO)
            {

            }
            
            return descripciónReglaAfip;
        }

        private static string GetDescripciónErrorAfip(Bloque bloque, BuscadorExcel buscadorExcelValidacionesExcluyentes2904,
                                            BuscadorExcel buscadorExcelValidacionesNoExcluyentes2904)
        {
            string descripciónReglaAfip = string.Empty;

            if (bloque.TipoErrorAfip == Traductor.ERROR_ORG_EXCLUYENTE)
            {
                if (buscadorExcelValidacionesExcluyentes2904 != null && bloque.ValorErrorAfip != Traductor.VALOR_ERROR_INDEFINIDO)
                {
                    return buscadorExcelValidacionesExcluyentes2904.FindCellByValueCodError(buscadorExcelValidacionesExcluyentes2904.ExcelPackage.Workbook.Worksheets[0],
                                                                                bloque.ValorErrorAfip)?.Text;
                }
            }
            else if (bloque.TipoErrorAfip == Traductor.ERROR_ORG_NO_EXCLUYENTE)
            {
                if (buscadorExcelValidacionesNoExcluyentes2904 != null && bloque.ValorErrorAfip != Traductor.VALOR_ERROR_INDEFINIDO)
                {
                    return buscadorExcelValidacionesNoExcluyentes2904.FindCellByValueCodError(buscadorExcelValidacionesNoExcluyentes2904.ExcelPackage.Workbook.Worksheets[0],
                                                                                bloque.ValorErrorAfip)?.Text;
                }
            }
            else if (bloque.TipoErrorAfip == Traductor.ERROR_ORG_INDEFINIDO)
            {

            }

            return descripciónReglaAfip;
        }
        private static int EscribirEnCeldaExcel(ExcelWorksheet worksheet, int row, int colExcel, string valorCelda)
        {
            worksheet.Cells[row, ++colExcel].Value = valorCelda;
            return colExcel;
        }

    }

}

  