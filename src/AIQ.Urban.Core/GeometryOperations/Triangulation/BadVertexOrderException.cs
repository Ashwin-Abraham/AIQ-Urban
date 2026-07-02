namespace AIQ.Urban.Core.GeometryOperations.Triangulation;

internal class BadVertexOrderException : Exception
{
    internal BadVertexOrderException()
        : base("Triangle vertices are not in counter-clockwise order.")
    {
    }
}
