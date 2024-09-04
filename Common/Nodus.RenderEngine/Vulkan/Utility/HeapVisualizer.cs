using System.Numerics;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.Memory;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Nodus.RenderEngine.Vulkan.Utility;

public static class HeapVisualizer
{
    public static void Visualize(IVkMemoryHeap[] heaps, string path)
    {
        var heapBarWidth = 512;
        var heapBarHeight = 48;
        var heapBarPadding = 16;
        var heapTitleSize = 24;

        var heapSectionHeight = heapTitleSize + heapBarHeight;
        
        if (!SystemFonts.TryGet("Lato", out var family))
        {
            throw new Exception("Failed to get Roboto system font.");
        }

        var font = family.CreateFont(heapTitleSize, FontStyle.Regular);
        var textOptions = new TextOptions(font) { Dpi = 72, KerningMode = KerningMode.Standard };
        var largestTextWidth = heaps.Max(x => TextMeasurer.MeasureSize(GetHeapLabel(x), textOptions).Width);
        heapBarWidth = Math.Max((int)largestTextWidth, heapBarWidth);
        
        using var image = new Image<Rgba32>(
            heapBarWidth + heapBarPadding * 2, 
            (heapBarHeight + heapTitleSize + heapBarPadding * 2) * heaps.Length - heapBarPadding
        );

        image.Mutate(x => x.Fill(new Color(new Rgba32(55,55,55))));
        
        for (var i = 0; i < heaps.Length; i++)
        {
            var heap = heaps[i];
            var titleRect = TextMeasurer.MeasureSize(heap.Meta.HeapId, textOptions);

            var yOffset = heapSectionHeight * i + heapTitleSize * i + heapBarPadding;
            image.Mutate(x =>
            {
                x.DrawText(GetHeapLabel(heap), font, Color.White, new PointF(heapBarPadding, yOffset));
                x.Fill(new Color(new Rgba32(10, 10, 10)), new RectangleF(new PointF(heapBarPadding, yOffset + titleRect.Height + 5), new SizeF(heapBarWidth, heapBarHeight)));

                var occupiedRegions = heap.GetOccupiedRegions().OrderBy(r => r.Offset);
                foreach (var region in occupiedRegions)
                {
                    var xStart = (float)region.Offset / heap.Meta.Size * heapBarWidth;
                    var xEnd = (float)region.End / heap.Meta.Size * heapBarWidth;

                    var regionRect = new RectangleF(xStart + heapBarPadding, yOffset + titleRect.Height + 5,
                        xEnd - xStart, heapBarHeight);
                    
                    x.Fill(Color.ParseHex("#27967c"), regionRect);
                    x.Draw(Color.ParseHex("#45ffb5"), 2, regionRect);
                }
            });
        }
        
        image.SaveAsPng(path);
    }

    private static string GetHeapLabel(IVkMemoryHeap heap)
    {
        return $"{heap.Meta.HeapId} | {GetByteSize(heap.Meta.Size)} | {(float)heap.GetOccupiedMemory() / heap.Meta.Size * 100f:0.00}%";
    }

    private static string GetByteSize(ulong size)
    {
        const ulong kb = 1024;
        const ulong mb = kb * 1024;
        const ulong gb = mb * 1024;

        return size switch
        {
            >= gb => $"{size / (double)gb:F2} GB",
            >= mb => $"{size / (double)mb:F2} MB",
            >= kb => $"{size / (double)kb:F2} KB",
            _ => $"{size} bytes"
        };
    }
}