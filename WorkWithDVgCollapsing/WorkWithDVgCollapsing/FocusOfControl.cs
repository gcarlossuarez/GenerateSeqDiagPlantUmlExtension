using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WorkWithDVgCollapsing
{
    public class FocusOfControl
    {
        public int RectId { get; private set; }

        public int CandidateKey { get; private set; }

        public string ContentId { get; private set; } = string.Empty;

        public List<int> Subordinates { get; private set; } = new List<int>();

        public FocusOfControl(XElement rect, int candidateKey, string contentId)
        {
            int rectId;
            if (int.TryParse(rect.Attribute("id")?.Value, out rectId))
            {
                this.RectId = rectId;
                this.CandidateKey = candidateKey;
                this.ContentId = contentId;
            }
        }
    }
}
