using System.Windows.Media;

namespace GrimmDiff.Gui;

public sealed record MascotOption(string Id, string Name, Brush Outline, Brush Body, Brush Accent, Brush Highlight, Brush? Detail = null);
