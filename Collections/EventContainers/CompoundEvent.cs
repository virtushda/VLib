using System;

namespace VLib
{
    public class CompoundEvent : EventContainer
    {
        private readonly ushort[] triggers;
        
        public CompoundEvent(int triggerCount)
        {
            triggers = new ushort[triggerCount];
        }

        public void Trigger(int index)
        {
            if(TryTrigger(triggers, index))
                Invoke();
        }

        public static bool TryTrigger(ushort[] triggers, int index)
        {
            triggers[index]++;
            
            for (int i = 0; i < triggers.Length; i++)
                if (triggers[i] == 0)
                    return false;
            
            for (int i = 0; i < triggers.Length; i++)
                triggers[i]--;

            return true;
        }
    }
    
    public class CompoundEvent<T> : EventContainer<T>
    {
        private readonly ushort[] triggers;
        
        public CompoundEvent(int triggerCount)
        {
            triggers = new ushort[triggerCount];
        }

        public void Trigger(int index, T args)
        {
            if(CompoundEvent.TryTrigger(triggers, index))
                Invoke(args);
        }
    }
}