using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Mesh Deformation", "Linear Blend Skinning Ex")]
    class LinearBlendSkinningNodeEx : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireVertexSkinning, IMayRequirePosition, IMayRequireNormal, IMayRequireTangent, IMayRequireVertexID
    {
        public const int kPositionSlotId = 0;
        public const int kNormalSlotId = 1;
        public const int kTangentSlotId = 2;
        public const int kPositionOutputSlotId = 3;
        public const int kNormalOutputSlotId = 4;
        public const int kTangentOutputSlotId = 5;

        public const string kSlotPositionName = "Vertex Position";
        public const string kSlotNormalName = "Vertex Normal";
        public const string kSlotTangentName = "Vertex Tangent";
        public const string kOutputSlotPositionName = "Skinned Position";
        public const string kOutputSlotNormalName = "Skinned Normal";
        public const string kOutputSlotTangentName = "Skinned Tangent";

        private bool __gpuSkinning;

        public LinearBlendSkinningNodeEx()
        {
            __gpuSkinning = PlayerSettings.gpuSkinning;

            name = "Linear Blend Skinning Ex";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new PositionMaterialSlot(kPositionSlotId, kSlotPositionName, kSlotPositionName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new NormalMaterialSlot(kNormalSlotId, kSlotNormalName, kSlotNormalName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new TangentMaterialSlot(kTangentSlotId, kSlotTangentName, kSlotTangentName, CoordinateSpace.Object, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kPositionOutputSlotId, kOutputSlotPositionName, kOutputSlotPositionName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kNormalOutputSlotId, kOutputSlotNormalName, kOutputSlotNormalName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kTangentOutputSlotId, kOutputSlotTangentName, kOutputSlotTangentName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            RemoveSlotsNameNotMatching(new[] { kPositionSlotId, kNormalSlotId, kTangentSlotId, kPositionOutputSlotId, kNormalOutputSlotId, kTangentOutputSlotId });
        }

        protected override void CalculateNodeHasError()
        {
#if !(HYBRID_RENDERER_0_6_0_OR_NEWER || ENTITIES_GRAPHICS_0_60_0_OR_NEWER)
            owner.AddSetupError(objectId, "Could not find a supported version (0.60.0 or newer) of the com.unity.entities.graphics package installed in the project.");
            hasError = true;
#endif
        }

        public bool RequiresVertexID(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return __gpuSkinning;
        }

        public bool RequiresVertexSkinning(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            return !__gpuSkinning;
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            if (stageCapability == ShaderStageCapability.Vertex || stageCapability == ShaderStageCapability.All)
                return NeededCoordinateSpace.Object;
            else
                return NeededCoordinateSpace.None;
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            if (stageCapability == ShaderStageCapability.Vertex || stageCapability == ShaderStageCapability.All)
                return NeededCoordinateSpace.Object;
            else
                return NeededCoordinateSpace.None;
        }

        public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            if (stageCapability == ShaderStageCapability.Vertex || stageCapability == ShaderStageCapability.All)
                return NeededCoordinateSpace.Object;
            else
                return NeededCoordinateSpace.None;
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (__gpuSkinning)
            {
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
                properties.AddShaderProperty(new Vector4ShaderProperty()
                {
                    displayName = "Compute Mesh Buffer Index Offset",
                    overrideReferenceName = "_DotsDeformationParams",
                    overrideHLSLDeclaration = true,
                    hlslDeclarationOverride = HLSLDeclaration.HybridPerInstance,
                    hidden = true,
                    value = new Vector4(0, 0, 0, 0)
                });
#else
                properties.AddShaderProperty(new Vector1ShaderProperty()
                {
                    displayName = "Compute Mesh Buffer Index Offset",
                    overrideReferenceName = "_ComputeMeshIndex",
                    overrideHLSLDeclaration = true,
                    hlslDeclarationOverride = HLSLDeclaration.HybridPerInstance,
                    hidden = true,
                    value = 0
                });
#endif
            }
            else
            {
                properties.AddShaderProperty(new Vector1ShaderProperty()
                {
                    displayName = "Skin Matrix Index Offset",
                    overrideReferenceName = "_SkinMatrixIndex",
                    overrideHLSLDeclaration = true,
                    hlslDeclarationOverride = HLSLDeclaration.HybridPerInstance,
                    hidden = true,
                    value = 0
                });
            }

            base.CollectShaderProperties(properties, generationMode);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            if (__gpuSkinning)
            {
                sb.AppendLine("#if defined(UNITY_DOTS_INSTANCING_ENABLED)");
                sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kPositionOutputSlotId));
                sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kNormalOutputSlotId));
                sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kTangentOutputSlotId));
                if (generationMode == GenerationMode.ForReals)
                {
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
                    sb.AppendLine("ApplyDeformedVertexData(" +
#else
                    sb.AppendLine($"{GetFunctionName()}(" +
#endif
                    $"IN.VertexID, " +
                        $"{GetVariableNameForSlot(kPositionOutputSlotId)}, " +
                        $"{GetVariableNameForSlot(kNormalOutputSlotId)}, " +
                        $"{GetVariableNameForSlot(kTangentOutputSlotId)});");
                }
                sb.AppendLine("#else");
                sb.AppendLine("$precision3 {0} = IN.ObjectSpacePosition;", GetVariableNameForSlot(kPositionOutputSlotId));
                sb.AppendLine("$precision3 {0} = IN.ObjectSpaceNormal;", GetVariableNameForSlot(kNormalOutputSlotId));
                sb.AppendLine("$precision3 {0} = IN.ObjectSpaceTangent;", GetVariableNameForSlot(kTangentOutputSlotId));

                sb.AppendLine("#endif");
            }
            else
            {
                sb.AppendLine("#if defined(UNITY_DOTS_INSTANCING_ENABLED)");
                sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kPositionOutputSlotId));
                sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kNormalOutputSlotId));
                sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kTangentOutputSlotId));
                if (generationMode == GenerationMode.ForReals)
                {
                    sb.AppendLine($"{GetFunctionName()}(" +
                        $"IN.BoneIndices, " +
                        $"IN.BoneWeights, " +
                        $"{GetSlotValue(kPositionSlotId, generationMode)}, " +
                        $"{GetSlotValue(kNormalSlotId, generationMode)}, " +
                        $"{GetSlotValue(kTangentSlotId, generationMode)}, " +
                        $"{GetVariableNameForSlot(kPositionOutputSlotId)}, " +
                        $"{GetVariableNameForSlot(kNormalOutputSlotId)}, " +
                        $"{GetVariableNameForSlot(kTangentOutputSlotId)});");
                }
                sb.AppendLine("#else");
                sb.AppendLine("$precision3 {0} = {1};", GetVariableNameForSlot(kPositionOutputSlotId), GetSlotValue(kPositionSlotId, generationMode));
                sb.AppendLine("$precision3 {0} = {1};", GetVariableNameForSlot(kNormalOutputSlotId), GetSlotValue(kNormalSlotId, generationMode));
                sb.AppendLine("$precision3 {0} = {1};", GetVariableNameForSlot(kTangentOutputSlotId), GetSlotValue(kTangentSlotId, generationMode));
                sb.AppendLine("#endif");
            }
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            if (__gpuSkinning)
            {
#if ENABLE_DOTS_DEFORMATION_MOTION_VECTORS
                registry.ProvideFunction("define", sb =>
                {
                    sb.AppendLine("#if defined(UNITY_DOTS_INSTANCING_ENABLED)"); // start of UNITY_DOTS_INSTANCING_ENABLED
                    sb.AppendLine("#define DOTS_DEFORMED");
                    sb.AppendLine("#include \"Packages/com.unity.entities.graphics/Unity.Entities.Graphics/Deformations/ShaderLibrary/DotsDeformation.hlsl\"");
                    sb.AppendLine("#endif");
                });
#else
                registry.ProvideFunction("DeformedVertexData", sb =>
                {
                    sb.AppendLine("struct DeformedVertexData");
                    sb.AppendLine("{");
                    using (sb.IndentScope())
                    {
                        sb.AppendLine("float3 Position;");
                        sb.AppendLine("float3 Normal;");
                        sb.AppendLine("float3 Tangent;");
                    }
                    sb.AppendLine("};");
                    sb.AppendLine("uniform StructuredBuffer<DeformedVertexData> _DeformedMeshData : register(t1);");

                });

                registry.ProvideFunction(GetFunctionName(), sb =>
                {
                    sb.AppendLine($"void {GetFunctionName()}(" +
                        "uint vertexID, " +
                        "out $precision3 positionOut, " +
                        "out $precision3 normalOut, " +
                        "out $precision3 tangentOut)");
                    sb.AppendLine("{");
                    using (sb.IndentScope())
                    {
                        sb.AppendLine("const DeformedVertexData vertexData = _DeformedMeshData[asuint(UNITY_ACCESS_HYBRID_INSTANCED_PROP(_ComputeMeshIndex, float)) + vertexID];");
                        sb.AppendLine("positionOut = vertexData.Position;");
                        sb.AppendLine("normalOut = vertexData.Normal;");
                        sb.AppendLine("tangentOut = vertexData.Tangent;");
                    }
                    sb.AppendLine("}");
                });
#endif
            }
            else
            {
                registry.ProvideFunction("SkinMatrices", sb =>
                {
                    sb.AppendLine("uniform ByteAddressBuffer _SkinMatrices;");
                });

                registry.ProvideFunction("LoadSkinMatrix", sb =>
                {
                    sb.AppendLine($"float3x4 LoadSkinMatrix(uint index)");
                    sb.AppendLine("{");
                    using (sb.IndentScope())
                    {
                        sb.AppendLine("uint offset = index * 48;");
                        sb.AppendLine("// Read in 4 columns of float3 data each.");
                        sb.AppendLine("// Done in 3 load4 and then repacking into final 3x4 matrix");
                        sb.AppendLine("// _SkinMatrices consists of float32");
                        sb.AppendLine("float4 p1 = asfloat(_SkinMatrices.Load4(offset + 0 * 16));");
                        sb.AppendLine("float4 p2 = asfloat(_SkinMatrices.Load4(offset + 1 * 16));");
                        sb.AppendLine("float4 p3 = asfloat(_SkinMatrices.Load4(offset + 2 * 16));");

                        sb.AppendLine("return float3x4(p1.x, p1.w, p2.z, p3.y,p1.y, p2.x, p2.w, p3.z,p1.z, p2.y, p3.x, p3.w);");

                    }
                    sb.AppendLine("}");
                });

                registry.ProvideFunction(GetFunctionName(), sb =>
                {
                    sb.AppendLine($"void {GetFunctionName()}(" +
                        "uint4 indices, " +
                        "$precision4 weights, " +
                        "$precision3 positionIn, " +
                        "$precision3 normalIn, " +
                        "$precision3 tangentIn, " +
                        "out $precision3 positionOut, " +
                        "out $precision3 normalOut, " +
                        "out $precision3 tangentOut)");
                    sb.AppendLine("{");
                    using (sb.IndentScope())
                    {
                        sb.AppendLine("positionOut = 0;");
                        sb.AppendLine("normalOut = 0;");
                        sb.AppendLine("tangentOut = 0;");

                        sb.AppendLine("$precision weight, totalWeight = 0;");
                        sb.AppendLine("for (int i = 0; i < 3; ++i)");
                        sb.AppendLine("{");
                        using (sb.IndentScope())
                        {
                            sb.AppendLine("uint skinMatrixIndex = indices[i] + asint(UNITY_ACCESS_HYBRID_INSTANCED_PROP(_SkinMatrixIndex,float));");
                            sb.AppendLine("$precision3x4 skinMatrix = LoadSkinMatrix(skinMatrixIndex);");
                            sb.AppendLine("$precision3 vtransformed = mul(skinMatrix, $precision4(positionIn, 1));");
                            sb.AppendLine("$precision3 ntransformed = mul(skinMatrix, $precision4(normalIn, 0));");
                            sb.AppendLine("$precision3 ttransformed = mul(skinMatrix, $precision4(tangentIn, 0));");
                            sb.AppendLine("");
                            sb.AppendLine("weight = weights[i];");
                            sb.AppendLine("positionOut += vtransformed * weight;");
                            sb.AppendLine("normalOut   += ntransformed * weight;");
                            sb.AppendLine("tangentOut  += ttransformed * weight;");
                            sb.AppendLine("totalWeight += weight;");
                        }
                        sb.AppendLine("}");
                        sb.AppendLine("uint skinMatrixIndex = indices[3] + asint(UNITY_ACCESS_HYBRID_INSTANCED_PROP(_SkinMatrixIndex,float));");
                        sb.AppendLine("$precision3x4 skinMatrix = LoadSkinMatrix(skinMatrixIndex);");
                        sb.AppendLine("$precision3 vtransformed = mul(skinMatrix, $precision4(positionIn, 1));");
                        sb.AppendLine("$precision3 ntransformed = mul(skinMatrix, $precision4(normalIn, 0));");
                        sb.AppendLine("$precision3 ttransformed = mul(skinMatrix, $precision4(tangentIn, 0));");
                        sb.AppendLine("");
                        sb.AppendLine("weight = 1.0 - totalWeight;");
                        sb.AppendLine("positionOut += vtransformed * weight;");
                        sb.AppendLine("normalOut   += ntransformed * weight;");
                        sb.AppendLine("tangentOut  += ttransformed * weight;");
                    }
                    sb.AppendLine("}");
                });
            }
        }

        string GetFunctionName()
        {
            return __gpuSkinning ? "ComputeDeformedVertex_$precision" : "LinearBlendSkinning_$precision";
        }
    }
}
