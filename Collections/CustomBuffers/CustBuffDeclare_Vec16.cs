using System;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    [Serializable]
    public class CustBuffDeclare_Vec16 : IVectorBufferDeclaration, IVectorBufferDeclaration<float4x4>
    {
        [SerializeField] protected float4x4 defaultValue;
        public float4x4 DefaultValue => defaultValue;

        public CustBuffDeclare_Vec16(float4x4 defaultValue) => this.defaultValue = defaultValue;

        public IVectorBufferDeclaration Clone() => new CustBuffDeclare_Vec16(defaultValue);

        public IVectorBuffer CreateBuffer(int initCapacity = 8) => new VectorBuffer16(defaultValue, initCapacity);
    }
}