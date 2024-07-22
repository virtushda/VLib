using System;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    [Serializable]
    public class CustBuffDeclare_Vec8 : IVectorBufferDeclaration, IVectorBufferDeclaration<float4x2>
    {
        [SerializeField] protected float4x2 defaultValue;
        public float4x2 DefaultValue => defaultValue;

        public CustBuffDeclare_Vec8(float4x2 defaultValue) => this.defaultValue = defaultValue;

        public IVectorBufferDeclaration Clone() => new CustBuffDeclare_Vec8(defaultValue);
        
        public IVectorBuffer CreateBuffer(int initCapacity = 8) => new VectorBuffer8(defaultValue, initCapacity);
    }
}