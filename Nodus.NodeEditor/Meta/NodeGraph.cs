using System.Collections.Generic;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Meta;

public record NodeGraph(string GraphName, IEnumerable<NodeData> Nodes, IEnumerable<Connection> Connections);