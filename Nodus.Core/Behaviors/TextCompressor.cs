using System;
using System.Diagnostics;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ReactiveUI;

namespace Nodus.Core.Behaviors;

public class TextCompressor : AvaloniaObject
{
    public static readonly AttachedProperty<int> MaxCharacterLengthProperty;
    public static readonly AttachedProperty<IDisposable?> CompressorChangeContractProperty;

    private const string Postfix = "...";
    
    static TextCompressor()
    {
        MaxCharacterLengthProperty =
            AvaloniaProperty.RegisterAttached<TextCompressor, TextBlock, int>("MaxCharacterLength");
        CompressorChangeContractProperty =
            AvaloniaProperty.RegisterAttached<TextCompressor, TextBlock, IDisposable?>("CompressorChangeContract");
    }

    public static void SetMaxCharacterLength(AvaloniaObject element, int length)
    {
        if (element is not TextBlock text) return;
        
        text.SetValue(MaxCharacterLengthProperty, length);
        text.GetValue(CompressorChangeContractProperty)?.Dispose();
        TryShortenText(text);

        text.SetValue(CompressorChangeContractProperty, text.WhenAnyValue(x => x.Text)
            .Subscribe(Observer.Create<string?>(_ =>
            {
                if (text.Text == null 
                    || !text.IsAttachedToVisualTree()
                    || text.Text.Length <= length) return;
                
                TryShortenText(text);
            })));

        void OnElementDetached(object? _, VisualTreeAttachmentEventArgs __)
        {
            text.DetachedFromVisualTree -= OnElementDetached;
            text.GetValue(CompressorChangeContractProperty)?.Dispose();
        }

        text.DetachedFromVisualTree += OnElementDetached;
    }

    private static void TryShortenText(TextBlock text)
    {
        if (text.Text == null) return;

        var maxLength = text.GetValue(MaxCharacterLengthProperty);
        
        Trace.WriteLine($"--------- Shorten {text.Text} to {maxLength} chars");
        
        text.Text = text.Text[..maxLength] + Postfix;
    }

    public static int GetMaxCharacterLength(AvaloniaObject element)
    {
        return element.GetValue(MaxCharacterLengthProperty);
    }
}