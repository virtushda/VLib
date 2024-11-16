namespace VLib
{
    public static class TimeExt
    {
        public static bool TriggerPeriodic(this double time, float deltaTime, float frequency)
        {
            return time % frequency + deltaTime >= frequency;
        }
    }
}