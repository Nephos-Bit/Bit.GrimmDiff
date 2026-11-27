using System.Windows.Media;

namespace GrimmDiff.Gui.Models;

public sealed record MascotOption(string Id, string Name, Brush Outline, Brush Body, Brush Accent, Brush Highlight, Brush? Detail = null);
