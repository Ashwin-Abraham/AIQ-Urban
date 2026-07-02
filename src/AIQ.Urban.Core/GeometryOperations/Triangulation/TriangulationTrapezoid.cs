namespace AIQ.Urban.Core.GeometryOperations.Triangulation;

internal class TriangulationTrapezoid
{
    private static readonly Dictionary<TriangulationEdge, HashSet<TriangulationTrapezoid>> TrapsByRightEdge = new();

    internal TriangulationVertex? TopVertex { get; set; }
    internal TriangulationVertex? BottomVertex { get; set; }
    internal List<TriangulationTrapezoid> TrapezoidsAbove { get; set; } = [];
    internal List<TriangulationTrapezoid> TrapezoidsBelow { get; set; } = [];
    internal TriangulationEdge? LeftEdge { get; set; }

    private TriangulationEdge? _rightEdge;

    internal TriangulationNode? AssociatedNode { get; set; }

    internal TriangulationTrapezoid()
    {
    }

    private TriangulationTrapezoid(
        TriangulationVertex? topVertex,
        TriangulationVertex? bottomVertex,
        TriangulationEdge? leftEdge,
        TriangulationEdge? rightEdge)
    {
        TopVertex = topVertex;
        BottomVertex = bottomVertex;
        LeftEdge = leftEdge;
        _rightEdge = rightEdge;
        if (rightEdge is not null)
            RegisterInEdgeRegistry(rightEdge.Value);
    }

    internal TriangulationEdge? RightEdge
    {
        get => _rightEdge;
        set
        {
            RemoveFromEdgeRegistry();
            _rightEdge = value;
            if (value is not null)
                RegisterInEdgeRegistry(value.Value);
        }
    }

    internal bool IsInside
    {
        get
        {
            if (!_isInsideComputed)
            {
                _isInsideValue = ComputeIsInside();
                _isInsideComputed = true;
            }
            return _isInsideValue;
        }
    }
    private bool _isInsideComputed;
    private bool _isInsideValue;

    private bool ComputeIsInside()
    {
        if (_rightEdge is null || LeftEdge is null)
            return false;

        if (!TrapsByRightEdge.TryGetValue(LeftEdge.Value, out var leftTraps))
            return false;

        if (leftTraps.Count == 0)
            return false;

        var leftTrap = leftTraps.First();
        return !leftTrap.IsInside;
    }

    internal List<TriangulationTrapezoid> GetAdjacentTraps(bool top)
        => top ? TrapezoidsAbove : TrapezoidsBelow;

    internal void SetAdjacentTraps(List<TriangulationTrapezoid> newTraps, bool top)
    {
        if (top)
            TrapezoidsAbove = newTraps;
        else
            TrapezoidsBelow = newTraps;
    }

    internal void RemoveFromEdgeRegistry()
    {
        if (_rightEdge is not null)
        {
            if (TrapsByRightEdge.TryGetValue(_rightEdge.Value, out var traps))
                traps.Remove(this);
        }
    }

    internal (TriangulationTrapezoid Bottom, TriangulationTrapezoid Top) SplitByVertex(TriangulationVertex vertex)
    {
        var topTrap = this;
        var bottomTrap = Duplicate();

        topTrap.BottomVertex = vertex;
        bottomTrap.TopVertex = vertex;

        bottomTrap.TrapezoidsAbove = [topTrap];
        bottomTrap.TrapezoidsBelow = TrapezoidsBelow;
        foreach (var trap in TrapezoidsBelow)
            Replace(trap.TrapezoidsAbove, this, bottomTrap);
        topTrap.TrapezoidsBelow = [bottomTrap];

        return (bottomTrap, topTrap);
    }

    internal (TriangulationTrapezoid Left, TriangulationTrapezoid Right) SplitByEdge(TriangulationEdge edge)
    {
        var rightTrap = this;
        var leftTrap = Duplicate();

        leftTrap.RightEdge = edge;
        rightTrap.LeftEdge = edge;

        return (leftTrap, rightTrap);
    }

    internal TriangulationVertex GetExtremePoint(bool top, bool right)
    {
        var relevantVertex = top ? TopVertex : BottomVertex;
        var relevantEdge = right ? RightEdge : LeftEdge;

        if (relevantVertex is null || relevantEdge is null)
            throw new InvalidOperationException("Extreme point does not exist.");

        var extremeY = relevantVertex.Value.Y;
        var extremeX = relevantEdge.Value.GetXByY(extremeY);

        return new TriangulationVertex(extremeX, extremeY);
    }

