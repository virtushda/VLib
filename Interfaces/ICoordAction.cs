using Unity.Mathematics;

namespace VLib
{
    public interface ICoordAction
    {
        public void Execute(int2 coord);
    }
}