using System;
using Unity.Mathematics;
using UnityEngine;

namespace VLib
{
    [Serializable]
    public class CustBuffDeclare_Vec2 : IVectorBufferDeclaration, IVectorBufferDeclaration<float2>
    {
        [SerializeField] protected float2 defaultValue;
        public float2 DefaultValue => defaultValue;

        public CustBuffDeclare_Vec2(float2 defaultValue) => this.defaultValue = defaultValue;

        public IVectorBufferDeclaration Clone() => new CustBuffDeclare_Vec2(defaultValue);
        
        public  IVectorBuffer CreateBuffer(int initCapacity = 8) => new VectorBuffer2(defaultValue, initCapacity);
    }
}