﻿using System;
using Android.App;
using Android.Content;
using Toggl.Phoebe.Net;
using XPlatUtils;
using Toggl.Joey.Bugsnag;

namespace Toggl.Joey.UI.Activities
{
    public abstract class BaseActivity : Activity
    {
        protected virtual bool RequireAuth {
            get { return true; }
        }

        private void CheckAuth ()
        {
            if (!RequireAuth)
                return;
            var authManager = ServiceContainer.Resolve<AuthManager> ();
            if (!authManager.IsAuthenticated) {
                var intent = new Intent (this, typeof(LoginActivity));
                intent.AddFlags (ActivityFlags.ClearTop);
                StartActivity (intent);
                Finish ();
            }
        }

        private BugsnagClient BugsnagClient {
            get {
                return (BugsnagClient)ServiceContainer.Resolve<Toggl.Phoebe.Bugsnag.BugsnagClient> ();
            }
        }

        protected override void OnCreate (Android.OS.Bundle state)
        {
            base.OnCreate (state);
            BugsnagClient.OnActivityCreated (this);
            CheckAuth ();
        }

        protected override void OnResume ()
        {
            base.OnResume ();
            BugsnagClient.OnActivityResumed (this);
            CheckAuth ();
        }

        protected override void OnPause ()
        {
            base.OnPause ();
            BugsnagClient.OnActivityPaused (this);
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy ();
            BugsnagClient.OnActivityDestroyed (this);
        }
    }
}