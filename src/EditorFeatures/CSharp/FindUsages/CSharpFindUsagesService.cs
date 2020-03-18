﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using VS.IntelliNav.Contracts;

namespace Microsoft.CodeAnalysis.Editor.CSharp.FindUsages
{
    [ExportLanguageService(typeof(IFindUsagesService), LanguageNames.CSharp), Shared]
    internal class CSharpFindUsagesService : AbstractFindUsagesService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpFindUsagesService(
            IThreadingContext threadingContext,
            [Import(AllowDefault = true)] ICodeIndexProvider? codeIndexProvider)
            : base(threadingContext, codeIndexProvider)
        {
        }
    }
}
