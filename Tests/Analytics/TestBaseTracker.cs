using System;
using NUnit.Framework;
using Toggl.Phoebe.Analytics;
using XPlatUtils;
using Toggl.Phoebe.Data;
using Moq;

namespace Toggl.Phoebe.Tests.Analytics
{
    [TestFixture]
    public class TestBaseTracker : Test
    {
        private TestTracker tracker;

        public override void SetUp ()
        {
            base.SetUp();
            ServiceContainer.Register<ISettingsStore> (Mock.Of<ISettingsStore> (
                        (store) => store.ExperimentId == (string)null));
            ServiceContainer.Register<ExperimentManager> (new ExperimentManager ());
            tracker = new TestTracker ();
        }

        [Test]
        public void TestSendSettingsChangeEvent()
        {
            try {
                tracker.SendSettingsChangeEvent(SettingName.AskForProject);
            } catch (Exception e) {
//                e.Data.
                Console.WriteLine("e: {0}", e.Data.Contains("category"));
            }
        } 
    }
}

