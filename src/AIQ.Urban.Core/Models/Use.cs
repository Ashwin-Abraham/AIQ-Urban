using System.Drawing;

namespace AIQ.Urban.Core.Models;

public class Use(Guid id, string name, Color color)
{
    public Guid Id { get; } = id;
    public string Name { get; set; } = name;
    public Color Color { get; set; } = color;
}
