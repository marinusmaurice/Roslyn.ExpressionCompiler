// Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    /// <summary>
    /// The source method but with its own parameter symbols. Everything else is forwarded to the original method
    /// (<see cref="WrappedMethodSymbol"/>) so new <see cref="MethodSymbol"/> members don't need to be added here.
    /// </summary>
    sealed class RenamedParametersMethodSymbol : WrappedMethodSymbol
    {
        private readonly MethodSymbol _originalMethod;
        private readonly ParameterSymbol? _thisParameter;
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        public RenamedParametersMethodSymbol(MethodSymbol originalMethod, MethodDebugInfo<TypeSymbol, LocalSymbol> methodDebugInfo)
        {
            _originalMethod = originalMethod;
            var builder = ArrayBuilder<ParameterSymbol>.GetInstance();

            var thisParameter = originalMethod.ThisParameter;
            var hasThisParameter = thisParameter is not null;
            if (hasThisParameter)
            {
                _thisParameter = MakeParameterSymbol(-1, GeneratedNames.ThisProxyFieldName(), thisParameter!);
                Debug.Assert(TypeSymbol.Equals(_thisParameter.Type, originalMethod.ContainingType, TypeCompareKind.ConsiderEverything));
            }

            foreach (var p in originalMethod.Parameters)
            {
                var ordinal = p.Ordinal;
                Debug.Assert(ordinal == builder.Count);
                // NOTE: dnSpy 6.1.8 also computed methodDebugInfo.GetParameterName() here but kept the metadata name.
                // That behavior is unchanged.
                var parameter = MakeParameterSymbol(ordinal, p.Name, p);
                builder.Add(parameter);
            }

            _parameters = builder.ToImmutableAndFree();
        }

        private ParameterSymbol MakeParameterSymbol(int ordinal, string name, ParameterSymbol sourceParameter) =>
            SynthesizedParameterSymbol.Create(this, sourceParameter.TypeWithAnnotations, ordinal, sourceParameter.RefKind, name, sourceParameter.EffectiveScope, refCustomModifiers: sourceParameter.RefCustomModifiers);

        public override MethodSymbol UnderlyingMethod => _originalMethod;
        public override ImmutableArray<ParameterSymbol> Parameters => _parameters;
        public override Symbol ContainingSymbol => _originalMethod.ContainingSymbol;
        public override ImmutableArray<TypeParameterSymbol> TypeParameters => _originalMethod.TypeParameters;
        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => _originalMethod.TypeArgumentsWithAnnotations;
        public override TypeWithAnnotations ReturnTypeWithAnnotations => _originalMethod.ReturnTypeWithAnnotations;
        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => _originalMethod.ExplicitInterfaceImplementations;
        public override ImmutableArray<CustomModifier> RefCustomModifiers => _originalMethod.RefCustomModifiers;
        public override Symbol? AssociatedSymbol => _originalMethod.AssociatedSymbol;
        public override bool IsAsync => _originalMethod.IsAsync;
        internal override bool HasSpecialNameAttribute => _originalMethod.HasSpecialNameAttribute;
        internal override ThreeState RuntimeAsyncMethodGenerationAttributeSetting => _originalMethod.RuntimeAsyncMethodGenerationAttributeSetting;
        internal override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => _originalMethod.GetUnmanagedCallersOnlyAttributeData(forceComplete);
        internal override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument) => _originalMethod.HasAsyncMethodBuilderAttribute(out builderArgument);
        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => _originalMethod.CalculateLocalSyntaxOffset(localPosition, localTree);
        internal override bool IsNullableAnalysisEnabled() => _originalMethod.IsNullableAnalysisEnabled();
        internal override int TryGetOverloadResolutionPriority() => _originalMethod.TryGetOverloadResolutionPriority();
        internal override CallerUnsafeMode GetCallerUnsafeMode(ConsList<FieldSymbol> fieldsBeingBound) => _originalMethod.GetCallerUnsafeMode(fieldsBeingBound);

        internal override bool TryGetThisParameter(out ParameterSymbol? thisParameter)
        {
            thisParameter = _thisParameter;
            return true;
        }

        // Only used to bind expressions, never emitted
        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
        }
    }
}
