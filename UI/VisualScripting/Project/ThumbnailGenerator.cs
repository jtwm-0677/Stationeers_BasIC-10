using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BasicToMips.UI.VisualScripting.Project
{
    /// <summary>
    /// Generates thumbnail preview images for visual script projects
    /// </summary>
    public static class ThumbnailGenerator
    {
        private const int ThumbnailWidth = 200;
        private const int ThumbnailHeight = 150;

        /// <summary>
        /// Capture a thumbnail from a visual canvas
        /// </summary>
        public static BitmapSource CaptureCanvasThumbnail(FrameworkElement canvas)
        {
            if (canvas == null)
            {
                return CreateEmptyThumbnail();
            }

            try
            {
                // Ensure the canvas is rendered
                canvas.UpdateLayout();

                // Get the actual bounds
                var bounds = VisualTreeHelper.GetDescendantBounds(canvas);
                if (bounds.IsEmpty)
                {
                    return CreateEmptyThumbnail();
                }

                // Calculate scale to fit thumbnail size
                double scaleX = ThumbnailWidth / bounds.Width;
                double scaleY = ThumbnailHeight / bounds.Height;
                double scale = Math.Min(scaleX, scaleY);

                // Limit scale to avoid too small details
                scale = Math.Min(scale, 1.0);

                int renderWidth = (int)(bounds.Width * scale);
                int renderHeight = (int)(bounds.Height * scale);

                // Create render target
                var renderTarget = new RenderTargetBitmap(
                    renderWidth,
                    renderHeight,
                    96, 96,
                    PixelFormats.Pbgra32);

                // Create drawing visual with transform
                var drawingVisual = new DrawingVisual();
                using (var context = drawingVisual.RenderOpen())
                {
                    var brush = new VisualBrush(canvas)
                    {
                        Stretch = Stretch.Uniform
                    };

                    context.DrawRectangle(
                        brush,
                        null,
                        new Rect(0, 0, renderWidth, renderHeight));
                }

                renderTarget.Render(drawingVisual);

                // Create final thumbnail with centered image on background
                return CreateCenteredThumbnail(renderTarget, renderWidth, renderHeight);
            }
            catch
            {
                return CreateEmptyThumbnail();
            }
        }

        /// <summary>
        /// Save a thumbnail to a PNG file
        /// </summary>
        public static void SaveThumbnail(BitmapSource thumbnail, string filePath)
        {
            try
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(thumbnail));

                using var stream = File.Create(filePath);
                encoder.Save(stream);
            }
            catch
            {
                // Ignore thumbnail save errors
            }
        }

        /// <summary>
        /// Load a thumbnail from a PNG file
        /// </summary>
        public static BitmapSource? LoadThumbnail(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Create an empty thumbnail with background
        /// </summary>
        private static BitmapSource CreateEmptyThumbnail()
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                // Draw background
                context.DrawRectangle(
                    new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    null,
                    new Rect(0, 0, ThumbnailWidth, ThumbnailHeight));

                // Draw "No Preview" text
                var text = new FormattedText(
                    "No Preview",
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    14,
                    Brushes.Gray,
                    96);

                context.DrawText(text,
                    new Point(
                        (ThumbnailWidth - text.Width) / 2,
                        (ThumbnailHeight - text.Height) / 2));
            }

            var renderTarget = new RenderTargetBitmap(
                ThumbnailWidth,
                ThumbnailHeight,
                96, 96,
                PixelFormats.Pbgra32);

            renderTarget.Render(visual);
            return renderTarget;
        }

        /// <summary>
        /// Create a centered thumbnail with background
        /// </summary>
        private static BitmapSource CreateCenteredThumbnail(BitmapSource source, int sourceWidth, int sourceHeight)
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                // Draw background
                context.DrawRectangle(
                    new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                    null,
                    new Rect(0, 0, ThumbnailWidth, ThumbnailHeight));

                // Center the image
                double x = (ThumbnailWidth - sourceWidth) / 2.0;
                double y = (ThumbnailHeight - sourceHeight) / 2.0;

                context.DrawImage(source, new Rect(x, y, sourceWidth, sourceHeight));
            }

            var renderTarget = new RenderTargetBitmap(
                ThumbnailWidth,
                ThumbnailHeight,
                96, 96,
                PixelFormats.Pbgra32);

            renderTarget.Render(visual);
            return renderTarget;
        }

        /// <summary>
        /// Get thumbnail path for a project
        /// </summary>
        public static string GetThumbnailPath(string projectFolderPath)
        {
            return Path.Combine(projectFolderPath, "thumbnail.png");
        }

        /// <summary>
        /// Capture and save thumbnail for a project
        /// </summary>
        public static void CaptureAndSaveProjectThumbnail(FrameworkElement canvas, string projectFolderPath)
        {
            try
            {
                var thumbnail = CaptureCanvasThumbnail(canvas);
                var thumbnailPath = GetThumbnailPath(projectFolderPath);
                SaveThumbnail(thumbnail, thumbnailPath);
            }
            catch
            {
                // Ignore thumbnail errors
            }
        }

        /// <summary>
        /// Load thumbnail for a project, or create empty if not found
        /// </summary>
        public static BitmapSource LoadProjectThumbnail(string projectFolderPath)
        {
            var thumbnailPath = GetThumbnailPath(projectFolderPath);
            return LoadThumbnail(thumbnailPath) ?? CreateEmptyThumbnail();
        }
    }
}
