using System;
using Toggl.Phoebe.Analytics;

namespace Toggl.Phoebe.Tests.Analytics
{
public class TestTracker : BaseTracker
    {
        protected override void StartNewSession()
        {
        }

        protected override void SendTiming(long elapsedMilliseconds, string category, string variable, string label = null)
        {
        }

        protected override void SendEvent(string category, string action, string label = null, long value = 0L)
        {
            throw new Exception(category);
        }

        protected override void SetCustomDimension(int idx, string value)
        {
        }

        public override string CurrentScreen {
            set {
            }
        }
    }

}

