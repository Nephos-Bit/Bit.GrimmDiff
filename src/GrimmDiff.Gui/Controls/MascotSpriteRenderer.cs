using System.Windows;
using System.Windows.Media;
using GrimmDiff.Gui.Models;

namespace GrimmDiff.Gui.Controls;

internal static class MascotSpriteRenderer
{
    private const int PixelSize = 2;

    public static void Render(DrawingContext context, MascotOption option, Brush outline, Brush body,
        Brush accent, Brush highlight, bool animated, bool isAnalyzing, int frame, int gazeX, int gazeY)
    {        if (option.Id != MascotCatalog.DefaultId)
        {
            DrawVariant(context, option.Id, outline, body, accent, highlight, option.Detail, animated, isAnalyzing, frame, gazeX, gazeY);
            return;
        }

        var bob = animated && (isAnalyzing ? frame % 4 >= 2 : frame % 6 >= 3) ? 1 : 0;
        var blinking = animated && !isAnalyzing && frame % 12 == 11;

        // All coordinates are whole pixels on an 18 × 18 grid.
        PixelRect(context, outline, 8, 1, 2, 2, bob);
        PixelRect(context, accent, 8, 1, 2, 1, bob);
        PixelRect(context, outline, 4, 2, 10, 1, bob);
        PixelRect(context, outline, 3, 3, 12, 12, bob);
        PixelRect(context, outline, 2, 6, 14, 6, bob);
        PixelRect(context, body, 4, 4, 10, 10, bob);
        PixelRect(context, accent, 4, 4, 10, 1, bob);
        PixelRect(context, accent, 2, 7, 1, 4, bob);
        PixelRect(context, accent, 15, 7, 1, 4, bob);
        PixelRect(context, outline, 4, 6, 10, 6, bob);
        PixelRect(context, body, 5, 7, 8, 4, bob);
        PixelRect(context, highlight, 5, 12, 1, 1, bob);
        PixelRect(context, highlight, 12, 12, 1, 1, bob);
        PixelRect(context, outline, 7, 15, 4, 1, bob);
        PixelRect(context, accent, 8, 16, 2, 1, bob);

        if (blinking)
        {
            PixelRect(context, highlight, 6, 8, 2, 1, bob);
            PixelRect(context, highlight, 10, 8, 2, 1, bob);
        }
        else
        {
            PixelRect(context, highlight, 5, 7, 3, 3, bob);
            PixelRect(context, highlight, 10, 7, 3, 3, bob);
            PixelRect(context, outline, 6 + gazeX, 8 + gazeY, 1, 1, bob);
            PixelRect(context, outline, 11 + gazeX, 8 + gazeY, 1, 1, bob);
        }

        if (animated && isAnalyzing)
        {
            PixelRect(context, highlight, 5 + frame % 7, 4, 2, 1, bob);
            PixelRect(context, accent, 8 + frame % 3, 13, 2, 1, bob);
        }
    }

