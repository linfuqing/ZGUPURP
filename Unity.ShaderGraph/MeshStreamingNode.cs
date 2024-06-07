using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Mesh Streaming", "Compute Mesh Streaming")]
    class MeshStreamingNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequirePosition, IMayRequireNormal, /*IMayRequireTangent, */IMayRequireVertexID
    {
        public const int kPositionOutputSlotId = 0;
        public const int kNormalOutputSlotId = 1;
        //public const int kTangentOutputSlotId = 2;

        public const string kOutputSlotPositionName = "Streaming Position";
        public const string kOutputSlotNormalName = "Streaming Normal";
        //public const string kOutputSlotTangentName = "Streaming Tangent";

        public MeshStreamingNode()
        {
            name = "Compute Mesh Streaming";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector3MaterialSlot(kPositionOutputSlotId, kOutputSlotPositionName, kOutputSlotPositionName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            AddSlot(new Vector3MaterialSlot(kNormalOutputSlotId, kOutputSlotNormalName, kOutputSlotNormalName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            //AddSlot(new Vector3MaterialSlot(kTangentOutputSlotId, kOutputSlotTangentName, kOutputSlotTangentName, SlotType.Output, Vector3.zero, ShaderStageCapability.Vertex));
            RemoveSlotsNameNotMatching(new[] { kPositionOutputSlotId, kNormalOutputSlotId/*, kTangentOutputSlotId*/ });
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
            return true;
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

        /*public NeededCoordinateSpace RequiresTangent(ShaderStageCapability stageCapability = ShaderStageCapability.All)
        {
            if (stageCapability == ShaderStageCapability.Vertex || stageCapability == ShaderStageCapability.All)
                return NeededCoordinateSpace.Object;
            else
                return NeededCoordinateSpace.None;
        }*/

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new Vector1ShaderProperty()
            {
                displayName = "Mesh Streaming Vertex Offset",
                overrideReferenceName = "_MeshStreamingVertexOffset",
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.HybridPerInstance,
                hidden = true,
                value = default
            });

            base.CollectShaderProperties(properties, generationMode);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine("#if defined(UNITY_DOTS_INSTANCING_ENABLED)");
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kPositionOutputSlotId));
            sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kNormalOutputSlotId));
            //sb.AppendLine("$precision3 {0} = 0;", GetVariableNameForSlot(kTangentOutputSlotId));
            if (generationMode == GenerationMode.ForReals)
            {
                sb.AppendLine($"{GetFunctionName()}(" +
                           $"IN.VertexID, " +
                           $"{GetVariableNameForSlot(kPositionOutputSlotId)}, " +
                           $"{GetVariableNameForSlot(kNormalOutputSlotId)});"/*, " +
                           $"{GetVariableNameForSlot(kTangentOutputSlotId)});"*/);
            }
            sb.AppendLine("#else");
            sb.AppendLine("$precision3 {0} = IN.ObjectSpacePosition;", GetVariableNameForSlot(kPositionOutputSlotId));
            sb.AppendLine("$precision3 {0} = IN.ObjectSpaceNormal;", GetVariableNameForSlot(kNormalOutputSlotId));
            //sb.AppendLine("$precision3 {0} = IN.ObjectSpaceTangent;", GetVariableNameForSlot(kTangentOutputSlotId));
            sb.AppendLine("#endif");
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            registry.ProvideFunction("MeshStreamingVertex", sb =>
            {
                sb.AppendLine("struct MeshStreamingVertex");
                sb.AppendLine("{");
                using (sb.IndentScope())
                {
                    sb.AppendLine("float4 position;");
                    sb.AppendLine("float4 normal;");
                    //sb.AppendLine("float4 tangent;");
                }
                sb.AppendLine("};");
                sb.AppendLine("uniform StructuredBuffer<MeshStreamingVertex> _MeshStreamingVertexData;"/* : register(t1)*/);
            });

            registry.ProvideFunction(GetFunctionName(), sb =>
            {
                sb.AppendLine($"void {GetFunctionName()}(" +
                            "uint vertexID, " +
                            "out $precision3 positionOut, " +
                            "out $precision3 normalOut)"/*, " +
                            "out $precision3 tangentOut)"*/);

                sb.AppendLine("{");
                using (sb.IndentScope())
                {
                    sb.AppendLine("const uint vertexIndex = asuint(UNITY_ACCESS_HYBRID_INSTANCED_PROP(_MeshStreamingVertexOffset, float));");
                    //sb.AppendLine("const uint4 vertexIndices = asuint(UNITY_ACCESS_HYBRID_INSTANCED_PROP(_MeshStreamingVertexOffset, float4));");
                    //sb.AppendLine("const uint vertexIndex = (vertexIndices.x << 0) | (vertexIndices.y << 8) | (vertexIndices.z << 16) | (vertexIndices.w << 24);");
                    sb.AppendLine("const MeshStreamingVertex vertex = _MeshStreamingVertexData[vertexIndex + vertexID];");
                    sb.AppendLine("$precision3 cameraPositionOS = TransformWorldToObject(_WorldSpaceCameraPos.xyz);");
                    sb.AppendLine("positionOut = lerp(cameraPositionOS, vertex.position.xyz, vertex.position.w);");
                    //sb.AppendLine("positionOut = vertex.position.xyz;");
                    sb.AppendLine("normalOut = vertex.normal.xyz;");
                    //sb.AppendLine("tangentOut = vertex.tangent.xyz;");
                }
                sb.AppendLine("}");
            });
        }

        string GetFunctionName()
        {
            return "Unity_ComputeMeshStreamingVertex_$precision";
        }
    }
}
