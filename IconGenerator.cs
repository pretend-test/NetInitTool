using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

internal static class IconGenerator
{
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    public static void Main(string[] args)
    {
        var output = args.Length > 0 ? args[0] : "NetInitTool.ico";
        using (var bitmap = new Bitmap(256, 256, PixelFormat.Format32bppArgb))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.Clear(Color.Transparent);

            using (var background = new SolidBrush(Color.FromArgb(31, 111, 235)))
            using (var border = new Pen(Color.FromArgb(255, 255, 255), 8F))
            {
                graphics.FillEllipse(background, 18, 18, 220, 220);
                graphics.DrawEllipse(border, 22, 22, 212, 212);
            }

            using (var globePen = new Pen(Color.FromArgb(225, 243, 255), 7F))
            {
                graphics.DrawEllipse(globePen, 54, 55, 112, 112);
                graphics.DrawArc(globePen, 79, 55, 62, 112, 90, 180);
                graphics.DrawArc(globePen, 79, 55, 62, 112, 270, 180);
                graphics.DrawLine(globePen, 58, 111, 162, 111);
                graphics.DrawArc(globePen, 54, 72, 112, 78, 180, 180);
                graphics.DrawArc(globePen, 54, 72, 112, 78, 0, 180);
            }

            using (var wrenchFill = new SolidBrush(Color.FromArgb(255, 205, 74)))
            using (var wrenchEdge = new Pen(Color.FromArgb(118, 75, 8), 6F))
            {
                var handle = new Point[]
                {
                    new Point(140, 173), new Point(159, 193), new Point(205, 147),
                    new Point(186, 128)
                };
                graphics.FillPolygon(wrenchFill, handle);
                graphics.DrawPolygon(wrenchEdge, handle);

                var head = new Point[]
                {
                    new Point(170, 126), new Point(164, 108), new Point(171, 91),
                    new Point(188, 80), new Point(210, 83), new Point(227, 96),
                    new Point(233, 113), new Point(227, 128), new Point(215, 137),
                    new Point(202, 123), new Point(194, 119), new Point(186, 125),
                    new Point(196, 139)
                };
                graphics.FillPolygon(wrenchFill, head);
                graphics.DrawPolygon(wrenchEdge, head);

                using (var cutout = new SolidBrush(Color.FromArgb(31, 111, 235)))
                {
                    var jawOpening = new Point[]
                    {
                        new Point(200, 91), new Point(217, 96), new Point(225, 105),
                        new Point(218, 116), new Point(207, 111), new Point(200, 104)
                    };
                    graphics.FillPolygon(cutout, jawOpening);
                }
            }

            bitmap.Save(Path.ChangeExtension(output, ".png"), ImageFormat.Png);
            var iconHandle = bitmap.GetHicon();
            using (var icon = Icon.FromHandle(iconHandle))
            using (var stream = File.Create(output))
            {
                icon.Save(stream);
            }
            DestroyIcon(iconHandle);
        }
    }
}
