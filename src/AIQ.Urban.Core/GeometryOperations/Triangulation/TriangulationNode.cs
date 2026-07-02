namespace AIQ.Urban.Core.GeometryOperations.Triangulation;

internal class TriangulationNode
{
    private enum NodeType { Trapezoid, Vertex, Edge }

    private NodeType _nodeType;
    private object _associatedObj;
    private TriangulationNode? _leftChild;
    private TriangulationNode? _rightChild;
    private readonly List<TriangulationNode> _parents = [];

    internal TriangulationNode(TriangulationTrapezoid trapezoid, TriangulationNode? parent = null)
    {
        _nodeType = NodeType.Trapezoid;
        _associatedObj = trapezoid;
        trapezoid.AssociatedNode = this;
        if (parent is not null)
            _parents.Add(parent);
    }

    private TriangulationTrapezoid Trapezoid
    {
        get
        {
            if (_nodeType != NodeType.Trapezoid)
                throw new InvalidOperationException("Node is not a trapezoid.");
            return (TriangulationTrapezoid)_associatedObj;
        }
    }

    private TriangulationVertex Vertex
    {
        get
        {
            if (_nodeType != NodeType.Vertex)
                throw new InvalidOperationException("Node is not a vertex.");
            return (TriangulationVertex)_associatedObj;
        }
    }

    private TriangulationEdge Edge
    {
        get
        {
            if (_nodeType != NodeType.Edge)
                throw new InvalidOperationException("Node is not an edge.");
            return (TriangulationEdge)_associatedObj;
        }
    }

    private TriangulationNode LeftChild
    {
        get => _leftChild ?? throw new InvalidOperationException("Node has no left child.");
    }

    private TriangulationNode RightChild
    {
        get => _rightChild ?? throw new InvalidOperationException("Node has no right child.");
    }

    internal void ReplaceByAnotherNodeInTree(TriangulationNode newNode)
    {
        if (_nodeType != NodeType.Trapezoid)
            throw new InvalidOperationException("Only a trapezoid node can be replaced.");
        if (newNode._nodeType != NodeType.Trapezoid)
            throw new InvalidOperationException("Can only replace with a trapezoid node.");
        if (newNode == this)
            return;

        foreach (var parent in _parents)
        {
            if (parent._leftChild == this)
                parent._leftChild = newNode;
            else if (parent._rightChild == this)
                parent._rightChild = newNode;
        }

        newNode._parents.AddRange(_parents);
    }

    internal void InsertVertex(TriangulationVertex vertex)
    {
        EnsureIsRoot();

        var area = SearchAreaContainingVertex(vertex);
        area.SplitByVertex(vertex);
    }

    internal void InsertEdge(
        TriangulationEdge edge,
        bool topJustInserted,
        bool bottomJustInserted)
    {
        EnsureIsRoot();

        var startNode = SearchAreaContainingVertex(edge.MidPoint);

        var nodesToSplitDown = startNode.FindNodesToSplitInDirection(edge, upDirection: false);
        var nodesToSplitUp = startNode.FindNodesToSplitInDirection(edge, upDirection: true);

        var createdTrapCouples = new List<(TriangulationTrapezoid Left, TriangulationTrapezoid Right)>();

        for (int i = nodesToSplitUp.Count - 1; i >= 0; i--)
            nodesToSplitUp[i].SplitByEdge(edge, createdTrapCouples);

        startNode.SplitByEdge(edge, createdTrapCouples);

        foreach (var node in nodesToSplitDown)
            node.SplitByEdge(edge, createdTrapCouples);

        TriangulationTrapezoid.ManageAdjacentTrapezoidsAfterEdgeSplit(
            edge, createdTrapCouples, topJustInserted, bottomJustInserted);

        TriangulationTrapezoid.MergeRedundantTrapezoids(createdTrapCouples);
    }

    internal List<TriangulationTrapezoid> GetAllTraps()
    {
        var result = new List<TriangulationTrapezoid>();
        CollectTraps(result);
        return result;
    }

    private void CollectTraps(List<TriangulationTrapezoid> acc)
    {
        if (_nodeType == NodeType.Trapezoid)
        {
            acc.Add(Trapezoid);
        }
        else
        {
            LeftChild.CollectTraps(acc);
            RightChild.CollectTraps(acc);
        }
    }

    private TriangulationNode SearchAreaContainingVertex(TriangulationVertex vertex)
    {
        if (_nodeType == NodeType.Trapezoid)
            return this;

        TriangulationNode relevantChild = LeftChild;

        if ((_nodeType == NodeType.Vertex && vertex > Vertex)
            || (_nodeType == NodeType.Edge && Edge.IsVertexAtTheRight(vertex)))
        {
            relevantChild = RightChild;
        }

        return relevantChild.SearchAreaContainingVertex(vertex);
    }

    private void SplitByVertex(TriangulationVertex vertex)
    {
        var (bottomTrap, topTrap) = Trapezoid.SplitByVertex(vertex);

        _nodeType = NodeType.Vertex;
        _associatedObj = vertex;

        _leftChild = new TriangulationNode(bottomTrap, this);
        _rightChild = new TriangulationNode(topTrap, this);
    }

    private void SplitByEdge(
        TriangulationEdge edge,
        List<(TriangulationTrapezoid Left, TriangulationTrapezoid Right)> createdTrapCouples)
    {
        var (leftTrap, rightTrap) = Trapezoid.SplitByEdge(edge);

        createdTrapCouples.Add((leftTrap, rightTrap));

        _nodeType = NodeType.Edge;
        _associatedObj = edge;

        _leftChild = new TriangulationNode(leftTrap, this);
        _rightChild = new TriangulationNode(rightTrap, this);
    }

    private List<TriangulationNode> FindNodesToSplitInDirection(TriangulationEdge edge, bool upDirection)
    {
        var nodesToSplit = new List<TriangulationNode>();
        var currentTrap = Trapezoid;

        while (!IsEndOfEdge(currentTrap, edge, upDirection))
        {
            var nextTraps = currentTrap.GetAdjacentTraps(top: upDirection);

            switch (nextTraps.Count)
            {
                case 1:
                    currentTrap = nextTraps[0];
                    break;

                case 2:
                    var leftTrapInDirection = nextTraps[0];
                    var leftTrapRightmostPt = leftTrapInDirection.GetExtremePoint(
                        top: !upDirection, right: true);
                    currentTrap = edge.IsVertexAtTheRight(leftTrapRightmostPt)
                        ? nextTraps[0]
                        : nextTraps[1];
                    break;

                default:
                    throw new InvalidOperationException("Inconsistent trapezoid neighborhood.");
            }

            nodesToSplit.Add(currentTrap.AssociatedNode!);
        }

        return nodesToSplit;
    }

    private static bool IsEndOfEdge(
        TriangulationTrapezoid trap,
        TriangulationEdge edge,
        bool upDirection)
    {
        var relevantVertex = upDirection ? trap.TopVertex : trap.BottomVertex;
        var edgeVertex = upDirection ? edge.TopVertex : edge.BottomVertex;
        return relevantVertex is not null && relevantVertex.Value == edgeVertex;
    }

    private void EnsureIsRoot()
    {
        if (_parents.Count != 0)
            throw new InvalidOperationException("This method must be called on the root node.");
    }
}
