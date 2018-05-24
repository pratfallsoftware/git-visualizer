using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitViewer
{
    class AnimatedValue<T>
    {
        public bool IsAnimating
        {
            get
            {
                if (InitialValue.Equals(FinalValue))
                {
                    return false;
                }
                if (DateTime.Now < StartTime)
                {
                    return false;
                }
                if (DateTime.Now > CompletionTime)
                {
                    return false;
                }

                return true;
            }
        }
        public DateTime StartTime { get; private set; }
        public DateTime CompletionTime { get; private set; }
        public T InitialValue { get; private set; }
        public T FinalValue { get; private set; }
        public T Value
        {
            get
            {
                return CalculateValue();
            }
        }

        public AnimatedValue(T value)
            : this(value, value, DateTime.Now, DateTime.Now)
        {
        }

        public AnimatedValue(T initialValue, T finalValue, DateTime completionTime)
            : this(initialValue, finalValue, DateTime.Now, completionTime)
        {
        }

        public AnimatedValue(T initialValue, T finalValue, DateTime startTime, DateTime completionTime)
        {
            Animate(initialValue, finalValue, startTime, completionTime);
        }

        public void Animate(T initialValue, T finalValue, DateTime startTime, DateTime completionTime)
        {
            InitialValue = initialValue;
            FinalValue = finalValue;
            CompletionTime = completionTime;
            StartTime = startTime;
        }

        private T CalculateValue()
        {
            double elapsedMilliseconds = DateTime.Now.Subtract(StartTime).TotalMilliseconds;
            double totalMilliseconds = CompletionTime.Subtract(StartTime).TotalMilliseconds;

            if (totalMilliseconds == 0)
            {
                return FinalValue;
            }

            double completedPercent = elapsedMilliseconds / totalMilliseconds;

            if (completedPercent < 0)
            {
                return InitialValue;
            }
            if (completedPercent > 1)
            {
                return FinalValue;
            }
            return (T)(completedPercent * (dynamic)FinalValue + (1 - completedPercent) * (dynamic)InitialValue);
        }

        internal void PopTo(T value)
        {
            Animate(value, value, DateTime.Now, DateTime.Now);
        }
    }
}
