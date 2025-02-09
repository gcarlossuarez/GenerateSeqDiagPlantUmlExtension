using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateSeqDiagPlantUmlExtension
{
    using Microsoft.VisualStudio.Shell;
    using System.ComponentModel;

    public class OptionsPage : DialogPage
    {
        [Category("Configuration")]
        [DisplayName("Directorio Base")]
        [Description("Directorio donde se guardarán los archivos generados.")]
        public string BaseDirectory { get; set; } = @"D:\MyTemp\PlantUmlTemp";

        [Category("Configuration")]
        [DisplayName("Máximum Deep")]
        [Description("Máximum Deep for Analisys. Default:200. You can experiment with different depth levels, to get higher-level or more detailed views.")]
        public int MaxDeep { get; set; } = 200;

        [Category("Configuration")]
        [DisplayName("Format Output diagram")]
        [Description("PlantUML output format for PlantUML File. Png = Image (For view in Image Viewer), Svg = (For view in Web Navigatgor) For view in Web Navigator, None = Only text file es showed.")]
        public FormatPlantUmlOutputDiagram FormatPlantUmlOutputDiagram { get; set; } = FormatPlantUmlOutputDiagram.Png;

        [Category("Configuration")]
        [DisplayName("PlantUML JAR file path")]
        [Description("Full path to the PlantUML JAR file (by default, generates a text file. This format is optional. None, does not generate the diagram; but does generate the text file.")]
        public string FullPathPlantUmlJar { get; set; }

        [Category("Configuration")]
        [DisplayName("Encoding PlantUML Text file")]
        [Description("Encoding desired in PlantUML Text file. Default:UTF-8.")]
        public Encoding EncodingPlantUmlTextFile { get; set; } = Encoding.UTF8;

        [Category("Configuration")]
        [DisplayName("PlantUML Limit Size.")]
        [Description("It's recommended a hig value due in the real life most of the methods ar complex.Default:8192")]
        public int PlantUmlLimitSize { get; set; } = 8192;

        [Category("Configuration")]
        [DisplayName("Dpi size.")]
        [Description("It's recommended a hig value due in the real life most of the methods ar complex.Default:300")]
        public int Dpi { get; set; } = 300;
    }

    public enum FormatPlantUmlOutputDiagram
    {
        None,
        Png,
        Svg
    }

}
