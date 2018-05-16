using System;

namespace TestParser.Workers
{
    public class WorkItemFinishedEventArgs : EventArgs
    {
        public WorkItemFinishedEventArgs(WorkResult result)
        {
            Result = result;
        }

        public WorkResult Result { get; private set; }
    }
}
