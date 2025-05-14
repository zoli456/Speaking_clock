using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.WIC;

namespace Speaking_clock.Widgets;

public static class GraphicsFactories
{
    internal static readonly ID2D1Factory1 D2DFactory;
    internal static readonly IDWriteFactory DWriteFactory;
    internal static readonly IWICImagingFactory WicFactory;

    static GraphicsFactories()
    {
        D2DFactory = D2D1.D2D1CreateFactory<ID2D1Factory1>();
        DWriteFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();
        WicFactory = new IWICImagingFactory();
    }
}