using System.Windows.Input;
#if ANDROID || IOS
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
#endif

namespace Mobile_apps_L1_Model_view.Views;

// The camera picture and barcode reader (ZXing.Net.Maui). It exists only on Android and iOS: the library is
// referenced for those targets alone, and elsewhere this view stays empty (the page offers manual entry).
// Bind IsActive to switch the camera on and off, and DetectedCommand to receive the barcode text.
public sealed class CameraScannerView : ContentView
{
    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
        nameof(IsActive), typeof(bool), typeof(CameraScannerView), false,
        propertyChanged: (view, _, active) => ((CameraScannerView)view).OnActiveChanged((bool)active));

    public static readonly BindableProperty DetectedCommandProperty = BindableProperty.Create(
        nameof(DetectedCommand), typeof(ICommand), typeof(CameraScannerView));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public ICommand? DetectedCommand
    {
        get => (ICommand?)GetValue(DetectedCommandProperty);
        set => SetValue(DetectedCommandProperty, value);
    }

#if ANDROID || IOS
    private CameraBarcodeReaderView? _reader;

    // A reader is created when the camera is switched on and dropped when it is switched off:
    // removing it from the page disconnects its handler, which releases the camera
    private void OnActiveChanged(bool active)
    {
        if (_reader is not null)
        {
            _reader.BarcodesDetected -= OnBarcodesDetected;
            _reader = null;
            Content = null;
        }

        if (!active)
        {
            return;
        }

        _reader = new CameraBarcodeReaderView
        {
            Options = new BarcodeReaderOptions { Formats = BarcodeFormats.OneDimensional, AutoRotate = true, Multiple = false },
            IsDetecting = true
        };
        _reader.BarcodesDetected += OnBarcodesDetected;
        Content = _reader;
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var text = e.Results.FirstOrDefault()?.Value;
        if (!string.IsNullOrWhiteSpace(text))
        {
            // The library reports on a camera thread
            MainThread.BeginInvokeOnMainThread(() => DetectedCommand?.Execute(text));
        }
    }
#else
    private void OnActiveChanged(bool active)
    {
    }
#endif
}
