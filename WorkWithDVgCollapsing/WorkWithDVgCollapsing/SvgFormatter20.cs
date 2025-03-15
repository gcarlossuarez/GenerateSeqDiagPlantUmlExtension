using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WorkWithDVgCollapsing
{
    public class SvgFormatter20
    {
        public static string Format(string inputFile)
        {
            FileInfo fileInfoInputfile = new FileInfo(inputFile);
            string outputFile = Path.Combine(fileInfoInputfile.DirectoryName ?? ".\\",
                Path.GetFileNameWithoutExtension(inputFile) + "_formatted" + fileInfoInputfile.Extension);
            if (!File.Exists(inputFile))
            {
                Console.WriteLine("❌ SVG file not found. Generate one with PlantUML first.");
                return string.Empty;
            }

            // Cargar el contenido del SVG
            string svgContent = File.ReadAllText(inputFile);

            // Parse SVG as an XML document
            XDocument svgDoc = XDocument.Parse(svgContent);

            // Defining the SVG namespace
            XNamespace svgNamespace = "http://www.w3.org/2000/svg";

            var lines = svgDoc.Descendants(svgNamespace + "line").OrderBy(x => x.Attribute("x1")?.Value);
            // Get all rectangles with color "lightblue" ==> "#ADD8E6"
            var rectangles = svgDoc.Descendants(svgNamespace + "rect")
                .Where(r => r.HasAttributes && r.Attribute("fill")?.Value == "#ADD8E6")
                .OrderByDescending(x=> x.Attribute("x")?.Value)
                .ThenBy(y=>y.Attribute("y")?.Value)
                .ToList();

            int groupId = 1;
            int elementId = 1;
            List<string> widgetsDataList = new List<string>();
            List<FocusOfControl> focusOfControls = new List<FocusOfControl>();

            List<XElement> xlementProcessed = new List<XElement>();
            List<XElement> rectsProcessed = new List<XElement>();
            // Process each rectangle
            foreach (XElement rect in rectangles)
            {
                if(xlementProcessed.Contains(rect)) continue;

                // Get the coordinates of the rectangle
                double rectX = GetCoordinate(rect, "x");
                double rectY = GetCoordinate(rect, "y");
                double rectWidth = GetCoordinate(rect, "width");
                double rectHeight = GetCoordinate(rect, "height");

                // Create a new collapsible group
                XElement newGroup = new XElement(svgNamespace + "g");
                newGroup.SetAttributeValue("id", $"grupo{groupId}");
                newGroup.SetAttributeValue("class", "collapsible");
                newGroup.SetAttributeValue("onclick", $"toggleGroup({groupId}, event)");

                // Create an internal group for content
                XElement contentGroup = new XElement(svgNamespace + "g");
                string contentId = $"contenido{groupId}";
                contentGroup.SetAttributeValue("id", contentId);
                contentGroup.SetAttributeValue("class", "contenido");
                contentGroup.SetAttributeValue("style", "visibility:visible;");

                // Collect items that are inside the rectangle
                var elementsToMove = svgDoc.Descendants()
                    .Where(element =>
                        element != rect && // To avoid include himself
                        (IsElementInsideRectangleBasicAnalisis(element, rectX, rectY, rectWidth, rectHeight) ||
                        IsElementInsideRectangle(element, rectX, rectY, rectWidth, rectHeight)))
                    .ToList();

                // Insert the new group after the rectangle
                rect.AddAfterSelf(newGroup);

                // If the content group has items, add it to the new group
                if (elementsToMove.Any())
                {
                    //rect.SetAttributeValue("id", $"{elementId++}");
                    //contentGroup.Add(rect);
                }

                // Move collected items to the content group
                foreach (XElement element in elementsToMove)
                {
                    if(xlementProcessed.Contains(element)) continue;

                    elementId = AddElementToGroup(xlementProcessed, element, widgetsDataList, elementId, groupId,
                        contentGroup, primaryElement: 0);
                }

                // If the content group has items, add it to the new group
                if (contentGroup.HasElements)
                {
                    elementId = AddElementToGroup(xlementProcessed, rect, widgetsDataList, elementId, groupId,
                        contentGroup, primaryElement:0);

                    // Create <text> element with attributes
                    XElement textElement = new XElement(svgNamespace + "text",
                        new XAttribute("fill", "#000000"),
                        new XAttribute("font-family", "sans-serif"),
                        new XAttribute("font-size", "13"),
                        new XAttribute("font-weight", "bold"),
                        new XAttribute("lengthAdjust", "spacing"),
                        new XAttribute("textLength", "15.1709"),
                        new XAttribute("x", rectX - 8),
                        new XAttribute("y", rectY),
                        "-"
                    );
                    elementId = AddElementToGroup(xlementProcessed, textElement, widgetsDataList, elementId, groupId,
                        contentGroup, primaryElement:0, removeFirst:false);

                    // Create<rect> element with attributes
                    XElement rectElement = new XElement(svgNamespace + "rect",
                        new XAttribute("fill", "green"),
                        new XAttribute("height", "10"),
                        new XAttribute("style", "stroke:#000000;stroke-width:1.5;"),
                        new XAttribute("width", "10"),
                        new XAttribute("x", rectX - 10),
                        new XAttribute("y", rectY),
                        new XAttribute("text", "-")
                    );

                    int candidateKey = elementId;
                    elementId = AddElementToGroup(xlementProcessed, rectElement, widgetsDataList, elementId, groupId,
                        contentGroup, primaryElement: 1, removeFirst: false);

                    UpdateFocusControlDependencies(svgDoc, svgNamespace, rect, candidateKey, contentId, focusOfControls, rectX, rectY, rectHeight);

                    // Add the content group to the new group
                    newGroup.Add(contentGroup);

                    // Increment group ID
                    groupId++;
                }
                else
                {
                    // If there are no items in the group, delete it
                    newGroup.Remove();
                }
            }

            // Save the modified SVG
            svgDoc.Save(outputFile);

            List<string> focusControlsStrList = new List<string>();
            foreach (var f in focusOfControls)
            {
                if (f.Subordinates.Any())
                {
                    string focusControlsStr = $"{{ rectId:{f.RectId}, candidateKey:{f.CandidateKey}, contentId:'{f.ContentId}', subordinates:new Set([{string.Join(", ", f.Subordinates)}]) }}";

                    focusControlsStrList.Add(focusControlsStr);
                }
            }
        

            string focusControls = $@"const focusControls = [
{string.Join(", \n", focusControlsStrList)}];";

            string widgest = $@"const widgets = [
		{string.Join(", \n", widgetsDataList)} 
		];";
            // Insert the interactivity script
            string interactiveScript = @"
    <script><![CDATA[
		" + widgest + "\n" + focusControls + "\n" + @"

        function toggleGroup(groupId, event) {
		    console.log('touched');

            if (!event) return;

            // Prevent the event from spreading to other elements
            event.stopPropagation();

            // Obtener el elemento sobre el que se hizo clic
            const clickedElement = event.target;

            // Verify that the click was on a <text>
            if (clickedElement.tagName !== 'rect') return;

            // Get the text ID
            const rectId = parseInt(clickedElement.getAttribute(""id""));
            
            // Find text in widgets with primaryElement = 1
            const rectData = widgets.find(r => r.id === rectId && r.primaryElement === 1);
            
            // If the text is not the main one, exit
            if (!rectData) return;

            const rectIdPrimaryElement = rectId;
            console.log(`groupId: ${groupId}, rectId: ${rectIdPrimaryElement}`);

            // Get the content group
            const content = document.getElementById(`contenido${groupId}`);
            if (!content) return;

            let isCollapsed = 0;

            // Toggle group visibility based on the state of the main text
            if (clickedElement.getAttribute(""text"") === ""+"") {
                // Expand
                console.log('Expanding...');
                clickedElement.setAttribute(""text"", ""-"");
                clickedElement.setAttribute(""fill"", ""green"");
                isCollapsed = 1;
            } else {
                // Collapse
                console.log('Collapsing...');
                clickedElement.setAttribute(""text"", ""+"");
                clickedElement.setAttribute(""fill"", ""blue"");
            }

            // Change visibility of group elements
            content.childNodes.forEach(element => {
                if (element.nodeType === Node.ELEMENT_NODE && parseInt(element.getAttribute(""id"")) !== rectIdPrimaryElement) {
                    element.style.visibility = isCollapsed ? 'visible' : 'hidden';
                    element.style.opacity = isCollapsed ? '1' : '0';
                    element.style.pointerEvents = isCollapsed ? 'auto' : 'none';
                }
            });


            // We look for the element in focusControls
			const currentElement = focusControls.find(control => control.candidateKey === rectIdPrimaryElement);
			if(currentElement){
				currentElement.subordinates.forEach(subordinatedId => {
					console.log(`  Subordinado ID: ${subordinatedId}`);
					
					// Buscar todos los elementos <id> dentro del SVG
					const id = document.getElementById(subordinatedId);
					if(id){
						console.log(id);
						
						// Get the group (<g>) that contains the <id>
						const groupContent = id.parentElement
						if(groupContent){
							// Change visibility of group elements
							groupContent.childNodes.forEach(element => {
								//console.log(element);
                                if (element.nodeType === Node.ELEMENT_NODE) {
                                    // Find text in widgets with primaryElement = 1
                                    const rectData = widgets.find(r => r.id === parseInt(element.getAttribute(""id"")));
                                    if(rectData.primaryElement === 1){
                                        // Expanding
                                        if(isCollapsed){
                                            element.setAttribute(""text"", ""-"");
                                            element.setAttribute(""fill"", ""green"");
                                        }
                                        else{
                                            // Collapsing.
                                            element.setAttribute(""text"", ""+"");
                                            element.setAttribute(""fill"", ""blue"");
                                        }
                                    }
                                    else{
									    element.style.visibility = isCollapsed ? 'visible' : 'hidden';
									    element.style.opacity = isCollapsed ? '1' : '0';
									    element.style.pointerEvents = isCollapsed ? 'auto' : 'none';
								    }
                                }
							});
						}
					}
				});
			}
        }
    ]]></script>
</svg>";

            string finalSvgContent = File.ReadAllText(outputFile);
            finalSvgContent = finalSvgContent.Replace("</svg>", interactiveScript);
            File.WriteAllText(outputFile, finalSvgContent, Encoding.UTF8);

            Console.WriteLine($"✅ SVG interactivo generado sin errores: {outputFile}");

            return outputFile;
        }

        private static void UpdateFocusControlDependencies(XDocument svgDoc, XNamespace svgNamespace, XElement rect,
            int candidateKey, string contentId, List<FocusOfControl> focusOfControls, double rectX, double rectY,
            double rectHeight)
        {
            if (int.TryParse(rect.Attribute("id")?.Value, out int rectId) &&
                focusOfControls.Any(x => x.RectId == rectId))
            {
                return;
            }

            FocusOfControl focusOfControl = new FocusOfControl(rect, candidateKey, contentId);
            foreach (var f in focusOfControls)
            {
                XElement element = svgDoc.Descendants(svgNamespace + "rect").FirstOrDefault(x =>
                        x.HasAttributes && int.TryParse(x.Attribute("id")?.Value, out rectId) && rectId == f.RectId);
                if (element == null) continue;

                double rectXFocusControl = GetCoordinate(element, "x");
                double rectYFocusControl = GetCoordinate(element, "y");
                double rectWidthFocusControl = GetCoordinate(element, "width");
                double rectHeightFocusControl = GetCoordinate(element, "height");
                if (rectX < rectXFocusControl &&
                    rectY <= rectYFocusControl &&
                    (rectY + rectHeight) >= (rectYFocusControl + rectHeightFocusControl))
                {
                    // The new control focus is contained in the previously loaded control focus
                    focusOfControl.Subordinates.Add(f.RectId);
                }
            }
            focusOfControls.Add(focusOfControl);
        }

        private static int AddElementToGroup(List<XElement> xlementProcessed, XElement element, List<string> widgetsDataList, int elementId,
            int groupId, XElement contentGroup, int primaryElement, bool removeFirst = true)
        {
            xlementProcessed.Add(element);

            if (removeFirst) element.Remove();

            string heightStrValue = element.Attribute("height") != null ? element.Attribute("height")?.Value : "10";
            widgetsDataList.Add($"{{ id: {elementId}, height: {heightStrValue}, groupId:{groupId}, primaryElement:{primaryElement} }}");
            element.SetAttributeValue("id", $"{elementId++}");
            contentGroup.Add(element);
            return elementId;
        }

        static bool IsElementInsideRectangleBasicAnalisis(XElement element, double rectX, double rectY, double rectWidth, double rectHeight)
        {
            // Get the coordinates of the element
            double x = GetCoordinate(element, "x");
            double y = GetCoordinate(element, "y");
            double width = GetCoordinate(element, "width");
            double height = GetCoordinate(element, "height");

            // Check if the element is inside the rectangle

            return IsInside(rectX, rectY, rectHeight, y, x, width);

            //return x >= rectX && y >= rectY && x + width <= rectX + rectWidth && y + height <= rectY + rectHeight;
        }

        private static bool IsInside(double rectX, double rectY, double rectHeight, double y, double x, double width)
        {
            // NOTE. - In an SVG file, the coordinate system works with the y-axis growing downwards. That is, the coordinate (0,0) is
            // at the top left corner of the SVG, and as y increases, elements shift downwards.
            bool sw1 = rectY <= y && y <= rectY + rectHeight;

            // NOTE. - In an SVG file, the x-axis starts at zero on the left and increases to the right. That is, the coordinate (0,0) is
            // at the top left corner of the SVG, and as the x-coordinates increase, elements shift to the right.
            // Case "return "Hello from ClassA" in mermaid diagram:https://mermaid.live/edit#pako:eNqFUstOwzAQ_JWVT4nog74uASJVIJUDoEockCActsm2tZTYxXaKqqr_ziZOKgEt5OSd2RmPN7sXqc5IRMLSR0kqpTuJK4NFooC_DRonU7lB5WA-ALQwTZ02J8hhRc6NPqMdVXSao7XTE-y4Ym_PsZOa1crqnBLlGzB1couO-GIPcLpuHHMZwSNKFVhnpFq9vQOalQ19z5Nmgd6Sqdv8fU0ouAFFnw0WhHC9MP243_dkb85erkW9MyBLGnpGLhiEV02QYRXkgp8cfeeP0XMHEmK49GWtGUO3Ts8aQ640ChJxT3muYWl00Y5GeAXllqq3QFDbhP_4zLTOFjs66aSyNpWPzcOO2lH3Xox09CAVBRj-nPrEAxn9CY1-Q_y7REcUZAqUGa_dvmpJhFtTQYmI-JjREsvcVRkP3Iql0887lYrImZI6wuhytRbREnkKHVFuMrZtdvaI8t68at3Why8QjucX
            bool sw2 = rectX <= x; // To the right of rect x corner

            // NOTE. - In an SVG file, the x-axis starts at zero on the left and increases to the right. That is, the coordinate (0,0) is
            // at the top left corner of the SVG, and as the x-coordinates increase, elements shift to the right.
            // Case method classA.Get(1) and if condicion (alt block) in mermaid diagram:https://mermaid.live/edit#pako:eNqFUstOwzAQ_JWVT4nog74uASJVIJUDoEockCActsm2tZTYxXaKqqr_ziZOKgEt5OSd2RmPN7sXqc5IRMLSR0kqpTuJK4NFooC_DRonU7lB5WA-ALQwTZ02J8hhRc6NPqMdVXSao7XTE-y4Ym_PsZOa1crqnBLlGzB1couO-GIPcLpuHHMZwSNKFVhnpFq9vQOalQ19z5Nmgd6Sqdv8fU0ouAFFnw0WhHC9MP243_dkb85erkW9MyBLGnpGLhiEV02QYRXkgp8cfeeP0XMHEmK49GWtGUO3Ts8aQ640ChJxT3muYWl00Y5GeAXllqq3QFDbhP_4zLTOFjs66aSyNpWPzcOO2lH3Xox09CAVBRj-nPrEAxn9CY1-Q_y7REcUZAqUGa_dvmpJhFtTQYmI-JjREsvcVRkP3Iql0887lYrImZI6wuhytRbREnkKHVFuMrZtdvaI8t68at3Why8QjucX
            bool sw3 = (x <= rectX && rectX <= (x + width)); // x corner of the rect is inside object

            return sw1 && (sw2 || sw3);
        }

        static bool IsElementInsideRectangle(XElement element, double rectX, double rectY, double rectWidth, double rectHeight)
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            switch (element.Name.LocalName)
            {
                case "polygon":
                {
                    var points = element.Attribute("points")?.Value;
                    if (string.IsNullOrEmpty(points))
                        return false;

                    var coordinates = points.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture))
                        .ToArray();

                    minX = coordinates.Where((_, i) => i % 2 == 0).Min();
                    maxX = coordinates.Where((_, i) => i % 2 == 0).Max();
                    minY = coordinates.Where((_, i) => i % 2 == 1).Min();
                    maxY = coordinates.Where((_, i) => i % 2 == 1).Max();

                    break;
                }

                case "rect":
                    {
                        // For rectangles, use the attributes directly
                        minX = GetCoordinate(element, "x");
                        minY = GetCoordinate(element, "y");
                        maxX = minX + GetCoordinate(element, "width");
                        maxY = minY + GetCoordinate(element, "height");
                        break;
                    }

                case "line":
                    {
                        // For lines, use the attributes x1, y1, x2, y2
                        double x1 = GetCoordinate(element, "x1");
                        double y1 = GetCoordinate(element, "y1");
                        double x2 = GetCoordinate(element, "x2");
                        double y2 = GetCoordinate(element, "y2");

                        minX = Math.Min(x1, x2);
                        minY = Math.Min(y1, y2);
                        maxX = Math.Max(x1, x2);
                        maxY = Math.Max(y1, y2);
                        return IsInside(rectX, rectY, rectHeight, minY, minX, maxX - minX);
                        break;
                    }

                case "path":
                    {
                        // For paths, parse the 'd' attribute
                        var pathData = element.Attribute("d")?.Value;
                        if (string.IsNullOrEmpty(pathData))
                            return false;

                        // Splitting the chain correctly
                        var points = pathData.Split(new[] { ' ', 'M', 'L', 'C', 'Z', 'm', 'l', 'c', 'z' }, StringSplitOptions.RemoveEmptyEntries)
                        .SelectMany(s => s.Split(',')) // Split by commas
                        .Where(s => !string.IsNullOrEmpty(s)) // Ignore empty fragments
                        .Select(s => double.Parse(s, System.Globalization.CultureInfo.InvariantCulture)) // Convert to double
                        .ToArray();

                        // Calculate the bounding box of the path
                        for (int i = 0; i < points.Length; i += 2)
                        {
                            double x = points[i];
                            double y = points[i + 1];

                            if (x < minX) minX = x;
                            if (y < minY) minY = y;
                            if (x > maxX) maxX = x;
                            if (y > maxY) maxY = y;
                        }
                        break;
                    }

                case "text":
                {
                    // For text elements, use the 'x' and 'y' attributes
                    minX = GetCoordinate(element, "x");
                    minY = GetCoordinate(element, "y");

                    // Estimate the width and height based on font size
                    double fontSize = GetCoordinate(element, "font-size");
                    if (fontSize == 0) fontSize = 12; // Default font size if not specified

                    // Estimate width based on text length (this is a rough approximation)
                    string textContent = element.Value;
                    double textWidth = textContent.Length * fontSize * 0.6; // Adjust factor as needed

                    maxX = minX + textWidth;
                    maxY = minY + fontSize; // Height is approximately the font size
                    break;
                }

                default:
                    // For other elements, it cannot be determined
                    return false;
            }

            // Check if the bounding box is inside the rectangle
            return IsInside(rectX, rectY, rectHeight, minY, minX, maxX);
            
            //return minX >= rectX && minY >= rectY &&
            //       maxX <= rectX + rectWidth &&
            //       maxY <= rectY + rectHeight;
        }

        private static bool IsFullyInside(double rectX, double rectY, double rectWidth, double rectHeight, double minX, double minY, double maxX, double maxY)
        {
            return (minX >= rectX && maxX <= rectX + rectWidth) &&
                   (minY >= rectY && maxY <= rectY + rectHeight);
        }


        static double GetCoordinate(XElement element, string attributeName)
        {
            // Get the attribute value and convert it to double
            string value = element.Attribute(attributeName)?.Value;
            if (value != null && double.TryParse(value, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double result))
            {
                return result;
            }
            return 0;
        }
    }
}
