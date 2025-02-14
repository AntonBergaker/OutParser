using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace OutParser.Generator;
public static class ValueProviderExtensions {
    public static IncrementalValuesProvider<T> NotNull<T>(this IncrementalValuesProvider<T?> provider) {
        return provider.Where(x => x is not null)!;
    }
}
