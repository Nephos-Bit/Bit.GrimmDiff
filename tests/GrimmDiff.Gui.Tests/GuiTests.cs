using GrimmDiff.Gui;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

var language = "English";
var text = new LocalizedTextService(() => language);

if (text["Appearance"] != "Appearance" || text["RepositoryApprovalDescription"] != "Approval applies only to the currently open repository.")
    throw new Exception("Expected English settings text.");

language = "Deutsch";
if (text["Appearance"] != "Darstellung" || text["RepositoryApprovalDescription"] != "Die Freigabe gilt nur für das aktuell geöffnete Repository." ||
    text["ChooseMascot"] != "MASKOTTCHEN W\u00C4HLEN")
    throw new Exception("Expected German settings text.");

Console.WriteLine("PASS Gui project structure");
Console.WriteLine("PASS settings text switches language");

var mascots = MascotCatalog.Options;
if (mascots.Count != 8 || mascots.Select(mascot => mascot.Id).Distinct().Count() != 8 ||
    mascots.Select(mascot => mascot.Name).Distinct().Count() != 8 ||
    mascots.Any(mascot => mascot.Outline is null || mascot.Body is null || mascot.Accent is null || mascot.Highlight is null) ||
    MascotCatalog.Find("sgt-schnabel").Name != "Sgt. Schnabel" ||
    MascotCatalog.Find("berta").Name != "Berta" ||
    MascotCatalog.Find("missing").Id != MascotCatalog.DefaultId)
    throw new Exception("Expected eight named, selectable mascots and a safe fallback.");

Console.WriteLine("PASS mascot options");

Exception? renderingError = null;
var renderThread = new Thread(() =>
{
    try
    {
        var metadata = (FrameworkPropertyMetadata)ReviewMascotControl.VariantProperty.GetMetadata(typeof(ReviewMascotControl));
        if (!metadata.AffectsRender)
            throw new Exception("Changing a mascot variant must invalidate its visual.");

        var control = new ReviewMascotControl();
        control.Measure(new Size(36, 36));
        control.Arrange(new Rect(0, 0, 36, 36));

        byte[] Snapshot()
        {
            var image = new RenderTargetBitmap(36, 36, 96, 96, PixelFormats.Pbgra32);
            image.Render(control);
            var pixels = new byte[36 * 36 * 4];
            image.CopyPixels(pixels, 36 * 4, 0);
            return pixels;
        }

        bool ContainsColor(byte[] pixels, string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            for (var index = 0; index < pixels.Length; index += 4)
            {
                if (pixels[index] == color.B && pixels[index + 1] == color.G &&
                    pixels[index + 2] == color.R && pixels[index + 3] == color.A)
                    return true;
            }
            return false;
        }

        void FlushRender()
        {
            var frame = new DispatcherFrame();
            control.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        var snapshots = new List<byte[]> { Snapshot() };
        foreach (var option in MascotCatalog.Options.Skip(1))
        {
            control.Variant = option.Id;
            FlushRender();
            var changed = Snapshot();
            if (snapshots.Any(previous => previous.SequenceEqual(changed)))
                throw new Exception($"Mascot {option.Name} did not produce a distinct rendered preview.");
            if (option.Id == "sgt-schnabel" &&
                (!ContainsColor(changed, "#F4CA3D") || !ContainsColor(changed, "#E98226")))
                throw new Exception("Sgt. Schnabel must render a yellow body and orange bill.");
            if (option.Id == "berta" &&
                (!ContainsColor(changed, "#FFFDF3") || !ContainsColor(changed, "#D93B2A")))
                throw new Exception("Berta must render a white body and red comb.");
            snapshots.Add(changed);
        }

        control.SetValue(ReviewMascotControl.OutlineBrushProperty, null);
        FlushRender();
        if (Snapshot().All(value => value == 0))
            throw new Exception("Variant palettes should not depend on the default mascot brushes.");

        control.Variant = MascotCatalog.DefaultId;
        FlushRender();
        if (Snapshot().Any(value => value != 0))
            throw new Exception("A mascot with a missing selected brush should not be rendered.");

        control.ClearValue(ReviewMascotControl.OutlineBrushProperty);
        control.Variant = "wisp";
        FlushRender();
        control.Variant = null;
        FlushRender();
        if (!snapshots[0].SequenceEqual(Snapshot()))
            throw new Exception("A null mascot variant should render the default mascot.");
    }
    catch (Exception exception)
    {
        renderingError = exception;
    }
});
renderThread.SetApartmentState(ApartmentState.STA);
renderThread.Start();
renderThread.Join();
if (renderingError is not null) throw renderingError;

Console.WriteLine("PASS mascot preview redraws after variant changes");
