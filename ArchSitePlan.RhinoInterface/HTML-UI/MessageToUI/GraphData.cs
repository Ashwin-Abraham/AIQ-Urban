using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchSitePlan.RhinoInterface.HTML_UI.MessageToUI;

public record GraphData(
    string Label,
    double Data,
    Color Color);