    private TriangulationTrapezoid Duplicate()
        => new(TopVertex, BottomVertex, LeftEdge, _rightEdge);

    private void RegisterInEdgeRegistry(TriangulationEdge edge)
    {
        if (!TrapsByRightEdge.TryGetValue(edge, out var traps))
        {
            traps = [];
            TrapsByRightEdge[edge] = traps;
        }
        traps.Add(this);
    }

    internal static void ClearEdgeRegistry()
        => TrapsByRightEdge.Clear();

    internal static void ManageAdjacentTrapezoidsAfterEdgeSplit(
        TriangulationEdge edge,
        List<(TriangulationTrapezoid Left, TriangulationTrapezoid Right)> createdTrapCouples,
        bool topJustInserted,
        bool bottomJustInserted)
    {
        ManageAdjacentTrapezoidAtInsertedEdgeEnd(
            edge, createdTrapCouples[0].Left, createdTrapCouples[0].Right,
            topJustInserted, topEnd: true);

        var last = createdTrapCouples[^1];
        ManageAdjacentTrapezoidAtInsertedEdgeEnd(
            edge, last.Left, last.Right,
            bottomJustInserted, topEnd: false);

        for (int i = 0; i < createdTrapCouples.Count - 1; i++)
        {
            var (topLeft, topRight) = createdTrapCouples[i];
            var (bottomLeft, bottomRight) = createdTrapCouples[i + 1];

            if (topRight.TrapezoidsBelow.Count == 2)
            {
                ManageAdjacentTrapezoidsOnBranch(
                    edge, bottomLeft, bottomRight, topLeft, topRight,
                    upwardBranch: false);
            }
            else if (bottomRight.TrapezoidsAbove.Count == 2)
            {
                ManageAdjacentTrapezoidsOnBranch(
                    edge, topLeft, topRight, bottomLeft, bottomRight,
                    upwardBranch: true);
            }
            else
            {
                if (bottomRight.TrapezoidsAbove.Count != 1
                    || topRight.TrapezoidsBelow.Count != 1)
                    throw new InvalidOperationException("Inconsistent trapezoid neighborhood.");

                topLeft.TrapezoidsBelow = [bottomLeft];
                bottomLeft.TrapezoidsAbove = [topLeft];
            }
        }
    }

    internal static void MergeRedundantTrapezoids(
        List<(TriangulationTrapezoid Left, TriangulationTrapezoid Right)> createdTrapCouples)
    {
        for (int side = 0; side <= 1; side++)
        {
            var stackToMerge = new List<TriangulationTrapezoid>
            {
                side == 0 ? createdTrapCouples[0].Left : createdTrapCouples[0].Right
            };

            for (int i = 1; i < createdTrapCouples.Count; i++)
            {
                var trap = side == 0 ? createdTrapCouples[i].Left : createdTrapCouples[i].Right;

                if (stackToMerge[^1].LeftEdge != trap.LeftEdge
                    || stackToMerge[^1]._rightEdge != trap._rightEdge)
                {
                    MergeTrapezoidsStack(stackToMerge);
                    stackToMerge = [];
                }

                stackToMerge.Add(trap);
            }

            MergeTrapezoidsStack(stackToMerge);
        }
    }

