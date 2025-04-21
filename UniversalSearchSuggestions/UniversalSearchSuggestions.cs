// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CommandPalette.Extensions;

namespace UniversalSearchSuggestions;

[Guid("803d0b05-d97d-4896-8fc2-3e5a0144eaa7")]
public sealed partial class UniversalSearchSuggestions : IExtension, IDisposable
{
    private readonly ManualResetEvent _disposeEvent;
    private readonly UniversalSearchSuggestionsCommandsProvider _provider = new();

    public UniversalSearchSuggestions(ManualResetEvent disposeEvent)
    {
        _disposeEvent = disposeEvent;
    }

    public object? GetProvider(ProviderType type) =>
        type switch
        {
            ProviderType.Commands => _provider,
            _ => null
        };

    public void Dispose() => _disposeEvent.Set();
}
