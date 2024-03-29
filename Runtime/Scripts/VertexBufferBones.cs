﻿// Copyright 2020-2022 Andreas Atteneder
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.IO;
using GLTFast.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace GLTFast {

    using Logging;
    using Schema;
    using Vertex;

    abstract class VertexBufferBonesBase {
        
        protected ICodeLogger logger;

        public VertexBufferBonesBase(ICodeLogger logger) {
            this.logger = logger;
        }

        public abstract JobHandle? ScheduleVertexBonesJob(
            IGltfBuffers buffers,
            int weightsAccessorIndex,
            int jointsAccessorIndex
        );
        public abstract void AddDescriptors(VertexAttributeDescriptor[] dst, int offset, int stream);
        public abstract void ApplyOnMesh(UnityEngine.Mesh msh, int stream, MeshUpdateFlags flags = PrimitiveCreateContextBase.defaultMeshUpdateFlags);
        public abstract void Dispose();
    }

    class VertexBufferBones : VertexBufferBonesBase {
        NativeArray<VBones> vData;

        public VertexBufferBones(ICodeLogger logger) : base(logger) {}
        
        public override unsafe JobHandle? ScheduleVertexBonesJob(
            IGltfBuffers buffers,
            int weightsAccessorIndex,
            int jointsAccessorIndex
        )
        {
            Profiler.BeginSample("ScheduleVertexBonesJob");
            Profiler.BeginSample("AllocateNativeArray");

            buffers.GetAccessor(jointsAccessorIndex, out var jointsAcc, out var jointsData, out var jointsByteStride);
            if (jointsAcc.isSparse) {
                logger.Error(LogCode.SparseAccessor, "bone joints");
            }
            vData = new NativeArray<VBones>(jointsAcc.count, VertexBufferConfigBase.defaultAllocator);
            var vDataPtr = (byte*) NativeArrayUnsafeUtility.GetUnsafePtr(vData);
            Profiler.EndSample();

            JobHandle jobHandle;

            {
                var h = GetJointsJob(
                    jointsData,
                    Accessor.GetAccessorAttributeTypeLength(jointsAcc.typeEnum),
                    jointsAcc.count,
                    jointsAcc.componentType,
                    jointsByteStride,
                    (uint4*)(vDataPtr + 16),
                    32,
                    logger
                );
                if (h.HasValue)
                {
                    jobHandle = h.Value;
                }
                else
                {
                    Profiler.EndSample();
                    return null;
                }
            }

            if (weightsAccessorIndex >= 0)
            {
                buffers.GetAccessor(weightsAccessorIndex, out var weightsAcc, out var weightsData, out var weightsByteStride);
                if (weightsAcc.isSparse)
                {
                    logger.Error(LogCode.SparseAccessor, "bone weights");
                }
                var h = GetWeightsJob(
                    weightsData,
                    Accessor.GetAccessorAttributeTypeLength(weightsAcc.typeEnum),
                    weightsAcc.count,
                    weightsAcc.componentType,
                    weightsByteStride,
                    (float4*)vDataPtr,
                    32,
                    weightsAcc.normalized
                );
                if (h.HasValue) {
                    jobHandle = JobHandle.CombineDependencies(h.Value, jobHandle);
                } else {
                    Profiler.EndSample();
                    return null;
                }
            } else
            {
                var h = GetDefaultWeightsJob(
                    Accessor.GetAccessorAttributeTypeLength(jointsAcc.typeEnum),
                    jointsAcc.count,
                    (float4*)vDataPtr, 32
                    );
                jobHandle = JobHandle.CombineDependencies(h.Value, jobHandle);
            }

            var skinWeights = (int)QualitySettings.skinWeights;

#if UNITY_EDITOR
            // If this is design-time import, fix and import all weights.
            if(!UnityEditor.EditorApplication.isPlaying || skinWeights < 4) {
                if (!UnityEditor.EditorApplication.isPlaying) {
                    skinWeights = 4;
                }
#else
            if(skinWeights < 4) { 
#endif
                var job = new SortAndRenormalizeBoneWeightsJob {
                    bones = vData,
                    skinWeights = math.max(1,skinWeights)
                };
                jobHandle = job.Schedule(vData.Length, GltfImport.DefaultBatchCount, jobHandle); 
            }
#if GLTFAST_SAFE
            else {
                // Re-normalizing alone is sufficient
                var job = new RenormalizeBoneWeightsJob {
                    bones = vData,
                };
                jobHandle = job.Schedule(vData.Length, GltfImport.DefaultBatchCount, jobHandle);
            }
#endif

            Profiler.EndSample();
            return jobHandle;
        }

        public override void AddDescriptors(VertexAttributeDescriptor[] dst, int offset, int stream) {
            dst[offset] = new VertexAttributeDescriptor(VertexAttribute.BlendWeight, VertexAttributeFormat.Float32, 4, stream);
            dst[offset+1] = new VertexAttributeDescriptor(VertexAttribute.BlendIndices, VertexAttributeFormat.UInt32, 4, stream);
        }

        public override unsafe void ApplyOnMesh(UnityEngine.Mesh msh, int stream, MeshUpdateFlags flags = PrimitiveCreateContextBase.defaultMeshUpdateFlags) {
            Profiler.BeginSample("ApplyBones");
            msh.SetVertexBufferData(vData,0,0,vData.Length,stream,flags);
            Profiler.EndSample();
        }

        public override void Dispose() {
            if (vData.IsCreated) {
                vData.Dispose();
            }
        }

        protected unsafe JobHandle? GetWeightsJob(
            void* input,
            int componentCount,
            int count,
            GLTFComponentType inputType,
            int inputByteStride,
            float4* output,
            int outputByteStride,
            bool normalized = false
            )
        {
            Profiler.BeginSample("GetWeightsJob");
            JobHandle? jobHandle;
            switch(inputType) {
                case GLTFComponentType.Float:
                    var jobTangentI = new Jobs.ConvertBoneWeightsFloatToFloatInterleavedJob();
                    jobTangentI.inputByteStride = inputByteStride>0 ? inputByteStride : componentCount * 4;
                    jobTangentI.input = (byte*)input;
                    jobTangentI.outputByteStride = outputByteStride;
                    jobTangentI.result = output;
                    jobTangentI.componentCount = componentCount;
#if UNITY_JOBS
                    jobHandle = jobTangentI.ScheduleBatch(count,GltfImport.DefaultBatchCount);
#else
                    jobHandle = jobTangentI.Schedule(count,GltfImport.DefaultBatchCount);
#endif
                    break;
                case GLTFComponentType.UnsignedShort: {
                    var job = new Jobs.ConvertBoneWeightsUInt16ToFloatInterleavedJob {
                        inputByteStride = inputByteStride>0 ? inputByteStride : componentCount *2 ,
                        input = (byte*)input,
                        outputByteStride = outputByteStride,
                        result = output
                    };
                    job.componentCount = componentCount;
#if UNITY_JOBS
                    jobHandle = job.ScheduleBatch(count,GltfImport.DefaultBatchCount);
#else
                    jobHandle = job.Schedule(count,GltfImport.DefaultBatchCount);
#endif
                    break;
                }
                case GLTFComponentType.UnsignedByte: {
                    var job = new Jobs.ConvertBoneWeightsUInt8ToFloatInterleavedJob {
                        inputByteStride = inputByteStride>0 ? inputByteStride : componentCount,
                        input = (byte*)input,
                        outputByteStride = outputByteStride,
                        result = output
                    };
                    job.componentCount = componentCount;
#if UNITY_JOBS
                    jobHandle = job.ScheduleBatch(count,GltfImport.DefaultBatchCount);
#else
                    jobHandle = job.Schedule(count,GltfImport.DefaultBatchCount);
#endif
                    break;
                }
                default:
                    logger?.Error(LogCode.TypeUnsupported,"Weights",inputType.ToString());
                    jobHandle = null;
                    break;
            }

            Profiler.EndSample();
            return jobHandle;
        }


        protected unsafe JobHandle? GetDefaultWeightsJob(
            int componentCount,
            int count,
            float4* output,
            int outputByteStride
            )
        {
            Profiler.BeginSample("GetWeightsJob");
            JobHandle? jobHandle;
            var jobTangentI = new Jobs.InitDefaultBoneWeightsInterleavedJob();
            jobTangentI.outputByteStride = outputByteStride;
            jobTangentI.componentCount = componentCount;
            jobTangentI.result = output;
#if UNITY_JOBS
            jobHandle = jobTangentI.ScheduleBatch(count,GltfImport.DefaultBatchCount);
#else
            jobHandle = jobTangentI.Schedule(count, GltfImport.DefaultBatchCount);
#endif
            Profiler.EndSample();
            return jobHandle;
        }

        static unsafe JobHandle? GetJointsJob(
            void* input,
            int componentCount,
            int count,
            GLTFComponentType inputType,
            int inputByteStride,
            uint4* output,
            int outputByteStride,
            ICodeLogger logger
        )
        {
            Profiler.BeginSample("GetJointsJob");
            JobHandle? jobHandle;
            switch(inputType) {
                case GLTFComponentType.UnsignedByte:
                    var jointsUInt8Job = new Jobs.ConvertBoneJointsUInt8ToUInt32Job();
                    jointsUInt8Job.inputByteStride = inputByteStride>0 ? inputByteStride : componentCount;
                    jointsUInt8Job.input = (byte*)input;
                    jointsUInt8Job.outputByteStride = outputByteStride;
                    jointsUInt8Job.result = output;
                    jointsUInt8Job.componentCount = componentCount;
                    jobHandle = jointsUInt8Job.Schedule(count,GltfImport.DefaultBatchCount);
                    break;
                case GLTFComponentType.UnsignedShort:
                    var jointsUInt16Job = new Jobs.ConvertBoneJointsUInt16ToUInt32Job();
                    jointsUInt16Job.inputByteStride = inputByteStride>0 ? inputByteStride : componentCount * 2;
                    jointsUInt16Job.input = (byte*)input;
                    jointsUInt16Job.outputByteStride = outputByteStride;
                    jointsUInt16Job.result = output;
                    jointsUInt16Job.componentCount = componentCount;
                    jobHandle = jointsUInt16Job.Schedule(count,GltfImport.DefaultBatchCount);
                    break;
                case GLTFComponentType.UnsignedInt:
                    var jointsUInt32Job = new Jobs.ConvertBoneJointsUInt32ToUInt32Job();
                    jointsUInt32Job.inputByteStride = inputByteStride > 0 ? inputByteStride : componentCount * 4;
                    jointsUInt32Job.input = (byte*)input;
                    jointsUInt32Job.outputByteStride = outputByteStride;
                    jointsUInt32Job.result = output;
                    jointsUInt32Job.componentCount = componentCount;
                    jobHandle = jointsUInt32Job.Schedule(count, GltfImport.DefaultBatchCount);
                    break;
                default:
                    logger?.Error(LogCode.TypeUnsupported, "Joints", inputType.ToString());
                    jobHandle = null;
                    break;
            }

            Profiler.EndSample();
            return jobHandle;
        }
    }
}