    internal static void ManageAdjacentTrapezoidAtInsertedEdgeEnd(
        TriangulationEdge edge,
        TriangulationTrapezoid endTrapLeft,
        TriangulationTrapezoid endTrapRight,
        bool endJustInserted,
        bool topEnd)
    {
        var exteriorAdjacentTraps = endTrapRight.GetAdjacentTraps(top: topEnd);

        if (endJustInserted)
        {
            if (exteriorAdjacentTraps.Count != 1)
                throw new InvalidOperationException("Inconsistent trapezoid neighborhood.");

            endTrapLeft.SetAdjacentTraps([.. exteriorAdjacentTraps], top: topEnd);
            var adjacentTrap = exteriorAdjacentTraps[0];
            adjacentTrap.SetAdjacentTraps([endTrapLeft, endTrapRight], top: !topEnd);
            return;
        }

        var edgeRelevantEnd = TriangulationEdge.GetEdgeVertex(edge, top: topEnd);
        var leftEdgeEnd = TriangulationEdge.GetEdgeVertex(endTrapLeft.LeftEdge, top: topEnd);
        var rightEdgeEnd = TriangulationEdge.GetEdgeVertex(endTrapRight.RightEdge, top: topEnd);

        if (leftEdgeEnd is not null && leftEdgeEnd.Value == edgeRelevantEnd!.Value)
        {
            if (exteriorAdjacentTraps.Count != 1)
                throw new InvalidOperationException("Inconsistent trapezoid neighborhood.");
        }
        else if (rightEdgeEnd is not null && rightEdgeEnd.Value == edgeRelevantEnd!.Value)
        {
            if (exteriorAdjacentTraps.Count != 1)
                throw new InvalidOperationException("Inconsistent trapezoid neighborhood.");

            endTrapLeft.SetAdjacentTraps(exteriorAdjacentTraps, top: topEnd);
            endTrapRight.SetAdjacentTraps([], top: topEnd);
            Replace(
                exteriorAdjacentTraps[0].GetAdjacentTraps(top: !topEnd),
                endTrapRight,
                endTrapLeft);
        }
        else
        {
            if (exteriorAdjacentTraps.Count != 2)
                throw new InvalidOperationException("Inconsistent trapezoid neighborhood.");

            var leftAdjacent = exteriorAdjacentTraps[0];
            var rightAdjacent = exteriorAdjacentTraps[1];
            endTrapLeft.SetAdjacentTraps([leftAdjacent], top: topEnd);
            endTrapRight.SetAdjacentTraps([rightAdjacent], top: topEnd);
            Replace(
                leftAdjacent.GetAdjacentTraps(top: !topEnd),
                endTrapRight,
                endTrapLeft);
        }
    }

    internal static void ManageAdjacentTrapezoidsOnBranch(
        TriangulationEdge edge,
        TriangulationTrapezoid leftTrapA,
        TriangulationTrapezoid rightTrapA,
        TriangulationTrapezoid leftTrapB,
        TriangulationTrapezoid rightTrapB,
        bool upwardBranch)
    {
        if (rightTrapA.GetAdjacentTraps(top: !upwardBranch).Count != 1)
            throw new InvalidOperationException("Inconsistent trapezoid neighborhood.");

        leftTrapA.SetAdjacentTraps([leftTrapB], top: !upwardBranch);

        var branchPoint = rightTrapB.GetAdjacentTraps(top: upwardBranch)[0]
            .GetExtremePoint(top: !upwardBranch, right: true);

        if (edge.IsVertexAtTheRight(branchPoint))
        {
            leftTrapB.SetAdjacentTraps([leftTrapA], top: upwardBranch);
            return;
        }

        var additionalLeftTrapA = rightTrapB.GetAdjacentTraps(top: upwardBranch)[0];

        rightTrapA.SetAdjacentTraps([rightTrapB], top: !upwardBranch);
        rightTrapB.SetAdjacentTraps([rightTrapA], top: upwardBranch);

        leftTrapB.SetAdjacentTraps(
            [additionalLeftTrapA, leftTrapA],
            top: upwardBranch);
        additionalLeftTrapA.SetAdjacentTraps([leftTrapB], top: !upwardBranch);
    }

    internal static void MergeTrapezoidsStack(List<TriangulationTrapezoid> trapezoidsStack)
    {
        if (trapezoidsStack.Count < 2)
            return;

        var topTrap = trapezoidsStack[0];
        var bottomTrap = trapezoidsStack[^1];

        topTrap.BottomVertex = bottomTrap.BottomVertex;
        topTrap.TrapezoidsBelow = bottomTrap.TrapezoidsBelow;

        foreach (var trap in bottomTrap.TrapezoidsBelow)
            Replace(trap.TrapezoidsAbove, bottomTrap, topTrap);

        for (int i = 1; i < trapezoidsStack.Count; i++)
        {
            var trap = trapezoidsStack[i];
            trap.AssociatedNode?.ReplaceByAnotherNodeInTree(topTrap.AssociatedNode!);
            trap.RemoveFromEdgeRegistry();
        }
    }

    private static void Replace<T>(List<T> list, T toReplace, T replaceBy)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (Equals(list[i], toReplace))
                list[i] = replaceBy;
        }
    }
}
