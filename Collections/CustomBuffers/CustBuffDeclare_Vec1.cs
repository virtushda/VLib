using System;
using UnityEngine;

namespace VLib
{
    [Serializable]
    public class CustBuffDeclare_Vec1 : IVectorBufferDeclaration, IVectorBufferDeclaration<float>
    {
        [SerializeField] protected float defaultValue;
        public float DefaultValue => defaultValue;

        public CustBuffDeclare_Vec1(float defaultValue) => this.defaultValue = defaultValue;

        public IVectorBufferDeclaration Clone() => new CustBuffDeclare_Vec1(defaultValue);

        public IVectorBuffer CreateBuffer(int initCapacity = 8) => new VectorBuffer1(defaultValue, initCapacity);
    }
}