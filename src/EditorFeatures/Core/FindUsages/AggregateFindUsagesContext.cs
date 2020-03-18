﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    /// <summary>
    /// Helper class that allows us to provide a unified Find-Usages progress over multiple
    /// subroutines computing different types of usages.
    /// </summary>
    internal class FindUsagesContextAggregator
    {
        /// <summary>
        /// Real context object we'll forward messages to.
        /// </summary>
        private readonly IFindUsagesContext _context;

        /// <summary>
        /// Underlying context objects that we'll receive messages from and forward to <see cref="_context"/>
        /// </summary>
        private readonly List<ForwardingFindUsagesContext> _underlyingContexts = new List<ForwardingFindUsagesContext>();

        private readonly object _gate = new object();
        private readonly Dictionary<ForwardingFindUsagesContext, (int current, int maxium)> _underlyingContextToProgress =
            new Dictionary<ForwardingFindUsagesContext, (int current, int maxium)>();

        public FindUsagesContextAggregator(IFindUsagesContext context)
        {
            _context = context;
        }

        public IFindUsagesContext CreateForwardingContext()
        {
            var result = new ForwardingFindUsagesContext(this);
            lock (_gate)
            {
                _underlyingContexts.Add(result);
            }
            return result;
        }

        private Task ReportProgressAsync(ForwardingFindUsagesContext context, int current, int maximum)
        {
            int combinedCurrent = 0, combinedMaximum = 0;
            lock (_gate)
            {
                _underlyingContextToProgress[context] = (current, maximum);

                foreach (var (_, (singleCurrent, singleMaximum)) in _underlyingContextToProgress)
                {
                    combinedCurrent += singleCurrent;
                    combinedMaximum += singleMaximum;
                }
            }

            return _context.ReportProgressAsync(combinedCurrent, combinedMaximum);
        }

        #region Simple forwarding calls

        private CancellationToken CancellationToken
            => _context.CancellationToken;

        private Task OnDefinitionFoundAsync(DefinitionItem definition)
            => _context.OnDefinitionFoundAsync(definition);

        private Task OnReferenceFoundAsync(SourceReferenceItem reference)
            => _context.OnReferenceFoundAsync(reference);

        private Task OnExternalReferenceFoundAsync(ExternalReferenceItem reference)
            => _context.OnExternalReferenceFoundAsync(reference);

        private Task ReportMessageAsync(string message)
            => _context.ReportMessageAsync(message);

        private Task SetSearchTitleAsync(string title)
            => _context.ReportMessageAsync(title);

        #endregion

        private class ForwardingFindUsagesContext : IFindUsagesContext
        {
            private readonly FindUsagesContextAggregator _aggregator;

            public ForwardingFindUsagesContext(FindUsagesContextAggregator aggregateFindUsagesContext)
            {
                _aggregator = aggregateFindUsagesContext;
            }

            // We pass ourselves along to the aggregator so it can keep track of the current/max
            // *per* underlying context.
            public Task ReportProgressAsync(int current, int maximum)
                => _aggregator.ReportProgressAsync(this, current, maximum);

            #region Simple forwarding calls

            public CancellationToken CancellationToken
                => _aggregator.CancellationToken;

            public Task OnDefinitionFoundAsync(DefinitionItem definition)
                => _aggregator.OnDefinitionFoundAsync(definition);

            public Task OnReferenceFoundAsync(SourceReferenceItem reference)
                => _aggregator.OnReferenceFoundAsync(reference);

            public Task OnExternalReferenceFoundAsync(ExternalReferenceItem reference)
                => _aggregator.OnExternalReferenceFoundAsync(reference);

            public Task ReportMessageAsync(string message)
                => _aggregator.ReportMessageAsync(message);

            public Task SetSearchTitleAsync(string title)
                => _aggregator.ReportMessageAsync(title);

            #endregion
        }
    }
}