    private static void DrawVariant(DrawingContext context, string variantId, Brush o, Brush b, Brush a, Brush h, Brush? detail, bool animated, bool isAnalyzing, int frame, int gazeX, int gazeY)
    {
        var bob = animated && (isAnalyzing ? frame % 4 >= 2 : frame % 6 >= 3) ? 1 : 0;
        var blinking = animated && !isAnalyzing && frame % 12 == 11;
        void P(Brush brush, int x, int y, int width, int height) => PixelRect(context, brush, x, y, width, height, bob);

        switch (variantId)
        {
            case "ember": // Fox: pointed ears, pale muzzle and a small tail.
                P(o, 2, 1, 4, 6); P(o, 12, 1, 4, 6); P(a, 3, 2, 2, 3); P(a, 13, 2, 2, 3);
                P(o, 3, 5, 12, 9); P(b, 4, 6, 10, 7); P(a, 4, 6, 10, 3);
                P(h, 5, 10, 8, 3); P(o, 8, 11, 2, 1); P(o, 15, 11, 2, 3); P(a, 16, 10, 1, 2);
                P(o, 5, 14, 3, 2); P(o, 10, 14, 3, 2);
                Eyes(5, 7, 11, 7);
                break;
            case "luma": // Owl: round head, ear tufts and broad wings.
                P(o, 3, 2, 3, 4); P(o, 12, 2, 3, 4); P(a, 4, 3, 2, 2); P(a, 12, 3, 2, 2);
                P(o, 4, 4, 10, 11); P(b, 5, 5, 8, 9); P(a, 2, 8, 3, 5); P(a, 13, 8, 3, 5);
                P(h, 5, 7, 3, 4); P(h, 10, 7, 3, 4); P(o, 8, 11, 2, 2); P(a, 6, 13, 6, 2);
                P(o, 5, 15, 3, 1); P(o, 10, 15, 3, 1);
                Eyes(6, 8, 11, 8);
                break;
            case "bolt": // Robot: antenna, square faceplate and two feet.
                P(o, 8, 1, 2, 3); P(h, 8, 1, 2, 1); P(o, 3, 4, 12, 10);
                P(b, 4, 5, 10, 8); P(a, 5, 6, 8, 5); P(o, 2, 8, 1, 4); P(o, 15, 8, 1, 4);
                P(h, 6, 7, 2, 2); P(h, 10, 7, 2, 2); P(o, 7, 11, 4, 1);
                P(o, 5, 14, 3, 2); P(o, 10, 14, 3, 2); P(h, 6, 15, 2, 1); P(h, 10, 15, 2, 1);
                Eyes(6, 7, 10, 7);
                break;
            case "sprig": // Sprout: two leaves above a face in a flowerpot.
                P(o, 8, 3, 2, 5); P(o, 3, 2, 5, 4); P(o, 10, 1, 5, 5);
                P(a, 4, 3, 3, 2); P(a, 11, 2, 3, 3); P(o, 4, 7, 10, 7);
                P(b, 5, 8, 8, 5); P(h, 6, 10, 6, 2); P(o, 3, 13, 12, 2);
                P(a, 4, 15, 10, 1); P(o, 5, 16, 8, 1);
                Eyes(6, 9, 11, 9);
                break;
            case "wisp": // Ghost: rounded head, raised arms and a tapering tail.
                P(o, 6, 2, 6, 1); P(o, 4, 3, 10, 2); P(o, 3, 5, 12, 9);
                P(b, 4, 5, 10, 8); P(a, 4, 5, 10, 2); P(o, 1, 8, 2, 4); P(o, 15, 8, 2, 4);
                P(a, 2, 9, 2, 2); P(a, 14, 9, 2, 2); P(o, 5, 14, 9, 1);
                P(b, 6, 14, 7, 2); P(a, 8, 16, 4, 1); P(h, 5, 7, 2, 1);
                Eyes(6, 8, 11, 8);
                break;
            case "sgt-schnabel": // Yellow rubber duck in profile with an orange bill.
                P(o, 5, 2, 7, 1); P(o, 4, 3, 9, 1); P(o, 3, 4, 10, 6);
                P(b, 4, 4, 8, 6); P(h, 5, 4, 5, 1);
                P(o, 1, 9, 4, 4); P(o, 2, 10, 12, 5); P(b, 2, 10, 2, 2);
                P(b, 3, 10, 10, 4); P(h, 5, 11, 6, 2); P(o, 4, 15, 9, 1);
                P(o, 12, 7, 5, 3); P(a, 13, 7, 3, 1); P(a, 12, 8, 5, 1);
                if (blinking) P(o, 9, 6, 2, 1);
                else { P(h, 9, 5, 2, 2); P(o, 9 + gazeX, 5 + gazeY, 1, 1); }
                break;
            case "berta": // White hen with a red comb and an orange beak.
                P(o, 7, 1, 2, 4); P(o, 10, 1, 2, 4); P(o, 5, 2, 2, 4);
                P(a, 6, 3, 5, 3); P(a, 8, 2, 3, 2);
                P(o, 4, 5, 10, 9); P(b, 5, 6, 8, 7);
                P(o, 2, 10, 3, 4); P(o, 13, 10, 3, 4);
                P(b, 3, 11, 2, 2); P(b, 13, 11, 2, 2);
                P(o, 5, 13, 8, 3); P(b, 6, 13, 6, 2);
                P(o, 5, 16, 3, 1); P(o, 10, 16, 3, 1);
                if (detail is not null) { P(detail, 8, 10, 2, 1); P(detail, 9, 11, 1, 1); }
                Eyes(6, 7, 11, 7);
                break;
        }

        if (animated && isAnalyzing) P(h, 8 + frame % 3, 4, 2, 1);

        void Eyes(int leftX, int leftY, int rightX, int rightY)
        {
            if (blinking)
            {
                P(o, leftX, leftY + 1, 2, 1); P(o, rightX, rightY + 1, 2, 1);
            }
            else
            {
                P(h, leftX, leftY, 2, 2); P(h, rightX, rightY, 2, 2);
                P(o, leftX + gazeX, leftY + gazeY, 1, 1);
                P(o, rightX + gazeX, rightY + gazeY, 1, 1);
            }
        }
    }

    private static void PixelRect(DrawingContext context, Brush brush, int x, int y, int width, int height, int bob)
    {
        context.DrawRectangle(brush, null,
            new Rect(x * PixelSize, (y + bob) * PixelSize, width * PixelSize, height * PixelSize));
    }

}
