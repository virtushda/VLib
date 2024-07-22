using System;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    [Serializable]
    public class CustBuffDeclare_Vec4 : IVectorBufferDeclaration, IVectorBufferDeclaration<float4>
    {
        [SerializeField] protected float4 defaultValue;
        public float4 DefaultValue => defaultValue;

        public CustBuffDeclare_Vec4(float4 defaultValue) => this.defaultValue = defaultValue;

        public IVectorBufferDeclaration Clone() => new CustBuffDeclare_Vec4(defaultValue);
        
        public IVectorBuffer CreateBuffer(int initCapacity = 8) => new VectorBuffer4(defaultValue, initCapacity);
    }
}